﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System;
using IDE.Core.Designers;
using IDE.Core.Controls;
using IDE.Core.Converters;
using IDE.Core.Interfaces;
using IDE.Core.Types.Media;
using IDE.Core.Presentation.Utilities;
using System.Linq;
using IDE.Core.Interfaces.Geometries;

namespace IDE.Core.Adorners
{
    public class RubberbandAdorner : Adorner
    {
        Point? startPoint;
        Point? endPoint;
        Pen rubberbandPen;

        FrameworkElement designerCanvas;

        public RubberbandAdorner(FrameworkElement canvas, Point? dragStartPoint)
            : base(canvas)
        {
            designerCanvas = canvas;
            startPoint = dragStartPoint;
            var t = GetPenThickness();
            rubberbandPen = new Pen(Brushes.LightGray, t);
            rubberbandPen.DashStyle = new DashStyle(new double[] { 2 }, 1);

            canvasModel = designerCanvas.DataContext as ICanvasDesignerFileViewModel;

            GeometryHelper = ServiceProvider.Resolve<IGeometryOutlineHelper>();
        }

        private readonly IGeometryOutlineHelper GeometryHelper;
        private readonly ICanvasDesignerFileViewModel canvasModel;

        double GetPenThickness()
        {
            var thickness = 1.0d;
            var drawingCanvas = AdornedElement as DrawingCanvas;
            if (drawingCanvas != null)
                thickness = thickness / drawingCanvas.Scale;

            return thickness;
        }

        private Point GetMousePosition()
        {
            var mp = Mouse.GetPosition(this);

            return mp;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsMouseCaptured)
                    CaptureMouse();

                endPoint = GetMousePosition();
                UpdateSelection();
                InvalidateVisual();
            }
            else
            {
                if (IsMouseCaptured)
                    ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            // release mouse capture
            if (IsMouseCaptured)
                ReleaseMouseCapture();

            // remove this adorner from adorner layer
            var adornerLayer = AdornerLayer.GetAdornerLayer(designerCanvas);
            if (adornerLayer != null)
                adornerLayer.Remove(this);

            canvasModel?.UpdateSelection();

            e.Handled = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // without a background the OnMouseMove event would not be fired !
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            if (startPoint.HasValue && endPoint.HasValue)
                dc.DrawRectangle(Brushes.Transparent, rubberbandPen, new Rect(startPoint.Value, endPoint.Value));
        }

        void UpdateSelection()
        {
            if (canvasModel == null)
            {
                return;
            }

            var rubberBandRect = new XRect(startPoint.Value.ToXPoint(), endPoint.Value.ToXPoint());
            rubberBandRect = MilimetersToDpiHelper.ConvertToMM(rubberBandRect);
            var origin = canvasModel.Origin;
            rubberBandRect.X -= origin.X;
            rubberBandRect.Y -= origin.Y;

            foreach (var item in canvasModel.GetItems())
            {
                if (canvasModel.CanSelectItem(item))
                {
                    if (GeometryHelper.ItemIntersectsRectangle(item, rubberBandRect))
                    {
                        item.IsSelected = true;

                        if (item is ISegmentedPolylineSelectableCanvasItem wire)
                        {
                            var intersectedSegments = wire.GetIntersectedSegmentsWith(rubberBandRect);
                            if (intersectedSegments.Count > 0)
                            {
                                wire.SelectSegment(intersectedSegments[0]);
                                if (intersectedSegments.Count > 1)
                                {
                                    wire.SelectSegmentAppend(intersectedSegments.Last());
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                        {
                            item.IsSelected = false;
                        }
                    }
                }
            }

        }
    }
}
