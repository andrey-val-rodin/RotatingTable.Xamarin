using Android.Bluetooth;
using Java.Util;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Services
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothSocket _socket;
        private static UUID MY_UUID = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

        /*
        public BluetoothAdapter Adapter
        {
            get
            {
                // Temporary code
                var adapter = CrossBluetoothLE.Current.Adapter;
                BluetoothAdapter nativeAdapter = GetPrivateField<BluetoothAdapter>(adapter, "_bluetoothAdapter");
                return nativeAdapter;
            }
        }

        // Temporary code
        public static T GetPrivateField<T>(object obj, string propertyName)
        {
            return (T)obj.GetType()
                .GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(obj);
        }
        */

        public IDevice Device { get; set; }
        public bool IsConnected => _socket == null ? false : _socket.IsConnected;
        /*
        public bool Connect(IDevice device)
        {
            var nativeDevice = device.NativeDevice as BluetoothDevice;
            if (nativeDevice == null)
                return false;

            var gattCallback = new StubGattCallback();
            var bluetoothGatt = nativeDevice.ConnectGatt(Application.Context, false, gattCallback);
            return bluetoothGatt != null;
        }
        */
        public async Task<bool> ConnectAsync(string address)
        {
            // We start the connection with the arduino
            var nativeDevice = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);

            // We indicate to the adapter that it is no longer visible
            BluetoothAdapter.DefaultAdapter.CancelDiscovery();
            try
            {
                // We start the communication socket with the arduino
                _socket = nativeDevice.CreateRfcommSocketToServiceRecord(MY_UUID);
                // We connect the socket
                _socket.Connect();
                System.Console.WriteLine("Connection Successful");
            }
            catch
            {
                try
                {
                    _socket.Close();
                }
                catch {}

                return false;
            }

            Write("0");
            var response = Read();
            bool success = response == "Turn led off";
            if (success)
            {
                var service = DependencyService.Resolve<IConfigService>();
                await service.SetMacAddressAsync(nativeDevice.Address);
            }

            return success;
        }

        public void Write(string text)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Bluetooth service is not connected");

            var stream = _socket.OutputStream;

            // Create the string that we will send
            var message = new Java.Lang.String(text);
            // Convert it to bytes
            byte[] msgBuffer = message.GetBytes();
            // Write the array we just generated to the buffer
            stream.Write(msgBuffer, 0, msgBuffer.Length);
        }

        public string Read()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Bluetooth service is not connected");

            var stream = _socket.InputStream;
            byte[] buffer = new byte[1024];
            string result = null;
            try
            {
                // Read the input buffer and allocate the number of incoming bytes
                var bytes = stream.Read(buffer, 0, buffer.Length);
                // We verify that the bytes contain information
                if (bytes > 0)
                {
                        // Convert the value of the information received to a string
                        result = UnsafeAsciiBytesToString(buffer);
                }
            }
            catch (Java.IO.IOException)
            {
                return null;
            }

            return result;
        }

        public static string UnsafeAsciiBytesToString(byte[] buffer)
        {
            int end = 0;
            while (end < buffer.Length && buffer[end] != 0)
            {
                end++;
            }

            unsafe
            {
                fixed (byte* pAscii = buffer)
                {
                    return new string((sbyte*)pAscii, 0, end);
                }
            }
        }
    }
}
