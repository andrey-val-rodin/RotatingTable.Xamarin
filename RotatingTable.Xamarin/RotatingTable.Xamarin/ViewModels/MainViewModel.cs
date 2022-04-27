using RotatingTable.Xamarin.Handlers;
using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.ViewModels
{
    public class MainViewModel : NotifyPropertyChangedImpl
    {
        public event CurrentStepChangedEventHandler CurrentStepChanged;
        public event CurrentPosChangedEventHandler CurrentPosChanged;
        public event StopEventHandler Stop;
        public event WaitingTimeoutHandler WaitingTimeout;

        private bool _isConnected;
        private bool _isRunning;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private int _currentMode;
        private int _currentStep;
        private int _currentPos;
        private int _stepsIndex;
        private int _acceleration;
        private int _exposure;
        private int _delay;
        private string _stopButtonText;
        private bool _isSoftStopping;
        private bool _performingManualStep;
        private ChangePWM _changingPWM = ChangePWM.None;
        private readonly object _locker = new();

        public MainViewModel()
        {
            StopButtonText = "Стоп";
            RunCommand = new Command(async () => await RunAsync());
            StopCommand = new Command(async () => await StopAsync());
            NextCommand = new Command(async () => await NextAsync());
            PhotoCommand = new Command(async () => await PhotoAsync());
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

        public IConfig Config { get => DependencyService.Resolve<IConfig>(); }

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
                    OnPropertyChanged(nameof(Info));
                    OnPropertyChanged(nameof(ShowPWMChanging));
                    OnPropertyChanged(nameof(ShowManualButtons));
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

        public string Info
        {
            get
            {
                switch (CurrentMode)
                {
                    case (int)Mode.Auto:
                    case (int)Mode.Manual:
                    case (int)Mode.Nonstop:
                        return $"{Modes[CurrentMode]} ({Steps})";

                    case (int)Mode.Rotate90:
                    case (int)Mode.FreeMovement:
                    case (int)Mode.Video:
                        return Modes[CurrentMode];

                    default:
                        throw new InvalidOperationException($"Invalid CurrentMode: {CurrentMode}");
                }
            }
        }

        public int Steps
        {
            get
            {
                return ConfigValidator.StepValues[StepsIndex];
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
                (CurrentMode == (int)Mode.Video ||
                CurrentMode == (int)Mode.Nonstop);
        }

        public bool ShowManualButtons
        {
            get => IsRunning &&
                CurrentMode == (int)Mode.Manual;
        }

        public bool ShowSteps
        {
            get => !IsRunning;
        }

        public bool ShowAcceleration
        {
            get => !IsRunning ||
                CurrentMode == (int)Mode.Auto ||
                CurrentMode == (int)Mode.Manual;
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

        public ChangePWM ChangingPWM
        {
            get
            {
                lock (_locker)
                {
                    return _changingPWM;
                }
            }
            set
            {
                lock (_locker)
                {
                    _changingPWM = value;
                }
            }
        }

        private bool IsSoftStopping
        {
            get => _isSoftStopping;
            set
            {
                StopButtonText = value ? "Остановка..." : "Стоп";
                _isSoftStopping = value;
            }
        }
        public string StopButtonText
        {
            get => _stopButtonText;
            set => SetProperty(ref _stopButtonText, value);
        }


        public Command RunCommand { get; }
        public Command StopCommand { get; }
        public Command NextCommand { get; }
        public Command PhotoCommand { get; }
        public Command ChangeStepsCommand { get; }
        public Command ChangeAccelerationCommand { get; }
        public Command ChangeExposureCommand { get; }
        public Command ChangeDelayCommand { get; }

        public async Task InitAsync()
        {
            IsConnected = Service.IsConnected;

            var steps = await Config.GetStepsAsync();
            var acceleration = await Config.GetAccelerationAsync();
            var delay = await Config.GetDelayAsync();
            var exposure = await Config.GetExposureAsync();

            StepsIndex = Array.FindIndex(ConfigValidator.StepValues, e => e == steps);
            Acceleration = acceleration;
            Exposure = exposure / 100;
            Delay = delay / 100;
        }

        private async Task RunAsync()
        {
            await _semaphore.WaitAsync();
            CurrentPos = 0;
            try
            {
                switch (CurrentMode)
                {
                    case (int)Mode.Auto:
                        IsRunning = await Service.RunAutoAsync((s, a) => OnDataReseived(a.Text));
                        break;

                    case (int)Mode.Manual:
                        CurrentStep = 1;
                        _performingManualStep = false;
                        IsRunning = await Service.RunManualAsync();
                        break;

                    case (int)Mode.Nonstop:
                        IsRunning = await Service.RunNonStopAsync(async (s, a) => await OnNonstopDataReseivedAsync(a.Text));
                        break;

                    case (int)Mode.Rotate90:
                    case (int)Mode.FreeMovement:
                        IsRunning = await Service.RunFreeMovementAsync();
                        break;

                    case (int)Mode.Video:
                        IsRunning = await Service.RunVideoAsync((s, a) => OnDataReseived(a.Text));
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid CurrentMode: {CurrentMode}");
                }
            }
            finally
            {
                _semaphore.Release();
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
                Stop?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task OnNonstopDataReseivedAsync(string text)
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

                // Store nonstop frequency
                var nonstopFrequency = await Service.GetNonstopFrequencyAsync();
                if (nonstopFrequency != null)
                    await Config.SetNonstopFrequencyAsync(nonstopFrequency.Value);
            }
        }

        private async Task StopAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsRunning)
                    return;

                CurrentStep = 0;
                CurrentPos = 0;
                if (CurrentMode == (int)Mode.Video)
                {
                    if (!IsSoftStopping)
                    {
                        if (!await Service.SoftStopAsync())
                        {
                            await Service.StopAsync();
                            return;
                        }

                        IsSoftStopping = true;
                        Service.BeginWaitingForStop(
                            async (s, a) =>
                            {
                                IsSoftStopping = false;
                                IsRunning = false;
                                Stop?.Invoke(this, EventArgs.Empty);

                                // Store current PWM
                                var videoPWM = await Service.GetVideoPWMAsync();
                                if (videoPWM != null)
                                    await Config.SetVideoPWMAsync(videoPWM.Value);
                            },
                            async (s, a) =>
                            {
                                IsSoftStopping = false;
                                IsRunning = false;
                                await Service.StopAsync();
                                bool ready = await Service.GetStatusAsync() == Commands.Ready;
                                if (ready)
                                    Stop?.Invoke(this, EventArgs.Empty);
                                else
                                    WaitingTimeout?.Invoke(this, EventArgs.Empty);
                            });
                    }
                    else
                    {
                        Service.EndWaitingForStop();
                        await Service.StopAsync();
                        IsSoftStopping = false;
                        IsRunning = false;
                        Stop?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    await Service.StopAsync();
                    IsRunning = false;
                    Stop?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task NextAsync()
        {
            if (_performingManualStep)
                return;

            if (CurrentMode != (int)Mode.Manual)
                throw new InvalidOperationException("Invalid mode");

            if (await Service.NextAsync((s, a) => OnManualStepDataReseived(a.Text)))
            {
                IsRunning = true;
                _performingManualStep = true;
            }
        }

        public async void OnManualStepDataReseived(string text)
        {
            if (text.StartsWith(Commands.Step))
            {
                CurrentStep = int.TryParse(text.Substring(Commands.Step.Length), out int i) ? i : 0;
                CurrentPos = 0;
                _performingManualStep = false;
            }
            else if (text.StartsWith(Commands.Position))
            {
                CurrentPos = int.TryParse(text.Substring(Commands.Position.Length), out int i) ? i : 0;
            }
            else if (text == Commands.End)
            {
                // finished
                IsRunning = false;
                _performingManualStep = false;
                CurrentStep = 0;
                CurrentPos = 0;

                // Store current frequency
                var nonstopFrequency = await Service.GetNonstopFrequencyAsync();
                if (nonstopFrequency != null)
                    await Config.SetNonstopFrequencyAsync(nonstopFrequency.Value);
            }
        }

        private async Task PhotoAsync()
        {
            if (CurrentMode != (int)Mode.Manual)
                throw new InvalidOperationException("Invalid mode");

            await Service.PhotoAsync();
        }

        private async Task ChangeStepsAsync()
        {
            var oldSteps = await Config.GetStepsAsync();
            var newSteps = ConfigValidator.StepValues[StepsIndex];
            if (newSteps == oldSteps)
                return;

            await _semaphore.WaitAsync();
            bool success = false;
            try
            {
                if (!ConfigValidator.IsStepsValid(Steps))
                    return;

                if (await Service.SetStepsAsync(newSteps))
                {
                    // Store in persistent memory
                    await Config.SetStepsAsync(newSteps);
                    success = true;
                }
            }
            finally
            {
                _semaphore.Release();
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeAccelerationAsync()
        {
            var oldAcceleration = await Config.GetAccelerationAsync();
            var newAcceleration = Acceleration;
            if (newAcceleration == oldAcceleration)
                return;

            await _semaphore.WaitAsync();
            bool success = false;
            try
            {
                if (!ConfigValidator.IsAccelerationValid(newAcceleration))
                    return;

                if (await Service.SetAccelerationAsync(newAcceleration))
                {
                    // Store in persistent memory
                    await Config.SetAccelerationAsync(newAcceleration);
                    success = true;
                }
            }
            finally
            {
                _semaphore.Release();
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeExposureAsync()
        {
            var oldExposure = await Config.GetExposureAsync();
            var newExposure = Exposure * 100;
            if (newExposure == oldExposure)
                return;

            await _semaphore.WaitAsync();
            bool success = false;
            try
            {
                if (!ConfigValidator.IsExposureValid(newExposure))
                    return;

                if (await Service.SetExposureAsync(newExposure))
                {
                    // Store in persistent memory
                    await Config.SetExposureAsync(newExposure);
                    success = true;
                }
            }
            finally
            {
                _semaphore.Release();
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }

        private async Task ChangeDelayAsync()
        {
            var oldDelay = await Config.GetDelayAsync();
            var newDelay = Delay * 100;
            if (newDelay == oldDelay)
                return;

            await _semaphore.WaitAsync();
            bool success = false;
            try
            {
                if (!ConfigValidator.IsDelayValid(newDelay))
                    return;

                if (await Service.SetDelayAsync(newDelay))
                {
                    // Store in persistent memory
                    await Config.SetDelayAsync(newDelay);
                    success = true;
                }
            }
            finally
            {
                _semaphore.Release();
                if (!success)
                    await InitAsync(); // back to old value (restore defaults)
            }
        }
    }
}