﻿using System;
using System.Threading.Tasks;
using System.Timers;

namespace RotatingTable.Xamarin.Services
{
    public interface IBluetoothService
    {
        event ElapsedEventHandler Timeout;

        bool IsConnected { get; }
        bool IsRunning { get; }

        Task<bool> ConnectAsync<T>(T deviceOrId);
        Task DisconnectAsync();

        Task<string> GetStatusAsync();
        Task<bool> RunAutoModeAsync(EventHandler<DeviceInputEventArgs> eventHandler);
        Task<bool> RunFreeMovementAsync();
        Task<bool> RotateAsync(int angle, EventHandler<DeviceInputEventArgs> eventHandler);
        Task<bool> RunVideoAsync(EventHandler<DeviceInputEventArgs> eventHandler);
        Task<bool> SetAccelerationAsync(int acceleration);
        Task<bool> SetDelayAsync(int delay);
        Task<bool> SetExposureAsync(int exposure);
        Task<bool> SetStepsAsync(int steps);
        Task<bool> StopAsync();
    }
}