using System;

namespace RotatingTable.Xamarin.ViewModels
{
    public class CurrentPosChangedEventArgs : EventArgs
    {
        public CurrentPosChangedEventArgs(int oldPos, int newPos)
        {
            OldPos = oldPos;
            NewPos = newPos;
        }

        public int OldPos { private set; get; }
        public int NewPos { private set; get; }
    }
}
