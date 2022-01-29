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

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var connectModel = BindingContext as ConnectModel;
            connectModel.CancelScan();

            var service = DependencyService.Resolve<IBluetoothService>();
            if (!service.Connect(connectModel.Devices[e.SelectedItemIndex]))
            {
                Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Не удаётся установить связь с Bluetooth устройством", "OK");
            }
        }
    }
}