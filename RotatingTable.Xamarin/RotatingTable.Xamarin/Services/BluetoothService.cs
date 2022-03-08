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
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Services
{
    public class BluetoothService : IBluetoothService
    {
        private static readonly Guid ServiceUuid = new("e7810a71-73ae-499d-8c15-faa9aef0c3f2");
        public const char Terminator = '\n';

        private readonly IUserDialogs _userDialogs;
        private string _response;
        private ListeningStream _stream;
        private EventHandler<DeviceInputEventArgs> _streamTokenHandler;
        private readonly List<string> _acceptedTokens = new();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly System.Timers.Timer _timer;

        public BluetoothService(IUserDialogs userDialogs)
        {
            _userDialogs = userDialogs;
            _timer = new System.Timers.Timer(3000);
            _timer.AutoReset = false;
        }

        public event ElapsedEventHandler Timeout
        {
            add => _timer.Elapsed += value;
            remove => _timer.Elapsed -= value;
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
                    Characteristic = await LoadCharacteristics(service);
                    if (Characteristic == null)
                    {
                        error = "Характеристика не обнаружена";
                        return false;
                    }

                    // Start listening from characteristic
                    IsConnected = true;
                    _stream = new();
                    Characteristic.ValueUpdated += CharacteristicListeningHandler;
                    await Characteristic.StartUpdatesAsync();

                    error = await CheckStatus();
                    if (!string.IsNullOrEmpty(error) || !IsConnected)
                    {
                        IsConnected = false;
                        return false;
                    }

                    if (tokenSource.Token.IsCancellationRequested)
                        return false;

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
                if (IsRunning)
                {
                    // Abort listening
                    EndListening();
                }

                if (Characteristic != null)
                {
                    Characteristic.ValueUpdated -= CharacteristicListeningHandler;
                    await Characteristic.StopUpdatesAsync();
                }
            }
            finally
            {
                _stream = null;
                Characteristic = null;
                IsRunning = false;
                IsConnected = false;
            }
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

        private async Task<ICharacteristic> LoadCharacteristics(IService service)
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

        private async Task<bool> WriteCommandAsync(string command)
        {
            return await WriteCommandAsync(command,
                    new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        private async Task<string> WriteCommandAsync(string command, string[] acceptedTokens)
        {
            System.Diagnostics.Debug.WriteLine($"Command: {command}");

            await _semaphore.WaitAsync();

            _acceptedTokens.Clear();
            _acceptedTokens.AddRange(acceptedTokens);
            if (!_acceptedTokens.Contains(Commands.Undefined))
                _acceptedTokens.Add(Commands.Undefined);

            // Append terminator
            command += Terminator;
            
            try
            {
                _stream.TokenUpdated += CommandHandler;
                _response = null;
                Characteristic.WriteType = CharacteristicWriteType.WithoutResponse;

                // See API limitations in https://github.com/xabre/xamarin-bluetooth-le
                // "Characteristic/Descriptor Write: make sure you call characteristic.WriteAsync(...) from the main thread,
                // failing to do so will most probably result in a GattWriteError."
                if (!await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Characteristic.WriteAsync(Encoding.ASCII.GetBytes(command))))
                    return null;

                var token = new CancellationTokenSource(500).Token;
                string response = await Task.Run(async () =>
                {
                    while (true)
                    {
                        if (!string.IsNullOrEmpty(_response) || token.IsCancellationRequested)
                            return _response;

                        await Task.Delay(5);
                    }
                }, token);

                if (string.IsNullOrEmpty(response))
                    await _userDialogs.AlertAsync("Превышено время ожидания ответа");

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
//                await _userDialogs.AlertAsync(ex.Message, "Ошибка соединения");
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
            // Take only first accepted token
            if (!string.IsNullOrEmpty(_response))
                return;

            if (_acceptedTokens.Contains(args.Text))
                _response = args.Text;
        }

        private void BeginListening(EventHandler<DeviceInputEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _streamTokenHandler = handler;
            _stream.TokenUpdated += RunningHandler;
            _timer.Start();
        }

        private void EndListening()
        {
            _stream.TokenUpdated -= RunningHandler;
            _streamTokenHandler = null;
            _timer.Stop();
            IsRunning = false;
        }

        private void RunningHandler(object sender, DeviceInputEventArgs args)
        {
            // Reset timer
            _timer.Stop();
            _timer.Start();

            _streamTokenHandler?.Invoke(this, args);
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

            return await WriteCommandAsync(Commands.Status,
                new[] { Commands.Running, Commands.Busy, Commands.Ready });
        }

        public async Task<bool> RunAutoAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunAutoMode, eventHandler);
        }

        public async Task<bool> RunNonStopAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunNonStopMode, eventHandler);
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

        public async Task<bool> RunVideoAsync(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            return await RunAsync(Commands.RunVideoMode, eventHandler);
        }

        public async Task<bool> IncreasePWMAsync()
        {
            return await WriteCommandAsync(Commands.IncreasePWM);
        }

        public async Task<bool> DecreasePWMAsync()
        {
            return await WriteCommandAsync(Commands.DecreasePWM);
        }

        private async Task<bool> RunAsync(string command, EventHandler<DeviceInputEventArgs> eventHandler = null)
        {
            if (IsRunning)
                throw new InvalidOperationException("Running already");

            var success = false;
            try
            {
                if (eventHandler != null)
                {
                    IsRunning = true;
                    BeginListening(eventHandler);
                }

                success = await WriteCommandAsync(command,
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

        public async Task<bool> StopAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var result = await WriteCommandAsync(Commands.Stop,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
            IsRunning = false;
            return result;
        }

        public async Task<bool> SetStepsAsync(int steps)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetSteps + ' ' + steps.ToString();
            return await WriteCommandAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetAccelerationAsync(int acceleration)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetAcceleration + ' ' + acceleration.ToString();
            return await WriteCommandAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetDelayAsync(int delay)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetDelay + ' ' + delay.ToString();
            return await WriteCommandAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }

        public async Task<bool> SetExposureAsync(int exposure)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            var command = Commands.SetExposure + ' ' + exposure.ToString();
            return await WriteCommandAsync(command,
                new[] { Commands.OK, Commands.Error }) == Commands.OK;
        }
    }
}
