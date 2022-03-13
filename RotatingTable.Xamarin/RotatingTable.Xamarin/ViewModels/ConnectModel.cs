using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using RotatingTable.Xamarin.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class ConnectModel : NotifyPropertyChangedImpl
    {
        private bool _isBusy = false;
        private bool _isBluetoothEnabled;
        private bool _isBluetoothDisabled;
        private string _deviceName;
        private CancellationTokenSource _cancellationTokenSource;

        public ConnectModel()
        {
            _isBluetoothEnabled = BluetoothLE.IsOn;
            _isBluetoothDisabled = !_isBluetoothEnabled;
            BluetoothLE.StateChanged += OnStateChanged;
            RefreshCommand = new Command(async () =>
                await ScanAsync());
        }

        private IBluetoothLE BluetoothLE => CrossBluetoothLE.Current;

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;

        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        public ObservableCollection<DeviceItem> Devices { get; set; } = new ObservableCollection<DeviceItem>();

        public bool IsBluetoothEnabled
        {
            get => _isBluetoothEnabled;
            set => SetProperty(ref _isBluetoothEnabled, value);
        }

        public bool IsBluetoothDisabled
        {
            get => _isBluetoothDisabled;
            set => SetProperty(ref _isBluetoothDisabled, value);
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public Command RefreshCommand { get; }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e)
        {
            IsBluetoothEnabled = e.NewState == BluetoothState.On;
            IsBluetoothDisabled = !_isBluetoothEnabled;
        }

        public async Task ScanAsync()
        {
            Devices.Clear();

            foreach (var connectedDevice in Adapter.ConnectedDevices)
            {
                AddOrUpdateDevice(connectedDevice);
            }

            Adapter.ScanTimeout = 3000;
            Adapter.ScanMode = ScanMode.LowLatency;
            Adapter.ScanTimeoutElapsed += (s, a) => IsBusy = false;
            Adapter.DeviceDiscovered += (s, a) => AddOrUpdateDevice(a.Device);
            _cancellationTokenSource = new CancellationTokenSource();
            await Adapter.StartScanningForDevicesAsync(cancellationToken: _cancellationTokenSource.Token);
        }

        private void AddOrUpdateDevice(IDevice device)
        {
            if (string.IsNullOrEmpty(device?.Name))
                return;

            var item = Devices.FirstOrDefault(d => d.Device.Id == device.Id);
            if (item != null)
                item.Update(device);
            else
                Devices.Add(new DeviceItem(device));
        }
    }
}