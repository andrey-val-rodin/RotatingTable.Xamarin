using RotatingTable.Xamarin.Draw;
using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
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
            Model.CurrentStepChanged += Model_CurrentStepChanged;
        }

        protected override async void OnAppearing()
        {
            var service = DependencyService.Resolve<IBluetoothService>();
            if (!service.IsConnected)
            {
                var configService = DependencyService.Resolve<IConfigService>();
                var address = await configService.GetMacAddressAsync();
                if (!string.IsNullOrEmpty(address))
                    await service.ConnectAsync(address);
            }

            if (service.IsConnected)
            {
                Model.StepsIndex = Array.FindIndex(MainModel.StepValues, e => e == service.Steps);
                Model.Acceleration = service.Acceleration;
                Model.Exposure = service.Exposure / 100;
                Model.Delay = service.Delay / 100;
            }
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            new BaseDrawer(Model).Draw(args);
        }

        private void Model_CurrentStepChanged(object sender, EventArgs args)
        {
            canvasView.InvalidateSurface();
        }
    }
}