﻿using Android.Bluetooth;
using Java.Util;
using Plugin.BLE.Abstractions.Contracts;
using RotatingTable.Xamarin.Models;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Services
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothSocket _socket;
        private static readonly UUID MY_UUID = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

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

        public bool IsConnected => _socket != null && _socket.IsConnected;

        public int Steps { get; set; }
        public int Acceleration { get; set; }
        public int Exposure { get; set; }
        public int Delay { get; set; }
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

            await WriteAsync(Commands.Status);
            var response = await ReadAsync();
            bool success = response == "READY";
            if (success)
            {
                var service = DependencyService.Resolve<IConfigService>();
                await service.SetMacAddressAsync(nativeDevice.Address);
            }

            return success && await ReadConfigurationAsync();
        }

        public async Task WriteAsync(string text)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Bluetooth service is not connected");

            var stream = _socket.OutputStream;

            // Create the string that we will send
            var message = new Java.Lang.String(text);
            // Convert it to bytes
            byte[] msgBuffer = message.GetBytes();
            // Write the array we just generated to the buffer
            await stream.WriteAsync(msgBuffer, 0, msgBuffer.Length);
        }

        public async Task<string> ReadAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Bluetooth service is not connected");

            var stream = _socket.InputStream;
            byte[] buffer = new byte[1024];
            string result = null;
            try
            {
                // Read the input buffer and allocate the number of incoming bytes
                var bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
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

        private string UnsafeAsciiBytesToString(byte[] buffer)
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

        private async Task<bool> ReadConfigurationAsync()
        {
            await WriteAsync(Commands.GetSteps);
            var response = await ReadAsync();
            if (!int.TryParse(response, out int steps))
                return false;

            await WriteAsync(Commands.GetAcceleration);
            response = await ReadAsync();
            if (!int.TryParse(response, out int acceleration))
                return false;

            await WriteAsync(Commands.GetExposure);
            response = await ReadAsync();
            if (!int.TryParse(response, out int exposure))
                return false;

            await WriteAsync(Commands.GetDelay);
            response = await ReadAsync();
            if (!int.TryParse(response, out int delay))
                return false;

            Steps = steps;
            Acceleration = acceleration;
            Exposure = exposure;
            Delay = delay;

            return true;
        }
    }
}