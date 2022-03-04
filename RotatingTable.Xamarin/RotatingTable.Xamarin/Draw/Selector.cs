using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;

namespace RotatingTable.Xamarin.Draw
{
    public class Selector
    {
        private readonly BaseDrawer[] _drawers = new BaseDrawer[(int)Mode.Last + 1];

        public Selector(SKCanvasView canvasView, MainModel model)
        {
            _drawers[(int)Mode.Auto] = new AutoDrawer(canvasView, model);
            _drawers[(int)Mode.Rotate90] = new Rotate90Drawer(canvasView, model);
            _drawers[(int)Mode.FreeMovement] = new FreeMovementDrawer(canvasView, model);
            _drawers[(int)Mode.Video] = new VideoDrawer(canvasView, model);
        }

        public BaseDrawer GetDrawer(Mode mode)
        {
            switch (mode)
            {
                case Mode.Auto:
                    return _drawers[(int)Mode.Auto];
                case Mode.Rotate90:
                    return _drawers[(int)Mode.Rotate90];
                case Mode.FreeMovement:
                    return _drawers[(int)Mode.FreeMovement];
                case Mode.Manual:
                    return _drawers[(int)Mode.Manual];
                case Mode.Nonstop:
                    return _drawers[(int)Mode.Nonstop];
                case Mode.Video:
                    return _drawers[(int)Mode.Video];
                default:
                    throw new InvalidOperationException("Unknown mode");
            }
        }

        public void Clear()
        {
            foreach (var drawer in _drawers)
            {
                if (drawer != null)
                    drawer.Clear();
            }
        }
    }
}
