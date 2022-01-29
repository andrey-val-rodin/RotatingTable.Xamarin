using Android.App;
using Android.Bluetooth;
using Android.Content;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Services
{
    public class StubDevice : IDevice
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? Address { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public System.Guid Id { get; set; }
        public string Name { get; set; }
        public int Rssi { get; set; }
        public object NativeDevice { get; set; }
        public DeviceState State { get; set; }
        public IList<AdvertisementRecord> AdvertisementRecords => new List<AdvertisementRecord>();

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task<IService> GetServiceAsync(System.Guid id, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IService>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> RequestMtuAsync(int requestValue)
        {
            throw new System.NotImplementedException();
        }

        public bool UpdateConnectionInterval(ConnectionInterval interval)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> UpdateRssiAsync()
        {
            throw new System.NotImplementedException();
        }
    }

    public class StubGattCallback : BluetoothGattCallback
    {
    }

    public class TemporaryAdapter : IAdapter
    {
        private readonly InternalReceiver _receiver;
        private CancellationTokenSource _scanCancellationTokenSource;

        public bool IsScanning { get; set; }
        public int ScanTimeout { get; set; }
        public Plugin.BLE.Abstractions.Contracts.ScanMode ScanMode { get; set; }
        public IReadOnlyList<IDevice> DiscoveredDevices => new List<IDevice>();
        public IReadOnlyList<IDevice> ConnectedDevices => new List<IDevice>();

        public TemporaryAdapter()
        {
            _receiver = new InternalReceiver(this);
        }

        public event System.EventHandler<DeviceEventArgs> DeviceAdvertised;
        public event System.EventHandler<DeviceEventArgs> DeviceDiscovered;
        public event System.EventHandler<DeviceEventArgs> DeviceConnected;
        public event System.EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event System.EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;
        public event System.EventHandler ScanTimeoutElapsed;

        public Task ConnectToDeviceAsync(IDevice device, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<IDevice> ConnectToKnownDeviceAsync(System.Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DisconnectDeviceAsync(IDevice device)
        {
            throw new System.NotImplementedException();
        }

        public IReadOnlyList<IDevice> GetSystemConnectedOrPairedDevices(System.Guid[] services = null)
        {
            throw new System.NotImplementedException();
        }

        public async Task StartScanningForDevicesAsync(System.Guid[] serviceUuids = null, System.Func<IDevice, bool> deviceFilter = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
        {
            if (IsScanning)
            {
                Trace.Message("Adapter: Already scanning!");
                return;
            }

            IsScanning = true;
            _scanCancellationTokenSource = new CancellationTokenSource();

            try
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                if (!adapter.Enable())
                    return;

                Application.Context.RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionFound));

                using (cancellationToken.Register(() => _scanCancellationTokenSource?.Cancel()))
                {
                    adapter.StartDiscovery();
                    await Task.Delay(ScanTimeout, _scanCancellationTokenSource.Token);
                    Trace.Message("Adapter: Scan timeout has elapsed.");
                    ScanTimeoutElapsed?.Invoke(this, new System.EventArgs());
                }
            }
            catch (TaskCanceledException)
            {
                Trace.Message("Adapter: Scan was cancelled.");
            }
            finally
            {
                IsScanning = false;
            }
        }

        public Task StopScanningForDevicesAsync()
        {
            throw new System.NotImplementedException();
        }

        private class InternalReceiver : BroadcastReceiver
        {
            private readonly TemporaryAdapter _adapter;

            public InternalReceiver(TemporaryAdapter adapter)
            {
                _adapter = adapter;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                var action = intent.Action;

                if (action != BluetoothDevice.ActionFound)
                    return;

                // Get the device
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

                if (device.BondState != Bond.Bonded)
                {
                    _adapter.DeviceDiscovered?.Invoke(this, new DeviceEventArgs
                    {
                        Device = new StubDevice
                        {
                            Name = device.Name,
                            Address = device.Address,
                            NativeDevice = device
                        }
                    });
                }
            }
        }
    }
}
