using System;
using System.IO;

namespace RotatingTable.Xamarin.Services
{
    public class ListeningStream
    {
        private MemoryStream _internalStream = new();
        private byte[] _buffer = new byte[1000];
        private bool disposedValue;

        public long Length
        {
            get => _internalStream.Length;
        }

        public void Append(byte[] bytes, EventHandler<DeviceInputEventArgs> eventHandler)
        {
            System.Diagnostics.Debug.WriteLine("Stream: " +
                UnsafeAsciiBytesToString(bytes).Replace(BluetoothService.Terminator, '|'));

            _internalStream.Write(bytes, 0, bytes.Length);
            Parse(eventHandler);
        }

        private void Parse(EventHandler<DeviceInputEventArgs> eventHandler)
        {
            _internalStream.Position = 0;
            int current = 0;
            int bytesToRemove = 0;
            while (true)
            {
                var b = _internalStream.ReadByte();
                if (b < 0)
                {
                    // End of stream
                    if (bytesToRemove > 0)
                    {
                        byte[] buf = _internalStream.GetBuffer();
                        Buffer.BlockCopy(buf, bytesToRemove, buf, 0, (int)_internalStream.Length - bytesToRemove);
                        _internalStream.SetLength(_internalStream.Length - bytesToRemove);
                    }
                    return;
                }

                if (b == BluetoothService.Terminator)
                {
                    // Convert token to string and invoke handler
                    var text = UnsafeAsciiBytesToString(_buffer, current);
                    System.Diagnostics.Debug.WriteLine("Token: " + text);
                    eventHandler?.Invoke(this, new DeviceInputEventArgs(text));
                    bytesToRemove += current + 1;
                    current = 0;
                    continue;
                }

                _buffer[current] = (byte)b;
                current++;
            }
        }

        private string UnsafeAsciiBytesToString(byte[] buffer)
        {
            int end = 0;
            while (end < buffer.Length && buffer[end] != 0)
            {
                end++;
            }

            return UnsafeAsciiBytesToString(buffer, end);
        }

        private string UnsafeAsciiBytesToString(byte[] buffer, int length)
        {
            unsafe
            {
                fixed (byte* pAscii = buffer)
                {
                    return new string((sbyte*)pAscii, 0, length);
                }
            }
        }
    }
}
