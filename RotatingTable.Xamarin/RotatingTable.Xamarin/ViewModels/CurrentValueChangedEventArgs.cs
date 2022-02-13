using System;

namespace RotatingTable.Xamarin.ViewModels
{
    public class CurrentValueChangedEventArgs : EventArgs
    {
        public CurrentValueChangedEventArgs(int oldValue, int newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public int OldValue { private set; get; }
        public int NewValue { private set; get; }
    }
}
