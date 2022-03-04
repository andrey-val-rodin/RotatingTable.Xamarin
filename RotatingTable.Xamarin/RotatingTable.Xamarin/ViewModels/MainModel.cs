using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class MainModel : NotifyPropertyChangedImpl
    {
        public event CurrentStepChangedEventHandler CurrentStepChanged;
        public event CurrentPosChangedEventHandler CurrentPosChanged;
        public event StopEventHandler Stop;

        public static readonly int[] StepValues =
            { 2, 4, 5, 6, 8, 9, 10, 12, 15, 18, 20, 24, 30, 36, 40, 45, 60, 72, 90, 120, 180, 360 };

        private bool _isConnected;
        private bool _isRunning;
        private bool _isBusy;
        private int _currentMode;
        private int _currentStep;
        private int _currentPos;
        private int _stepsIndex;
        private int _acceleration;
        private int _exposure;
        private int _delay;

        public MainModel()
        {
            RunCommand = new Command(async () => await RunAsync());
            StopCommand = new Command(async () => await StopAsync());
            ChangeStepsCommand = new Command(async () => await ChangeStepsAsync());
            ChangeAccelerationCommand = new Command(async () => await ChangeAccelerationAsync());
            ChangeExposureCommand = new Command(async () => await ChangeExposureAsync());
            ChangeDelayCommand = new Command(async () => await ChangeDelayAsync());
        }

        // order as in Mode enum
        public string[] Modes => new[]
            {
                "Авто",
                "Ручной",
                "Безостановочный",
                "Видео",
                "Поворот 90°",
                "Свободное перемещение"
            };

        public IBluetoothService Service { get => DependencyService.Resolve<IBluetoothService>(); }

        public IConfigService ConfigService { get => DependencyService.Resolve<IConfigService>(); }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value)) 
                {
                    OnPropertyChanged("IsReady");
                    OnPropertyChanged(nameof(ShowPWMChanging));
                    OnPropertyChanged(nameof(ShowSteps));
                    OnPropertyChanged(nameof(ShowAcceleration));
                    OnPropertyChanged(nameof(ShowExposure));
                    OnPropertyChanged(nameof(ShowDelay));
                }
            }
        }

        public bool IsReady
        {
            get => !IsRunning;
        }

        public int CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                var oldValue = _currentStep;
                if (SetProperty(ref _currentStep, value))
                    CurrentStepChanged?.Invoke(this, new CurrentValueChangedEventArgs(oldValue, _currentStep));
            }
        }

        public int CurrentPos
        {
            get => _currentPos;
            set
            {
                var oldValue = _currentPos;
                if (SetProperty(ref _currentPos, value))
                    CurrentPosChanged?.Invoke(this, new CurrentValueChangedEventArgs(oldValue, _currentPos));
            }
        }

        public int StepsIndex
        {
            get => _stepsIndex;
            set
            {
                if (SetProperty(ref _stepsIndex, value))
                    OnPropertyChanged(nameof(Steps));
            }
        }

        public int Steps
        {
            get
            {
                return StepValues[StepsIndex];
            }
        }

        public int Acceleration
        {
            get => _acceleration;
            set
            {
                if (SetProperty(ref _acceleration, value))
                    OnPropertyChanged(nameof(AccelerationText));
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
                    OnPropertyChanged(nameof(ExposureText));
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
                    OnPropertyChanged(nameof(DelayText));
            }
        }

        public string DelayText
        {
            get => (Delay * 100).ToString();
        }

        public bool ShowPWMChanging
        {
            get => IsRunning &&
                CurrentMode == (int)Mode.Video;
        }

        public bool ShowSteps
        {
            get => !IsRunning ||
                CurrentMode == (int)Mode.Auto ||
                CurrentMode == (int)Mode.Manual ||
                CurrentMode == (int)Mode.Nonstop;
        }

        public bool ShowAcceleration
        {
            get => !IsRunning ||
                CurrentMode != (int)Mode.Video;
        }

        public bool ShowExposure
        {
            get => !IsRunning ||
                CurrentMode == (int)Mode.Auto ||
                CurrentMode == (int)Mode.Manual;
        }

        public bool ShowDelay
        {
            get => !IsRunning ||
                CurrentMode == (int)Mode.Auto;
        }

        public Command RunCommand { get; }
        public Command StopCommand { get; }
        public Command ChangeStepsCommand { get; }
        public Command ChangeAccelerationCommand { get; }
        public Command ChangeExposureCommand { get; }
        public Command ChangeDelayCommand { get; }

        public async Task InitAsync()
        {
            IsConnected = Service.IsConnected;

            var steps = await ConfigService.GetStepsAsync();
            var acceleration = await ConfigService.GetAccelerationAsync();
            var delay = await ConfigService.GetDelayAsync();
            var exposure = await ConfigService.GetExposureAsync();

            StepsIndex = Array.FindIndex(MainModel.StepValues, e => e == steps);
            Acceleration = acceleration;
            Exposure = exposure / 100;
            Delay = delay / 100;
        }

        private async Task RunAsync()
        {
            if (_isBusy || Service.IsRunning)
                return;

            _isBusy = true;
            try
            {
                switch (CurrentMode)
                {
                    case (int)Mode.Auto:
                        IsRunning = await Service.RunAutoModeAsync((s, a) => OnDataReseived(a.Text));
                        break;

                    case (int)Mode.Rotate90:
                    case (int)Mode.FreeMovement:
                        IsRunning = await Service.RunFreeMovementAsync();
                        break;

                    case (int)Mode.Video:
                        IsRunning = await Service.RunVideoAsync((s, a) => OnDataReseived(a.Text));
                        break;

                    case (int)Mode.Manual:
                    case (int)Mode.Nonstop:
                    default:
                        await Application.Current.MainPage.DisplayAlert("",
                            "Не поддерживается пока", "OK");
                        break;
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        public void OnDataReseived(string text)
        {
            if (text.StartsWith(Commands.Step))
            {
                CurrentStep = int.TryParse(text.Substring(Commands.Step.Length), out int i) ? i : 0;
            }
            else if (text.StartsWith(Commands.Position))
            {
                CurrentPos = int.TryParse(text.Substring(Commands.Position.Length), out int i) ? i : 0;
            }
            else if (text == Commands.End)
            {
                // finished
                IsRunning = false;
                CurrentStep = 0;
                CurrentPos = 0;
            }
        }

        private async Task StopAsync()
        {
            if (_isBusy)
                return;

            _isBusy = true;
            try
            {
                CurrentStep = 0;
                CurrentPos = 0;
                if (await Service.StopAsync())
                    IsRunning = false;
                else
                {
                    //TODO what to do here?
                }
                Stop?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task ChangeStepsAsync()
        {
            var oldSteps = await ConfigService.GetStepsAsync();
            var newSteps = StepValues[StepsIndex];
            if (newSteps == oldSteps)
                return;

            if (_isBusy)
            {
                await InitAsync(); // back to old value (restore defaults)
                return;
            }

            _isBusy = true;
            bool success = false;
            try
            {
                if (!ConfigValidator.IsStepsValid(Steps))
                    return;

                if (await Service.SetStepsAsync(newSteps))
                {
                    // Store in persistent memory
                    await ConfigService.SetStepsAsync(newSteps);
                    success = true;
                }
            }
            finally
            {
                _isBusy = false;
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeAccelerationAsync()
        {
            var oldAcceleration = await ConfigService.GetAccelerationAsync();
            var newAcceleration = Acceleration;
            if (newAcceleration == oldAcceleration)
                return;

            if (_isBusy)
            {
                await InitAsync(); // back to old value (restore defaults)
                return;
            }

            _isBusy = true;
            bool success = false;
            try
            {
                if (!ConfigValidator.IsAccelerationValid(newAcceleration))
                    return;

                if (await Service.SetAccelerationAsync(newAcceleration))
                {
                    // Store in persistent memory
                    await ConfigService.SetAccelerationAsync(newAcceleration);
                    success = true;
                }
            }
            finally
            {
                _isBusy = false;
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeExposureAsync()
        {
            var oldExposure = await ConfigService.GetExposureAsync();
            var newExposure = Exposure * 100;
            if (newExposure == oldExposure)
                return;

            if (_isBusy)
            {
                await InitAsync(); // back to old value (restore defaults)
                return;
            }

            _isBusy = true;
            bool success = false;
            try
            {
                if (!ConfigValidator.IsExposureValid(newExposure))
                    return;

                if (await Service.SetExposureAsync(newExposure))
                {
                    // Store in persistent memory
                    await ConfigService.SetExposureAsync(newExposure);
                    success = true;
                }
            }
            finally
            {
                _isBusy = false;
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeDelayAsync()
        {
            var oldDelay = await ConfigService.GetDelayAsync();
            var newDelay = Delay * 100;
            if (newDelay == oldDelay)
                return;

            if (_isBusy)
            {
                await InitAsync(); // back to old value (restore defaults)
                return;
            }

            _isBusy = true;
            bool success = false;
            try
            {
                if (!ConfigValidator.IsDelayValid(newDelay))
                    return;

                if (await Service.SetDelayAsync(newDelay))
                {
                    // Store in persistent memory
                    await ConfigService.SetDelayAsync(newDelay);
                    success = true;
                }
            }
            finally
            {
                _isBusy = false;
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }
    }
}