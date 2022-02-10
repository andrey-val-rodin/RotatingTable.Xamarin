using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;

namespace RotatingTable.Xamarin.Draw
{
    public class Factory
    {
        private readonly AutoDrawer _autoDrawer;
        private readonly Rotate90Drawer _rotate90Drawer;

        public Factory(MainModel model)
        {
            _autoDrawer = new(model);
            _rotate90Drawer = new(model);
        }

        public BaseDrawer GetDrawer(Mode mode)
        {
            switch (mode)
            {
                case Mode.Auto:
                    return _autoDrawer;
                case Mode.Rotate90:
                    return _rotate90Drawer;
                case Mode.Manual:
                case Mode.Nonstop:
                case Mode.Video:
                default:
                    return null;
            }
        }
    }
}
