using System;

namespace RotatingTable.Xamarin.Services
{
    public class DeviceInputEventArgs : EventArgs
    {
        public DeviceInputEventArgs(string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }
}