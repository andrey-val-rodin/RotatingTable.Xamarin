using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Services
{
    public interface IBluetoothService
    {
        bool IsConnected { get; }
        IDevice Device { get; set; }
        int Acceleration { get; set; }
        int Steps { get; set; }
        int Exposure { get; set; }
        int Delay { get; set; }

        Task<bool> ConnectAsync(string address);
        Task<string> ReadAsync();
        Task WriteAsync(string text);
        void BeginListening(EventHandler<DeviceInputEventArgs> eventHandler);
        void EndListening();
    }
}