using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class MainModel : NotifyPropertyChangedImpl
    {
        public enum Mode
        {
            Auto = 0,
            Manual = 1,
            Nonstop = 2,
            Video = 3,
            Rotate = 4
        };

        public static readonly int[] StepValues =
            { 2, 4, 5, 6, 8, 9, 10, 12, 15, 18, 20, 24, 30, 36, 40, 45, 60, 72, 90, 120, 180, 360 };

        private int _currentMode;
        private bool _isRunning;
        private string _currentStep;
        private int _steps;
        private int _acceleration;
        private int _exposure;
        private int _delay;

        // order as in Mode enum
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

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                    OnPropertyChanged("IsReady");
            }
        }

        public string CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public bool IsReady
        {
            get => !IsRunning;
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
        public Command StopCommand { get; }
        public Command ChangeStepsCommand { get; }
        public Command ChangeAccelerationCommand { get; }
        public Command ChangeExposureCommand { get; }
        public Command ChangeDelayCommand { get; }

        public MainModel()
        {
            RunCommand = new Command(async () => await RunAsync());
            StopCommand = new Command(() => Stop());
            ChangeStepsCommand = new Command(async () => await ChangeStepsAsync());
            ChangeAccelerationCommand = new Command(async () => await ChangeAccelerationAsync());
            ChangeExposureCommand = new Command(async () => await ChangeExposureAsync());
            ChangeDelayCommand = new Command(async () => await ChangeDelayAsync());
        }

        private async Task RunAsync()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            string response;
            switch (CurrentMode)
            {
                case (int)Mode.Auto:
                    await service.WriteAsync(Commands.RunAutoMode);
                    response = await service.ReadAsync();
                    IsRunning = response == "OK";
                    service.BeginListening((s, a) => OnDataReseived(a.Text));
                    break;

                case (int)Mode.Manual:
                case (int)Mode.Nonstop:
                case (int)Mode.Video:
                case (int)Mode.Rotate:
                default:
                    await Application.Current.MainPage.DisplayAlert("",
                        "Не поддерживается пока", "OK");
                    break;
            }
        }

        public void OnDataReseived(string text)
        {
            Console.WriteLine($"Received text: '{text}'");
            if (text.StartsWith("STEP "))
            {
                CurrentStep = text.Substring(5);
            }
            else if (text == "END")
            {
                // finished
                var service = DependencyService.Resolve<IBluetoothService>();
                service.EndListening();
                CurrentStep = string.Empty;
                IsRunning = false;
            }
        }

        private void Stop()
        {
            //TODO
            IsRunning = false;
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