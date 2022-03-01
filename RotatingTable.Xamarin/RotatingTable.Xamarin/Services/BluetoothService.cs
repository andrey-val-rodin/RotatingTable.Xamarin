using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using RotatingTable.Xamarin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Services
{
    public class BluetoothService : IBluetoothService
    {
        private static readonly Guid ServiceUuid = new("e7810a71-73ae-499d-8c15-faa9aef0c3f2");
        public const char Terminator = '\n';

        private readonly IUserDialogs _userDialogs;
        private string _response;
        private readonly ListeningStream _stream = new();
        private EventHandler<DeviceInputEventArgs> _streamTokenHandler;
        private readonly List<string> _acceptedTokens = new();
        private bool _isListening;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public BluetoothService()
        {
            _userDialogs = UserDialogs.Instance;
        }

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;
        private ICharacteristic Characteristic { get; set; }

        public bool IsConnected { get; private set; }
        public bool IsRunning { get; private set; }

        public async Task<bool> ConnectAsync<T>(T deviceOrId)
        {
            CancellationTokenSource tokenSource = new();
            string error = null;
            try
            {
                using (var progress = _userDialogs.Progress(CreateDialogConfig(tokenSource)))
                {
                    IDevice device = deviceOrId is IDevice
                        ? await ConnectToDeviceAsync((IDevice)deviceOrId, tokenSource.Token)
                        : await ConnectToDeviceAsync((Guid)(object)deviceOrId, tokenSource.Token);
                    if (device == null)
                        return false;

                    // Get service
                    var service = await LoadService(device, tokenSource.Token);
                    if (service == null)
                    {
                        error = $"Сервис {ServiceUuid} не найден";
                        return false;
                    }

                    // Get characteristic
                    Characteristic = await LoadCharacteristics(service, tokenSource.Token);
                    if (Characteristic == null)
                    {
                        error = "Характеристика не обнаружена";
                        return false;
                    }

                    // Start listening
                    IsConnected = true;
                    await Characteristic.StartUpdatesAsync();
                    Characteristic.ValueUpdated += CharacteristicListeningHandler;

                    error = await CheckStatus();
                    if (!string.IsNullOrEmpty(error))
                    {
                        IsConnected = false;
                        return false;
                    }

                    // Save config
                    var configService = DependencyService.Resolve<IConfigService>();
                    if (!await SetStepsAsync(await configService.GetStepsAsync()) ||
                        !await SetAccelerationAsync(await configService.GetAccelerationAsync()) ||
                        !await SetDelayAsync(await configService.GetDelayAsync()) ||
                        !await SetExposureAsync(await configService.GetExposureAsync()))
                    {
                        IsConnected = false;
                        error = "Не удалось передать столу параметры";
                        return false;
                    }

                    return IsConnected;
                }
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
                tokenSource.Dispose();
                if (!IsConnected)
                {
                    if (!string.IsNullOrEmpty(error))
                        await _userDialogs.AlertAsync(error);

                    if (Characteristic != null)
                    {
                        await Characteristic.StopUpdatesAsync();
                        Characteristic = null;
                    }
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (IsRunning)
                throw new InvalidOperationException("Unable to disconnect while running");

            if (Characteristic != null)
            {
                await Characteristic.StopUpdatesAsync();
                Characteristic = null;
            }

            IsConnected = false;
        }

        private void CharacteristicListeningHandler(object sender, CharacteristicUpdatedEventArgs args)
        {
            _stream.Append(args.Characteristic.Value);
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

        private async Task<IService> LoadService(IDevice device, CancellationToken token)
        {
            try
            {
                var services = await device.GetServicesAsync(token);
                return services.FirstOrDefault(s => s.Id == ServiceUuid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        private async Task<ICharacteristic> LoadCharacteristics(IService service, CancellationToken token)
        {
            try
            {
                var characteristics = await service.GetCharacteristicsAsync();
                return characteristics.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
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
                    Text = "Стол в процессе работы. Завершить?",
                    CancelText = "Отмена",
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
                    return "Перегрузите стол и повторите попытку подключения позже";
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

        private async Task<string> WriteWithResponseAsync(string text, string[] acceptedTokens)
        {
            System.Diagnostics.Debug.WriteLine($"Command: {text}");

            await _semaphore.WaitAsync();

            _acceptedTokens.Clear();
            _acceptedTokens.AddRange(acceptedTokens);

            // Append terminator
            text += Terminator;
            
            try
            {
                _stream.TokenUpdated += CommandHandler;
                _response = null;

                if (!await Characteristic.WriteAsync(Encoding.ASCII.GetBytes(text)))
                    return null;

                var token = new CancellationTokenSource(500).Token;
                string response = null;
                await Task.Run(() =>
                {
                    while (true)
                    {
                        response = _response;
                        if (!string.IsNullOrEmpty(response) || token.IsCancellationRequested)
                            return;
                    }
                }, token);

                if (string.IsNullOrEmpty(response))
                    await _userDialogs.AlertAsync("Превышено время ожидания ответа");

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return null;
            }
            finally
            {
                _semaphore.Release();
                _stream.TokenUpdated -= CommandHandler;
            }
        }

        private void CommandHandler(object sender, DeviceInputEventArgs args)
        {
            var token = args.Text;

            // Take only first eccepted token
            if (string.IsNullOrEmpty(_response) && _acceptedTokens.Contains(token))
                _response = args.Text;
        }

        private void BeginListening(EventHandler<DeviceInputEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (_isListening)
                throw new InvalidOperationException("Listen already");

            _streamTokenHandler = handler;
            _isListening = true;
            _stream.TokenUpdated += RunningHandler;
        }

        private void EndListening()
        {
            if (!_isListening)
                throw new InvalidOperationException("BeginListening was not called");

            _stream.TokenUpdated -= RunningHandler;
            _isListening = false;
            _streamTokenHandler = null;
        }

        public async Task<string> GetStatusAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await WriteWithResponseAsync(Commands.Status,
                new[] { Commands.Running, Commands.Busy, Commands.Ready });
        }

        public async Task<bool> RunAutoModeAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunAutoMode, eventHandler);
        }

        public async Task<bool> RunFreeMovementAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunFreeMovement);
        }

        public async Task<bool> RotateAsync(int angle, EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.FreeMovement + angle, eventHandler);
        }

        private async Task<bool> RunAsync(string command, EventHandler<DeviceInputEventArgs> eventHandler = null)
        {
            if (IsRunning)
                throw new InvalidOperationException("Running already");

            IsRunning = true;
            var success = false;
            try
            {
                if (eventHandler != null)
                {
                    BeginListening(eventHandler);
                }

                success = await WriteWithResponseAsync(command,
                    new[] { Commands.OK, Commands.Error }) == Commands.OK;
            }
            finally
            {
                if (!success)
                {
                    IsRunning = false;
                    EndListening();
                }
            }

            return success;
        }

        private void RunningHandler(object sender, DeviceInputEventArgs args)
        {
            _streamTokenHandler?.Invoke(this, args);
            if (args.Text == Commands.End)
            {
                // Finishing
                IsRunning = false;
                EndListening();
            }
            // TODO: what happens if input never get "END"? - add here timeout for input
            // Timer class?
        }

        public async Task<bool> StopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var result = await WriteWithResponseAsync(Commands.Stop,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
            if (_isListening)
                EndListening();

            return result;
        }

        public async Task<bool> SetStepsAsync(int steps)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetSteps + ' ' + steps.ToString();
            return await WriteWithResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetAccelerationAsync(int acceleration)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetAcceleration + ' ' + acceleration.ToString();
            return await WriteWithResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetDelayAsync(int delay)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetDelay + ' ' + delay.ToString();
            return await WriteWithResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetExposureAsync(int exposure)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetExposure + ' ' + exposure.ToString();
            return await WriteWithResponseAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }
    }
}
