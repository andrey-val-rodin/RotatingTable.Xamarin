using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;

namespace RotatingTable.Xamarin.Draw
{
    public class Selector
    {
        private readonly BaseDrawer[] _drawers = new BaseDrawer[(int)Mode.Last + 1];

        public Selector(SKCanvasView canvasView, MainViewModel model)
        {
            _drawers[(int)Mode.Auto] = new AutoDrawer(canvasView, model);
            _drawers[(int)Mode.Manual] = new ManualDrawer(canvasView, model);
            _drawers[(int)Mode.Nonstop] = new NonStopDrawer(canvasView, model);
            _drawers[(int)Mode.Rotate90] = new Rotate90Drawer(canvasView, model);
            _drawers[(int)Mode.FreeMovement] = new FreeMovementDrawer(canvasView, model);
            _drawers[(int)Mode.Video] = new VideoDrawer(canvasView, model);
        }

        public BaseDrawer GetDrawer(Mode mode)
        {
            return mode switch
            {
                Mode.Auto => _drawers[(int)Mode.Auto],
                Mode.Manual => _drawers[(int)Mode.Manual],
                Mode.Nonstop => _drawers[(int)Mode.Nonstop],
                Mode.Rotate90 => _drawers[(int)Mode.Rotate90],
                Mode.FreeMovement => _drawers[(int)Mode.FreeMovement],
                Mode.Video => _drawers[(int)Mode.Video],
                _ => throw new InvalidOperationException("Unknown mode"),
            };
        }

        public void Clear()
        {
            foreach (var drawer in _drawers)
            {
                drawer?.Clear();
            }
        }
    }
}
