using RotatingTable.Xamarin.Draw;
using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RotatingTable.Xamarin.TouchTracking;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Acr.UserDialogs;
using System.Timers;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Threading;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly Selector _selector;
        private readonly IUserDialogs _userDialogs;
        private CancellationTokenSource _tokenSource;

        public MainPage()
        {
            InitializeComponent();

            _userDialogs = UserDialogs.Instance;
            _selector = new(canvasView, Model);
            
            Model.CurrentStepChanged += OnCurrentStepChanged;
            Model.CurrentPosChanged += OnCurrentPosChanged;
            
            Model.Stop += OnStop;
            Adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;

            Model.Service.Timeout += Service_Timeout;
        }

        private IAdapter Adapter => CrossBluetoothLE.Current.Adapter;

        public MainModel Model
        {
            get
            {
                return BindingContext as MainModel;
            }
        }

        protected override async void OnAppearing()
        {
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (!Model.Service.IsConnected)
            {
                var id = await Model.ConfigService.GetDeviceIdAsync();
                if (id != Guid.Empty)
                    await Model.Service.ConnectAsync(id);
            }

            if (Model.Service.IsConnected)
                await Model.InitAsync();
            else
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

        private void Adapter_DeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _userDialogs.AlertAsync("Соединение со столом разорвано");
                await Shell.Current.GoToAsync("//ConnectPage");
            });
        }


        private void Service_Timeout(object sender, ElapsedEventArgs args)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _userDialogs.AlertAsync("Превышено время ожидания ответа от стола в процессе работы");
                await Model.Service.DisconnectAsync();
                Model.IsRunning = false;
                Model.IsConnected = false;
                await ConnectAsync();
            });
        }

        private void DecreasePWMButton_Pressed(object sender, EventArgs e)
        {
            if (_tokenSource != null)
                return;

            _tokenSource = new();
            var token = _tokenSource.Token;
            Task.Run(async () =>
            {
                while (true)
                {
                    if (!await Model.Service.DecreasePWMAsync())
                        break;

                    if (token.IsCancellationRequested)
                        break;

                    await Task.Delay(100);
                }

                _tokenSource.Dispose();
                _tokenSource = null;
            });
        }

        private void DecreasePWMButton_Released(object sender, EventArgs e)
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }

        private void IncreasePWMButton_Pressed(object sender, EventArgs e)
        {
            if (_tokenSource != null)
                return;

            _tokenSource = new();
            var token = _tokenSource.Token;
            Task.Run(async () =>
            {
                while (true)
                {
                    if (!await Model.Service.IncreasePWMAsync())
                        break;

                    if (token.IsCancellationRequested)
                        break;

                    await Task.Delay(100);
                }

                _tokenSource.Dispose();
                _tokenSource = null;
            });
        }

        private void IncreasePWMButton_Released(object sender, EventArgs e)
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }

        private void StopButton_Pressed(object sender, EventArgs e)
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }
    }
}