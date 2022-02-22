using Plugin.BLE.Abstractions.Contracts;
using System;

namespace RotatingTable.Xamarin.ViewModels
{
    public class DeviceItem
    {
        public DeviceItem(IDevice device)
        {
            Device = device;
        }

        public IDevice Device { get; private set; }
        public Guid Id => Device.Id;
        public string Name => Device.Name;

        public void Update(IDevice newDevice)
        {
            Device = newDevice ?? throw new ArgumentNullException(nameof(newDevice));
        }
    }
}
