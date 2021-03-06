using RotatingTable.Xamarin.TouchTracking;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;

namespace RotatingTable.Xamarin.Draw
{
    public class Rotate90Drawer : RotateDrawer
    {
        private int _startQuadrant = -1;
        private int _endQuadrant = -1;
        private bool _isClockWise;

        public Rotate90Drawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        protected override int StartAngle
        {
            get
            {
                if (_startQuadrant < 0 || _endQuadrant < 0)
                    return 0;

                int angle;
                if (IsClockwise)
                    angle = _startQuadrant == 3 && _endQuadrant == 0 ? -90 : _startQuadrant * 90 + _offset;
                else
                    angle = _startQuadrant * 90 + 90 + _offset;

                return angle;
            }
            set { }
        }

        protected override int EndAngle
        {
            get
            {
                if (_startQuadrant < 0 || _endQuadrant < 0)
                    return 0;

                int angle;
                if (IsClockwise)
                    angle = _endQuadrant * 90 + 90;
                else
                    angle = _startQuadrant == 0 && _endQuadrant == 3 ? -90 : _endQuadrant * 90;

                return angle;
            }
            set { }
        }

        private bool IsClockwise
        {
            get
            {
                if (_startQuadrant == _endQuadrant)
                    return _isClockWise;
                if (_startQuadrant == 0 && _endQuadrant == 3)
                    return false;
                if (_startQuadrant == 3 && _endQuadrant == 0)
                    return true;

                return _endQuadrant >= _startQuadrant;
            }
        }

        private SKPoint PreviousPoint { get; set; }

        public override async void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            if (_isBusy)
                return;

            var pt = Transform(args.Location);
            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    PreviousPoint = pt;
                    _isClockWise = true;
                    int quadrant = GetQuadrant(pt);
                    if (quadrant < 0)
                    {
                        Clear();
                        break;
                    }

                    _startQuadrant = _endQuadrant = quadrant;
                    _isDragging = true;
                    CanvasView.InvalidateSurface();
                    break;

                case TouchActionType.Moved:
                    if (!_isDragging)
                        break;

                    if (!IsSibling(_startQuadrant, pt))
                        Clear();
                    else
                    {
                        _endQuadrant = GetQuadrant(pt);
                        if (_startQuadrant == _endQuadrant && pt != PreviousPoint)
                        {
                            var endAngle = (int)ToAngle(pt);
                            var startAngle = (int)ToAngle(PreviousPoint);
                            if (Math.Abs(endAngle - startAngle) > 3)
                            {
                                _isClockWise = endAngle > startAngle;
                                PreviousPoint = pt;
                            }
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
            _startQuadrant = _endQuadrant = -1;
        }

        private int GetQuadrant(SKPoint pt)
        {
            if (!IsInsideCircle(pt))
                return -1;

            var angle = ToRadians(ToAngle(pt));
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            if (sin == 0 || cos == 0)
                return -1;

            if (cos > 0)
                return sin > 0 ? 0 : 3;
            else
                return sin > 0 ? 1 : 2;
        }

        private bool IsSibling(int quadrant, SKPoint pt)
        {
            int other = GetQuadrant(pt);
            return other == quadrant || other == Prev(quadrant) || other == Next(quadrant);
        }

        private int Prev(int quadrant)
        {
            return quadrant == 0 ? 3 : quadrant - 1;
        }

        private int Next(int quadrant)
        {
            return quadrant == 3 ? 0 : quadrant + 1;
        }
    }
}
