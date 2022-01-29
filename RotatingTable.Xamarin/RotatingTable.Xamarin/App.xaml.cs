using RotatingTable.Xamarin.Services;
using Xamarin.Forms;

namespace RotatingTable.Xamarin
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
            DependencyService.RegisterSingleton<IBluetoothService>(new BluetoothService());
            DependencyService.RegisterSingleton<IConfigService>(new ConfigService());
        }

        protected override async void OnStart()
        {
            var configService = DependencyService.Resolve<IConfigService>();
            var bluetoothService = DependencyService.Resolve<IBluetoothService>();
            var address = await configService.GetMacAddressAsync();
            if (string.IsNullOrEmpty(address) || 
                !await bluetoothService.ConnectAsync(address ))
                await Shell.Current.GoToAsync("//ConnectPage");
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
