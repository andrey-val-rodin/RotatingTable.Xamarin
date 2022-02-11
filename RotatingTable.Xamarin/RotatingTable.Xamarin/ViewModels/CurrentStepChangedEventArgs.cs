using System;

namespace RotatingTable.Xamarin.ViewModels
{
    public class CurrentStepChangedEventArgs : EventArgs
    {
        public CurrentStepChangedEventArgs(int oldStep, int newStep)
        {
            OldStep = oldStep;
            NewStep = newStep;
        }

        public int OldStep { private set; get; }
        public int NewStep { private set; get; }
    }
}
