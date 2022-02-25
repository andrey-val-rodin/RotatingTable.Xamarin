using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using RotatingTable.Xamarin.Models;
using System;
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
        private readonly object _responseLock = new();
        private EventHandler<CharacteristicUpdatedEventArgs> _listeningHandler;
        private ListeningStream _listeningStream;

        public BluetoothService()
        {
            _userDialogs = UserDialogs.Instance;
        }

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;
        private ICharacteristic Characteristic { get; set; }

        public bool IsConnected { get; private set; }
        public bool IsListening { get; private set; }

        public async Task<bool> ConnectAsync(Guid id)
        {
            CancellationTokenSource tokenSource = new();
            string error = null;
            try
            {
                using (var progress = _userDialogs.Progress(CreateDialogConfig(tokenSource)))
                {
                    IDevice device = await ConnectToDeviceAsync(id, tokenSource.Token);
                    if (device == null)
                        return false;

                    error = await DoConnectSteps(device, tokenSource.Token);
                    return IsConnected;
                }
            }
            finally
            {
                tokenSource.Dispose();
                if (!string.IsNullOrEmpty(error))
                    await _userDialogs.AlertAsync(error);
            }
        }

        public async Task<bool> ConnectAsync(IDevice device)
        {
            Monitor.Enter(this);
            CancellationTokenSource tokenSource = new();
            string error = null;
            try
            {
                using (var progress = _userDialogs.Progress(CreateDialogConfig(tokenSource)))
                {
                    if (!await ConnectToDeviceAsync(device, tokenSource.Token))
                        return false;

                    error = await DoConnectSteps(device, tokenSource.Token);
                    return IsConnected;
                }
            }
            finally
            {
                tokenSource.Dispose();
                if (!string.IsNullOrEmpty(error))
                    await _userDialogs.AlertAsync(error);
            }
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

        private async Task<bool> ConnectToDeviceAsync(IDevice device, CancellationToken token)
        {
            try
            {
                await Adapter.ConnectToDeviceAsync(device,
                    new ConnectParameters(false, forceBleTransport: true), token);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                if (!token.IsCancellationRequested)
                    await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return false;
            }
        }

        private async Task<string> DoConnectSteps(IDevice device, CancellationToken token)
        {
            var service = await LoadService(device, token);
            if (service == null)
                return $"Сервис {ServiceUuid} не найден";

            Characteristic = await LoadCharacteristics(service, token);
            if (Characteristic == null)
                return "Характеристика не обнаружена";

            IsConnected = true;
            var response = await CheckStatus();
            if (!string.IsNullOrEmpty(response))
                return response;

            // Save config
            var configService = DependencyService.Resolve<IConfigService>();
            if (!await SetStepsAsync(await configService.GetStepsAsync()) ||
                !await SetAccelerationAsync(await configService.GetAccelerationAsync()) ||
                !await SetDelayAsync(await configService.GetDelayAsync()) ||
                !await SetExposureAsync(await configService.GetExposureAsync()))
            {
                IsConnected = false;
                return "Не удалось передать столу параметры";
            }

            return null;
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
            if (response == "RUNNING")
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
            else if (response == "BUSY")
            {
                // Try to stop and then ask table for status again
                if (!await StopAsync())
                {
                    IsConnected = false;
                    return "Перегрузите стол и повторите попытку подключения позже";
                }

                response = await GetStatusAsync();
            }

            if (response != Commands.StatusReady)
            {
                IsConnected = false;
                return string.IsNullOrEmpty(response)
                    ? "Стол не отвечает"
                    : $"Неизвестный ответ стола: {response}";
            }

            return null;
        }

        private async Task<string> WriteWithResponseAsync(string text)
        {
            System.Diagnostics.Debug.WriteLine("Write: " + text);

            // Append terminator
            text += Terminator;
            try
            {
                _response = null;
                await BeginListeningAsync(OnCharacteristicValueUpdated);
                await Characteristic.WriteAsync(Encoding.ASCII.GetBytes(text));
                var token = new CancellationTokenSource(500).Token;
                string response = null;
                await Task.Run(() =>
                {
                    while (true)
                    {
                        response = GetResponse();
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
                return null;
            }
            finally
            {
                await EndListeningAsync();
            }
        }

        private string GetResponse()
        {
            lock (_responseLock)
            {
                return _response;
            }
        }

        private void OnCharacteristicValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
        {
            string response = null;
            _listeningStream.Append(args.Characteristic.Value, (e, a) =>
            {
                if (response == null)
                    response = a.Text;
            });

            lock (_responseLock)
            {
                _response = response;
            }
        }

        private async Task BeginListeningAsync(EventHandler<CharacteristicUpdatedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (IsListening)
                throw new InvalidOperationException("Listen already");

            _listeningStream = new();
            _listeningHandler = handler;
            Characteristic.ValueUpdated += _listeningHandler;
            IsListening = true;
            await Characteristic.StartUpdatesAsync();
        }

        private async Task EndListeningAsync()
        {
            await Characteristic.StopUpdatesAsync();
            Characteristic.ValueUpdated -= _listeningHandler;
            _listeningStream = null;
            IsListening = false;
        }

        public async Task<string> GetStatusAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await WriteWithResponseAsync(Commands.Status);
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

        public async Task<bool> StopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            if (IsListening)
                await EndListeningAsync();

            return await WriteWithResponseAsync(Commands.Stop) == Commands.OK;
        }

        private async Task<bool> RunAsync(string command, EventHandler<DeviceInputEventArgs> eventHandler = null)
        {
            var response = await WriteWithResponseAsync(command);
            if (response != Commands.OK)
                return false;

            if (eventHandler != null)
            {
                await BeginListeningAsync((source, args) =>
                {
                    _listeningStream.Append(args.Characteristic.Value, async (e, a) =>
                    {
                        eventHandler.Invoke(this, new DeviceInputEventArgs(a.Text));
                        if (a.Text == Commands.End)
                            await EndListeningAsync();
                        // TODO: what happens if input never get "END"? - add here timeout for input
                        // Timer class?
                    });
                });
            }

            return true;
        }

        public async Task<bool> SetStepsAsync(int steps)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetSteps + ' ' + steps.ToString();
            return await WriteWithResponseAsync(command) == Commands.OK;
        }

        public async Task<bool> SetAccelerationAsync(int acceleration)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetAcceleration + ' ' + acceleration.ToString();
            return await WriteWithResponseAsync(command) == Commands.OK;
        }

        public async Task<bool> SetDelayAsync(int delay)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetDelay + ' ' + delay.ToString();
            return await WriteWithResponseAsync(command) == Commands.OK;
        }

        public async Task<bool> SetExposureAsync(int exposure)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetExposure + ' ' + exposure.ToString();
            return await WriteWithResponseAsync(command) == Commands.OK;
        }
    }
}
