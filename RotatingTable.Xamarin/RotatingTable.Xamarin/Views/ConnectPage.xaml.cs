using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.ViewModels;
using System.Linq;
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
            var id = await configService.GetDeviceIdAsync();
            var connectModel = BindingContext as ConnectModel;
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
                var configService = DependencyService.Resolve<IConfigService>();
                await configService.SetDeviceIdAsync(item.Device.Id);
                var mainModel = GetMainModel();
                await mainModel?.InitAsync();
                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        private MainModel GetMainModel()
        {
            var mainPage = (MainPage)(Shell.Current?.Items[0]?.CurrentItem as IShellSectionController)?.PresentedPage;
            return mainPage?.Model;
        }
    }
}