using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.ViewModels;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
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
                var model = BindingContext as MainModel;
                model.Steps = Array.FindIndex(MainModel.StepValues, e => e == service.Steps);
                model.Acceleration = service.Acceleration;
                model.Exposure = service.Exposure / 100;
                model.Delay = service.Delay / 100;
            }
        }
    }
}