using RotatingTable.Xamarin.Models;
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
            var configService = DependencyService.Resolve<IConfig>();
            var id = await configService.GetDeviceIdAsync();
            var connectModel = BindingContext as ConnectViewModel;
            connectModel.DeviceName = id.ToString().Replace("00000000-0000-0000-0000-", "");
            await connectModel.ScanAsync();
        }

        private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var item = e.SelectedItem as DeviceItem;
            if (item == null)
                return;

            var service = DependencyService.Resolve<IBluetoothService>();
            if (!await service.ConnectAsync(item.Device))
                ((ListView)sender).SelectedItem = null;
            else
            {
                var configService = DependencyService.Resolve<IConfig>();
                await configService.SetDeviceIdAsync(item.Device.Id);
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }
}