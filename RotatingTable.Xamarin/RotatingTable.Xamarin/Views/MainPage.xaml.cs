using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions.EventArgs;
using RotatingTable.Xamarin.Draw;
using RotatingTable.Xamarin.Handlers;
using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.TouchTracking;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly Selector _selector;
        private readonly IUserDialogs _userDialogs;
        private CancellationTokenSource _tokenSource;
        private CancellationTokenSource _alertSource;

        public MainPage()
        {
            InitializeComponent();

            _userDialogs = UserDialogs.Instance;
            _selector = new(canvasView, Model);

            Model.CurrentStepChanged += OnCurrentStepChanged;
            Model.CurrentPosChanged += OnCurrentPosChanged;

            Model.Stop += OnStop;
            Model.WaitingTimeout += OnWaitingTimeout;
            CrossBluetoothLE.Current.Adapter.DeviceConnectionLost += DeviceConnectionLost;

            Model.Service.Timeout += ServiceTimeout;
        }

        public MainViewModel Model
        {
            get => BindingContext as MainViewModel;
        }

        protected override async void OnAppearing()
        {
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (!Model.Service.IsConnected)
            {
                var id = await Model.Config.GetDeviceIdAsync();
                if (id != Guid.Empty)
                    await Model.Service.ConnectAsync(id);
            }

            await Model.InitAsync();
            if (!Model.IsConnected)
                await Shell.Current.GoToAsync("//ConnectPage");
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            _selector.GetDrawer((Mode)Model.CurrentMode).Draw(args);
        }

        private void OnCurrentStepChanged(object sender, CurrentValueChangedEventArgs args)
        {
            canvasView.InvalidateSurface();
        }

        private void OnCurrentPosChanged(object sender, CurrentValueChangedEventArgs args)
        {
            canvasView.InvalidateSurface();
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            _selector.GetDrawer((Mode)Model.CurrentMode).OnTouchEffectAction(sender, args);
        }

        private void OnStop(object sender, EventArgs args)
        {
            _selector.Clear();
        }

        private void OnWaitingTimeout(object sender, EventArgs args)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Model.Service.DisconnectAsync();
                Model.IsRunning = false;
                Model.IsConnected = false;
                _selector.Clear();
                await Shell.Current.GoToAsync("//ConnectPage");
            });
        }

        private void DeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Model.Service.DisconnectAsync();
                    Model.IsRunning = false;
                    Model.IsConnected = false;
                    _selector.Clear();

                    if (_alertSource != null)
                        _alertSource.Cancel();

                    if (Shell.Current.CurrentState.Location.ToString() != "//ConnectPage")
                    {
                        _alertSource = new CancellationTokenSource();
                        await _userDialogs.AlertAsync("Соединение со столом разорвано",
                            cancelToken: _alertSource.Token);

                        await Shell.Current.GoToAsync("//ConnectPage");
                    }
                }
                catch (TaskCanceledException) { }
                finally
                {
                    _alertSource?.Dispose();
                    _alertSource = null;
                }
            });
        }


        private void ServiceTimeout(object sender, ElapsedEventArgs args)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Model.Service.DisconnectAsync();
                    Model.IsRunning = false;
                    Model.IsConnected = false;
                    _selector.Clear();

                    if (_alertSource != null)
                        _alertSource.Cancel();

                    _alertSource = new CancellationTokenSource();
                    await _userDialogs.AlertAsync("Превышено время ожидания ответа от стола в процессе работы",
                            cancelToken: _alertSource.Token);

                    await Shell.Current.GoToAsync("//ConnectPage");
                }
                catch (TaskCanceledException) { }
                finally
                {
                    _alertSource?.Dispose();
                    _alertSource = null;
                }
            });
        }

        private void DecreasePWMButton_Pressed(object sender, EventArgs e)
        {
            if (Model.ChangingPWM != ChangePWM.None)
                return;

            Model.ChangingPWM = ChangePWM.Decrease;
            _tokenSource?.Cancel();
            _tokenSource = new();
            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested || Model.ChangingPWM != ChangePWM.Decrease)
                            break;

                        if (!await Model.Service.DecreasePWMAsync())
                            break;

                        if (token.IsCancellationRequested)
                            break;

                        await Task.Delay(200, token);
                    }
                }
                catch { }
                finally
                {
                    Model.ChangingPWM = ChangePWM.None;
                }
            });
        }

        private void DecreasePWMButton_Released(object sender, EventArgs e)
        {
            if (Model.ChangingPWM == ChangePWM.Decrease)
                Cancel();
        }

        private void IncreasePWMButton_Pressed(object sender, EventArgs e)
        {
            if (Model.ChangingPWM != ChangePWM.None)
                return;

            Model.ChangingPWM = ChangePWM.Increase;
            _tokenSource?.Cancel();
            _tokenSource = new();
            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested || Model.ChangingPWM != ChangePWM.Increase)
                            break;

                        if (!await Model.Service.IncreasePWMAsync())
                            break;

                        if (token.IsCancellationRequested)
                            break;

                        await Task.Delay(200, token);
                    }
                }
                catch { }
                finally
                {
                    Model.ChangingPWM = ChangePWM.None;
                }
            });
        }

        private void IncreasePWMButton_Released(object sender, EventArgs e)
        {
            if (Model.ChangingPWM == ChangePWM.Increase)
                Cancel();
        }

        private void StopButton_Pressed(object sender, EventArgs e)
        {
            Cancel();
        }

        private void Cancel()
        {
            _tokenSource?.Cancel();
            Model.ChangingPWM = ChangePWM.None;
        }
    }
}