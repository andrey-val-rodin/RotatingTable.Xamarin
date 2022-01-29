﻿using Android.Bluetooth;
using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RotatingTable.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectPage : ContentPage
    {
        public ConnectPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            var configService = DependencyService.Resolve<IConfigService>();
            var address = await configService.GetMacAddressAsync();
            var connectModel = BindingContext as ConnectModel;
            connectModel.DeviceName = address;
        }

        private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var connectModel = BindingContext as ConnectModel;
            connectModel.CancelScan();

            var service = DependencyService.Resolve<IBluetoothService>();
            if (!await service.ConnectAsync((connectModel.Devices[e.SelectedItemIndex].NativeDevice as BluetoothDevice).Address))
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Не удаётся установить связь с Bluetooth устройством", "OK");
            }
        }
    }
}