﻿using Acr.UserDialogs;
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
        private readonly object _responseLock = new();
        private EventHandler<CharacteristicUpdatedEventArgs> _listeningHandler;
        private readonly ListeningStream _listeningStream = new();
        private readonly List<string> _acceptedTokens = new();

        public BluetoothService()
        {
            _userDialogs = UserDialogs.Instance;
        }

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;
        private ICharacteristic Characteristic { get; set; }

        public bool IsConnected { get; private set; }

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
                    Characteristic.ValueUpdated += OnCharacteristicValueUpdated;
                    await Characteristic.StartUpdatesAsync();

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

                    Characteristic.ValueUpdated -= OnCharacteristicValueUpdated;
                    await Characteristic.StopUpdatesAsync();
                }
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
            _acceptedTokens.Clear();
            _acceptedTokens.AddRange(acceptedTokens);

            // Append terminator
            text += Terminator;
            try
            {
                _response = null;
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
                await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
                return null;
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
                // Ignore all tokens except accepted ones
                var token = a.Text;
                if (_acceptedTokens.Contains(token))
                    response = a.Text;
            });

            if (!string.IsNullOrEmpty(response))
            {
                lock (_responseLock)
                {
                    _response = response;
                }
            }
        }

        private void BeginListening(EventHandler<CharacteristicUpdatedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _listeningHandler = handler;
            Characteristic.ValueUpdated += _listeningHandler;
        }

        private void EndListening()
        {
            Characteristic.ValueUpdated -= _listeningHandler;
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
            var response = await WriteWithResponseAsync(command,
                new[] { Commands.OK, Commands.Error });
            if (response != Commands.OK)
                return false;

            if (eventHandler != null)
            {
                BeginListening((source, args) =>
                {
                    _listeningStream.Append(args.Characteristic.Value, (e, a) =>
                    {
                        eventHandler.Invoke(this, new DeviceInputEventArgs(a.Text));
                        if (a.Text == Commands.End)
                            EndListening();
                        // TODO: what happens if input never get "END"? - add here timeout for input
                        // Timer class?
                    });
                });
            }

            return true;
        }

        public async Task<bool> StopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            EndListening();

            return await WriteWithResponseAsync(Commands.Stop,
                new[] { Commands.OK }) == Commands.OK;
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
