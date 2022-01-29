using Plugin.BLE.Abstractions.Contracts;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Services
{
    public interface IBluetoothService
    {
        IDevice Device { get; set; }

        Task<bool> ConnectAsync(string address);
        string Read();
        void Write(string text);
    }
}