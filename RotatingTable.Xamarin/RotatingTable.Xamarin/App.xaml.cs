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
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
