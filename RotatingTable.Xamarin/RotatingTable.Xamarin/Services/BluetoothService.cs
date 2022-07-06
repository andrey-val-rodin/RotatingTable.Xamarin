using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using RotatingTable.Xamarin.Handlers;
using RotatingTable.Xamarin.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Services
{
    public class BluetoothService : IBluetoothService
    {
        private static readonly Guid UpdatesCharacteristicUuid = new("0000ffe1-0000-1000-8000-00805f9b34fb");
        private static readonly Guid WriteCharacteristicUuid   = new("0000ffe2-0000-1000-8000-00805f9b34fb");

        public const char Terminator = '\n';

        private readonly IUserDialogs _userDialogs;
        private string _stringResponse;
        private int? _intResponse;
        private float? _floatResponse;
        private ListeningStream _stream;
        private EventHandler<DeviceInputEventArgs> _streamTokenHandler;
        private EventHandler<DeviceInputEventArgs> _listeningHandler;
        private readonly List<string> _acceptedTokens = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly System.Timers.Timer _timer;
        private readonly AutoResetEvent _waitingEvent = new(false);

        public BluetoothService(IUserDialogs userDialogs)
        {
            _userDialogs = userDialogs;
            _timer = new System.Timers.Timer(3000) { AutoReset = false };
        }

        public event ElapsedEventHandler Timeout
        {
            add => _timer.Elapsed += value;
            remove => _timer.Elapsed -= value;
        }

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;
        private ICharacteristic WriteCharacteristic { get; set; }
        private ICharacteristic UpdatesCharacteristic { get; set; }

        public bool IsConnected { get; private set; }
        private bool IsListening { get; set; }

        public async Task<bool> ConnectAsync<T>(T deviceOrId)
        {
            if (IsConnected)
                throw new InvalidOperationException("Connected already");

            CancellationTokenSource tokenSource = new();
            string error = null;
            try
            {
                using var progress = _userDialogs.Progress(CreateDialogConfig(tokenSource));

                IDevice device = deviceOrId is IDevice d
                    ? await ConnectToDeviceAsync(d, tokenSource.Token)
                    : await ConnectToDeviceAsync((Guid)(object)deviceOrId, tokenSource.Token);
                if (device == null)
                    return false;

                if (!await LoadCharacteristics(device, tokenSource.Token))
                {
                    error = "Характеристика не обнаружена";
                    return false;
                }

                error = await CheckStatus();
                if (!string.IsNullOrEmpty(error) || !IsConnected)
                {
                    IsConnected = false;
                    return false;
                }

                if (tokenSource.Token.IsCancellationRequested)
                    return false;

                if (!await SetConfigAsync())
                {
                    IsConnected = false;
                    error = "Не удалось передать столу параметры";
                    return false;
                }

                return IsConnected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                if (!tokenSource.Token.IsCancellationRequested)
                    await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");

                IsConnected = false;
                return false;
            }
            finally
            {
                if (tokenSource.Token.IsCancellationRequested)
                    IsConnected = false;
                tokenSource.Dispose();

                if (!IsConnected)
                {
                    if (!string.IsNullOrEmpty(error))
                        await _userDialogs.AlertAsync(error);

                    await DisconnectAsync();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (IsListening)
                {
                    // Abort listening
                    EndListening();
                }

                if (UpdatesCharacteristic != null)
                {
                    UpdatesCharacteristic.ValueUpdated -= CharacteristicListeningHandler;
                    await UpdatesCharacteristic.StopUpdatesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                _stream = null;
                UpdatesCharacteristic = null;
                WriteCharacteristic = null;
                IsListening = false;
                IsConnected = false;
            }
        }

        private void CharacteristicListeningHandler(object sender, CharacteristicUpdatedEventArgs args)
        {
            _stream?.Append(args.Characteristic.Value);
        }

        private async Task<IDevice> ConnectToDeviceAsync(Guid id, CancellationToken token)
        {
            try
            {
                var device = await Adapter.ConnectToKnownDeviceAsync(id,
                    new ConnectParameters(false, forceBleTransport: true), token);

                return device;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                if (!token.IsCancellationRequested)
                    await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return null;
            }
        }

        private async Task<IDevice> ConnectToDeviceAsync(IDevice device, CancellationToken token)
        {
            try
            {
                await Adapter.ConnectToDeviceAsync(device,
                    new ConnectParameters(false, forceBleTransport: true), token);

                return device;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                if (!token.IsCancellationRequested)
                    await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return null;
            }
        }

        private ProgressDialogConfig CreateDialogConfig(CancellationTokenSource tokenSource)
        {
            return new ProgressDialogConfig()
            {
                Title = "Соединение...",
                CancelText = "Отмена",
                IsDeterministic = false,
                OnCancel = tokenSource.Cancel
            };
        }

        private async Task<bool> LoadCharacteristics(IDevice device, CancellationToken token)
        {
            try
            {
                UpdatesCharacteristic = null;
                WriteCharacteristic = null;
                var services = await device.GetServicesAsync(token);
                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();
                    if (UpdatesCharacteristic == null)
                        UpdatesCharacteristic = characteristics.FirstOrDefault(c => c.Id == UpdatesCharacteristicUuid);
                    if (WriteCharacteristic == null)
                        WriteCharacteristic = characteristics.FirstOrDefault(c => c.Id == WriteCharacteristicUuid);

                    if (UpdatesCharacteristic != null && WriteCharacteristic != null)
                        break;
                }

                if (UpdatesCharacteristic == null || WriteCharacteristic == null)
                    return false;

                // Start listening from update characteristic
                IsConnected = true;
                _stream = new();
                UpdatesCharacteristic.ValueUpdated += CharacteristicListeningHandler;
                await UpdatesCharacteristic.StartUpdatesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private async Task<string> CheckStatus()
        {
            var response = await GetStatusAsync();
            if (response == Commands.Running)
            {
                // Ask the user if app should stop table
                var result = await _userDialogs.PromptAsync(new PromptConfig
                {
                    Title = "Стол в процессе работы",
                    Text = "Завершить?",
                    CancelText = "Нет",
                    OkText = "Да"
                });

                if (result.Ok)
                {
                    if (!await StopAsync())
                    {
                        IsConnected = false;
                        return "Перезагрузите стол и повторите попытку подключения позже";
                    }

                    response = await GetStatusAsync();
                }
                else
                {
                    IsConnected = false;
                    return null;
                }
            }
            else if (response == Commands.Busy)
            {
                // Try to stop and then ask table for status again
                if (!await StopAsync())
                {
                    IsConnected = false;
                    return "Перезагрузите стол и повторите попытку подключения позже";
                }

                response = await GetStatusAsync();
            }

            if (response != Commands.Ready)
            {
                IsConnected = false;
                return string.IsNullOrEmpty(response)
                    ? "Стол не отвечает"
                    : $"Неизвестный ответ стола: {response}";
            }

            return null;
        }

        private async Task<bool> SetConfigAsync()
        {
            var config = DependencyService.Resolve<IConfig>();
            var steps = await config.GetStepsAsync();
            var acceleration = await config.GetAccelerationAsync();
            var delay = await config.GetDelayAsync();
            var exposure = await config.GetExposureAsync();
            var videoPwm = await config.GetVideoPWMAsync();
            var nonstopFrequency = await config.GetNonstopFrequencyAsync();

            if (!await SetStepsAsync(steps) ||
                !await SetAccelerationAsync(acceleration) ||
                !await SetDelayAsync(delay) ||
                !await SetExposureAsync(exposure) ||
                !await SetNonstopFrequencyAsync(nonstopFrequency))
                return false;

            if (!await SetVideoPWMAsync(videoPwm))
            {
                // Current videoPwm can be invalid for this table
                // try to use default
                if (await SetVideoPWMAsync(ConfigValidator.DefaultVideoPWMValue))
                    videoPwm = ConfigValidator.DefaultVideoPWMValue;
                else
                    return false;
            }

            await config.SetStepsAsync(steps);
            await config.SetAccelerationAsync(acceleration);
            await config.SetDelayAsync(delay);
            await config.SetExposureAsync(exposure);
            await config.SetVideoPWMAsync(videoPwm);
            await config.SetNonstopFrequencyAsync(nonstopFrequency);
            return true;
        }

        private async Task<bool> SendCommandAsync(string command)
        {
            return await WriteCommandAndGetResponseAsync(command,
                    new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        private async Task<int?> GetIntParameterAsync(string parameter)
        {
            return await WriteCommandAndGetResponseAsync(parameter, IntHandler, ListenForInt);
        }

        private async Task<float?> GetFloatParameterAsync(string parameter)
        {
            return await WriteCommandAndGetResponseAsync(parameter, FloatHandler, ListenForFloat);
        }

        private async Task<string> WriteCommandAndGetResponseAsync(string command, string[] acceptedTokens)
        {
            _acceptedTokens.Clear();
            _acceptedTokens.AddRange(acceptedTokens);

            return await WriteCommandAndGetResponseAsync(command, CommandHandler, ListenForOkOrErrorAsync);
        }

        private async Task<T> WriteCommandAndGetResponseAsync<T>(
            string command,
            EventHandler<DeviceInputEventArgs> handler,
            Func<string, EventHandler<DeviceInputEventArgs>, Task<T>> func)
        {
            // Append terminator
            command += Terminator;

            await _semaphore.WaitAsync();
            try
            {
                return await func(command, handler);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return default;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> ListenForOkOrErrorAsync(string command, EventHandler<DeviceInputEventArgs> handler)
        {
            System.Diagnostics.Debug.WriteLine($"Command: {command}");

            _stringResponse = null;
            _stream.TokenUpdated += handler;
            try
            {
                // See API limitations in https://github.com/xabre/xamarin-bluetooth-le
                // "Characteristic/Descriptor Write: make sure you call characteristic.WriteAsync(...) from the main thread,
                // failing to do so will most probably result in a GattWriteError."
                if (!await MainThread.InvokeOnMainThreadAsync(async () =>
                    await WriteCharacteristic.WriteAsync(Encoding.ASCII.GetBytes(command))))
                    return null;

                await Task.Run(() => _waitingEvent.WaitOne(1000));

                if (string.IsNullOrEmpty(_stringResponse))
                    await _userDialogs.AlertAsync("Стол не отвечает на команду");

                return _stringResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                await _userDialogs.AlertAsync(ex.Message, $"Не удалось отправить команду {command}");
                return null;
            }
            finally
            {
                if (_stream != null)
                    _stream.TokenUpdated -= handler;
            }
        }

        private void CommandHandler(object sender, DeviceInputEventArgs args)
        {
            // Take only first accepted token
            if (!string.IsNullOrEmpty(_stringResponse))
                return;

            if (_acceptedTokens.Contains(args.Text))
            {
                _stringResponse = args.Text;
                _waitingEvent.Set();
            }
        }

        private async Task<int?> ListenForInt(string command, EventHandler<DeviceInputEventArgs> handler)
        {
            System.Diagnostics.Debug.WriteLine($"Command: {command}");

            _intResponse = null;
            _stream.TokenUpdated += handler;
            try
            {
                // See API limitations in https://github.com/xabre/xamarin-bluetooth-le
                // "Characteristic/Descriptor Write: make sure you call characteristic.WriteAsync(...) from the main thread,
                // failing to do so will most probably result in a GattWriteError."
                if (!await MainThread.InvokeOnMainThreadAsync(async () =>
                    await WriteCharacteristic.WriteAsync(Encoding.ASCII.GetBytes(command))))
                    return null;

                await Task.Run(() => _waitingEvent.WaitOne(500));

                if (_intResponse == null)
                    await _userDialogs.AlertAsync("Стол не возвращает целочисленное значение");

                return _intResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                await _userDialogs.AlertAsync(ex.Message, $"Не удалось отправить команду {command}");
                return null;
            }
            finally
            {
                if (_stream != null)
                    _stream.TokenUpdated -= handler;
            }
        }

        private void IntHandler(object sender, DeviceInputEventArgs args)
        {
            // Take only first accepted token
            if (_intResponse != null)
                return;

            if (int.TryParse(args.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                _intResponse = result;
        }

        private async Task<float?> ListenForFloat(string command, EventHandler<DeviceInputEventArgs> handler)
        {
            System.Diagnostics.Debug.WriteLine($"Command: {command}");

            _floatResponse = null;
            _stream.TokenUpdated += handler;
            try
            {
                // See API limitations in https://github.com/xabre/xamarin-bluetooth-le
                // "Characteristic/Descriptor Write: make sure you call characteristic.WriteAsync(...) from the main thread,
                // failing to do so will most probably result in a GattWriteError."
                if (!await MainThread.InvokeOnMainThreadAsync(async () =>
                    await WriteCharacteristic.WriteAsync(Encoding.ASCII.GetBytes(command))))
                    return null;

                await Task.Run(() => _waitingEvent.WaitOne(500));

                if (_floatResponse == null)
                    await _userDialogs.AlertAsync("Стол не возвращает значение с плавающей запятой");

                return _floatResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                await _userDialogs.AlertAsync(ex.Message, $"Не удалось отправить команду {command}");
                return null;
            }
            finally
            {
                if (_stream != null)
                    _stream.TokenUpdated -= handler;
            }
        }

        private void FloatHandler(object sender, DeviceInputEventArgs args)
        {
            // Take only first accepted token
            if (_floatResponse != null)
                return;

            if (float.TryParse(args.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                _floatResponse = result;
        }

        private void BeginListening(
            EventHandler<DeviceInputEventArgs> handler,
            EventHandler<DeviceInputEventArgs> listeningHandler)
        {
            _streamTokenHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            _listeningHandler = listeningHandler;
            _stream.TokenUpdated += _streamTokenHandler;
            _stream.TokenUpdated += _listeningHandler;
            _timer.Start();
            IsListening = true;
        }

        private void EndListening()
        {
            if (_stream != null)
            {
                _stream.TokenUpdated -= _streamTokenHandler;
                _stream.TokenUpdated -= _listeningHandler;
            }
            _streamTokenHandler = null;
            _timer.Stop();
            IsListening = false;
        }

        private void ListeningHandler(object sender, DeviceInputEventArgs args)
        {
            // Reset timer
            _timer.Stop();
            _timer.Start();

            if (args.Text == Commands.End)
            {
                // Finishing
                EndListening();
            }
        }

        public async Task<string> GetStatusAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await WriteCommandAndGetResponseAsync(Commands.Status,
                new[] { Commands.Running, Commands.Busy, Commands.Ready });
        }

        public async Task<bool> RunAutoAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunAutoMode, eventHandler, ListeningHandler);
        }

        public async Task<bool> RunManualAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunManualMode, null, null);
        }

        public async Task<bool> RunNonStopAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunNonStopMode, eventHandler, ListeningHandler);
        }

        public async Task<bool> RunFreeMovementAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunFreeMovement, null, ListeningHandler);
        }

        public async Task<bool> RotateAsync(int angle, EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.FreeMovement + angle, eventHandler, ListeningHandler);
        }

        public async Task<bool> RunVideoAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunVideoMode, eventHandler, ListeningHandler);
        }

        public async Task<bool> IncreasePWMAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await SendCommandAsync(Commands.IncreasePWM);
        }

        public async Task<bool> DecreasePWMAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await SendCommandAsync(Commands.DecreasePWM);
        }

        public async Task<bool> PhotoAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await SendCommandAsync(Commands.Shutter);
        }

        public async Task<bool> NextAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.Next, eventHandler, ManualListeningHandler);
        }

        private void ManualListeningHandler(object sender, DeviceInputEventArgs args)
        {
            // Reset timer
            _timer.Stop();
            _timer.Start();

            if (args.Text == Commands.End || args.Text.StartsWith(Commands.Step))
            {
                // Finishing
                EndListening();
            }
        }

        private async Task<bool> RunAsync(string command,
            EventHandler<DeviceInputEventArgs> eventHandler,
            EventHandler<DeviceInputEventArgs> listeningHandler)
        {
            if (IsListening)
                throw new InvalidOperationException("Listening already");

            var success = false;
            try
            {
                if (eventHandler != null)
                {
                    BeginListening(eventHandler, listeningHandler);
                }

                success = await WriteCommandAndGetResponseAsync(command,
                    new[] { Commands.OK, Commands.Error }) == Commands.OK;
            }
            finally
            {
                if (!success)
                    EndListening();
            }

            return success;
        }

        public async Task<bool> StopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var result = await WriteCommandAndGetResponseAsync(Commands.Stop,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;

            return result;
        }

        public async Task<bool> SoftStopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var result = await WriteCommandAndGetResponseAsync(Commands.SoftStop,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;

            return result;
        }

        public async Task<bool> SetStepsAsync(int steps)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetSteps + ' ' + steps.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetAccelerationAsync(int acceleration)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetAcceleration + ' ' + acceleration.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetDelayAsync(int delay)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetDelay + ' ' + delay.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetExposureAsync(int exposure)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetExposure + ' ' + exposure.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetVideoPWMAsync(int videoPwm)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetVideoPWM + ' ' + videoPwm.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetNonstopFrequencyAsync(float nonstopFrequency)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetNFrequency + ' ' + nonstopFrequency.ToString(CultureInfo.InvariantCulture);
            return await WriteCommandAndGetResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<int?> GetStepsAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetIntParameterAsync(Commands.GetSteps);
        }

        public async Task<int?> GetAccelerationAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetIntParameterAsync(Commands.GetAcceleration);
        }

        public async Task<int?> GetDelayAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetIntParameterAsync(Commands.GetDelay);
        }

        public async Task<int?> GetExposureAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetIntParameterAsync(Commands.GetExposure);
        }

        public async Task<int?> GetVideoPWMAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetIntParameterAsync(Commands.GetVideoPWM);
        }

        public async Task<float?> GetNonstopFrequencyAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await GetFloatParameterAsync(Commands.GetNFrequency);
        }
    }
}
