using Plugin.BLE.Abstractions.Contracts;

namespace RotatingTable.Xamarin.Services
{
    public interface IBluetoothService
    {
        IDevice Device { get; set; }

        bool Connect(IDevice device);
        string Read();
        void Write(string text);
    }
}