using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class Selector
    {
        private readonly AutoDrawer _autoDrawer;
        private readonly Rotate90Drawer _rotate90Drawer;
        private readonly FreeMovementDrawer _freeMovementDrawer;

        public Selector(SKCanvasView canvasView, MainModel model)
        {
            _autoDrawer = new(canvasView, model);
            _rotate90Drawer = new(canvasView, model);
            _freeMovementDrawer = new(canvasView, model);
        }

        public BaseDrawer GetDrawer(Mode mode)
        {
            switch (mode)
            {
                case Mode.Auto:
                    return _autoDrawer;
                case Mode.Rotate90:
                    return _rotate90Drawer;
                case Mode.FreeMovement:
                    return _freeMovementDrawer;

                case Mode.Manual:
                case Mode.Nonstop:
                case Mode.Video:
                default:
                    return null;
            }
        }
    }
}
