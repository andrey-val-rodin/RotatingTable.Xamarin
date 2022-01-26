using RotatingTable.Xamarin.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}