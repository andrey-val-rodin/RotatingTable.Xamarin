using RotatingTable.Xamarin.TouchTracking;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;
using System;

namespace RotatingTable.Xamarin.Draw
{
    public class FreeMovementDrawer : RotateDrawer
    {
        private int _startAngle;

        public FreeMovementDrawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        protected override int StartAngle
        {
            get => _startAngle + _offset;
            set { _startAngle = value; }
        }

        protected override int EndAngle
        {
            get;
            set;
        }

        public override async void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            if (_isBusy)
                return;

            var pt = Transform(args.Location);
            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    if (!IsInsideCircle(pt))
                    {
                        Clear();
                        break;
                    }

                    StartAngle = EndAngle = (int)ToAngle(pt);
                    _isDragging = true;
                    CanvasView.InvalidateSurface();
                    break;

                case TouchActionType.Moved:
                    if (!_isDragging)
                        break;

                    if (!IsInsideCircle(pt))
                        Clear();
                    else
                    {
                        EndAngle = (int)ToAngle(pt);
                        if (Math.Abs(EndAngle - StartAngle) > 180)
                        {
                            if (StartAngle > 180)
                                EndAngle += 360;
                            else
                                EndAngle -= 360;
                        }
                    }
                    CanvasView.InvalidateSurface();
                    break;

                case TouchActionType.Released:
                    _isDragging = false;
                    await StartMovementAsync();
                    break;

                default:
                    CanvasView.InvalidateSurface();
                    Clear();
                    break;
            }
        }

        public override void Clear()
        {
            base.Clear();
            StartAngle = EndAngle = 0;
        }
    }
}
