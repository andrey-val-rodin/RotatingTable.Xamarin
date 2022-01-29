using Android.Bluetooth;
using Plugin.BLE.Abstractions.Contracts;
using RotatingTable.Xamarin.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class ConnectModel : NotifyPropertyChangedImpl
    {
        private bool _isBusy = false;
        private string _deviceName;
        private readonly ObservableCollection<string> _deviceNames = new ObservableCollection<string>();
        private readonly List<IDevice> _devices = new List<IDevice>();

        private IAdapter _adapter;
        public IAdapter Adapter
        {
            get
            {
                if (_adapter == null)
                {
                    _adapter = new TemporaryAdapter();
                }
                return _adapter;
                /*
                var adapter = CrossBluetoothLE.Current.Adapter;
                BluetoothAdapter nativeAdapter = GetPrivateField<BluetoothAdapter>(adapter, "_bluetoothAdapter");
                return CrossBluetoothLE.Current.Adapter;
                */
            }
        }

        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        public ObservableCollection<string> DeviceNames { get { return _deviceNames; } }

        public IReadOnlyList<IDevice> Devices => _devices;

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public Command RefreshCommand { get; }

        public ConnectModel()
        {
            RefreshCommand = new Command(async () =>
                await Scan());
        }

        private async Task<bool> Scan()
        {
            _isBusy = true;
            try
            {
                _devices.Clear();
                DeviceNames.Clear();

                Adapter.ScanTimeout = 10000;
                //Adapter.ScanMode = ScanMode.LowPower;
                Adapter.ScanTimeoutElapsed += (s, a) => IsBusy = false;
                Adapter.DeviceDiscovered += (s, a) =>
                {
                    _devices.Add(a.Device);
                    DeviceNames.Add(GetDeviceName(a.Device));
                };

                await Adapter.StartScanningForDevicesAsync();
                return true;
            }
            catch(Exception e)
            {
                IsBusy = false;
                await Application.Current.MainPage.DisplayAlert("Что-то пошло не так...", e.Message, "OK");
                return false;
            }
        }

        private string GetDeviceName(IDevice device)
        {
            return string.IsNullOrEmpty(device.Name) ? "?" : device.Name;
        }

        public void CancelScan()
        {
            BluetoothAdapter.DefaultAdapter.CancelDiscovery();
            IsBusy = false;
        }
    }
}