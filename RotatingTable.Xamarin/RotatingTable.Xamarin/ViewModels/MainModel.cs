using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class MainModel : NotifyPropertyChangedImpl
    {
        public static readonly int[] StepValues =
            { 2, 4, 5, 6, 8, 9, 10, 12, 15, 18, 20, 24, 30, 36, 40, 45, 60, 72, 90, 120, 180, 360 };

        private int _currentMode;
        private int _steps;
        private int _acceleration;
        private int _exposure;
        private int _delay;

        public string[] Modes => new[]
            {
                "Авто",
                "Ручной",
                "Безостановочный",
                "Видео",
                "Поворот 90°"
            };

        public int CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public int Steps
        {
            get => _steps;
            set
            {
                if (SetProperty(ref _steps, value))
                    OnPropertyChanged("StepsText");
            }
        }

        public string StepsText
        {
            get
            {
                return CheckSteps()
                    ? StepValues[Steps].ToString()
                    : string.Empty;
            }
        }

        public int Acceleration
        {
            get => _acceleration;
            set
            {
                if (SetProperty(ref _acceleration, value))
                    OnPropertyChanged("AccelerationText");
            }
        }

        public string AccelerationText
        {
            get => Acceleration.ToString();
        }

        public int Exposure
        {
            get => _exposure;
            set
            {
                if (SetProperty(ref _exposure, value))
                    OnPropertyChanged("ExposureText");
            }
        }

        public string ExposureText
        {
            get => (Exposure * 100).ToString();
        }

        public int Delay
        {
            get => _delay;
            set
            {
                if (SetProperty(ref _delay, value))
                    OnPropertyChanged("DelayText");
            }
        }

        public string DelayText
        {
            get => (Delay * 100).ToString();
        }

        public Command RunCommand { get; }
        public Command RunAutoCommand { get; }
        public Command ChangeStepsCommand { get; }
        public Command ChangeAccelerationCommand { get; }
        public Command ChangeExposureCommand { get; }
        public Command ChangeDelayCommand { get; }

        public MainModel()
        {
            RunCommand = new Command(async () => await RunAsync());
            ChangeStepsCommand = new Command(async () => await ChangeStepsAsync());
            ChangeAccelerationCommand = new Command(async () => await ChangeAccelerationAsync());
            ChangeExposureCommand = new Command(async () => await ChangeExposureAsync());
            ChangeDelayCommand = new Command(async () => await ChangeDelayAsync());
        }

        private async Task RunAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            switch (CurrentMode)
            {
                case 0:
                    await service.WriteAsync(Commands.RunAutoMode);
                    break;

                default:
                    await Application.Current.MainPage.DisplayAlert("",
                        "Не поддерживается пока", "OK");
                    break;
            }
        }

        private async Task ChangeStepsAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            if (!CheckSteps())
            {
                // Back to old value
                Steps = Array.FindIndex(StepValues, e => e == service.Steps);
                return;
            }

            var newSteps = StepValues[Steps];
            if (newSteps == service.Steps)
                return;

            var command = Commands.SetSteps + ' ' + newSteps.ToString();
            await service.WriteAsync(command);
            var response = await service.ReadAsync();
            if (response == "OK")
            {
                // success
                service.Steps = newSteps;
            }
            else
            {
                // Back to old value
                Steps = Array.FindIndex(StepValues, e => e == service.Steps);
            }
        }

        private async Task ChangeAccelerationAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            if (!CheckAcceleration())
            {
                // Back to old value
                Acceleration = service.Acceleration;
            }

            var newAcceleration = Acceleration;
            if (newAcceleration == service.Acceleration)
                return;

            var command = Commands.SetAcceleration + ' ' + newAcceleration.ToString();
            await service.WriteAsync(command);
            var response = await service.ReadAsync();
            if (response == "OK")
            {
                // success
                service.Acceleration = newAcceleration;
            }
            else
            {
                // Back to old value
                Acceleration = service.Acceleration;
            }
        }

        private async Task ChangeExposureAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            if (!CheckExposure())
            {
                // Back to old value
                Exposure = service.Exposure / 100;
                return;
            }

            var newExposure = Exposure * 100;
            if (newExposure == service.Exposure)
                return;

            var command = Commands.SetExposure + ' ' + newExposure.ToString();
            await service.WriteAsync(command);
            var response = await service.ReadAsync();
            if (response == "OK")
            {
                // success
                service.Exposure = newExposure;
            }
            else
            {
                // Back to old value
                Exposure = service.Exposure / 100;
            }
        }

        private async Task ChangeDelayAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            if (!CheckDelay())
            {
                // Back to old value
                Delay = service.Delay / 100;
                return;
            }

            var newDelay = Delay * 100;
            if (newDelay == service.Delay)
                return;

            var command = Commands.SetDelay + ' ' + newDelay.ToString();
            await service.WriteAsync(command);
            var response = await service.ReadAsync();
            if (response == "OK")
            {
                // success
                service.Delay = newDelay;
            }
            else
            {
                // Back to old value
                Delay = service.Delay / 100;
            }
        }

        private bool CheckSteps()
        {
            return 0 <= Steps && Steps <= StepValues.Length;
        }

        private bool CheckAcceleration()
        {
            return 1 <= Acceleration && Acceleration <= 10;
        }

        private bool CheckExposure()
        {
            return 1 <= Exposure && Exposure <= 5;
        }

        private bool CheckDelay()
        {
            return 0 <= Delay && Delay <= 50;
        }
    }
}