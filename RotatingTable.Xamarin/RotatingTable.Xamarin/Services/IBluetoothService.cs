using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Services
{
    public interface IBluetoothService
    {
        bool IsConnected { get; }
        bool IsListening { get; }

        Task<bool> ConnectAsync(Guid id);
        Task<bool> ConnectAsync(IDevice device);
        Task<string> GetStatusAsync();
        Task<bool> RunAutoModeAsync(EventHandler<DeviceInputEventArgs> eventHandler);
        Task<bool> RunFreeMovementAsync();
        Task<bool> RotateAsync(int angle, EventHandler<DeviceInputEventArgs> eventHandler);
        Task<bool> SetAccelerationAsync(int acceleration);
        Task<bool> SetDelayAsync(int delay);
        Task<bool> SetExposureAsync(int exposure);
        Task<bool> SetStepsAsync(int steps);
        Task<bool> StopAsync();
    }
}