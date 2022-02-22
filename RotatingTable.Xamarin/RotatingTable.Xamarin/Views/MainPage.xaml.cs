using RotatingTable.Xamarin.Draw;
using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RotatingTable.Xamarin.TouchTracking;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly Selector _selector;

        public MainModel Model
        {
            get
            {
                return BindingContext as MainModel;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            _selector = new(canvasView, Model);
            Model.CurrentStepChanged += OnCurrentStepChanged;
            Model.CurrentPosChanged += OnCurrentPosChanged;
        }

        protected override async void OnAppearing()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            var configService = DependencyService.Resolve<IConfigService>();
            if (!service.IsConnected)
            {
                var id = await configService.GetDeviceIdAsync();
                if (id != Guid.Empty)
                    await service.ConnectAsync(id);
            }

            if (service.IsConnected)
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
    }
}