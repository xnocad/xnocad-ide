﻿using IDE.Core.Converters;
using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Types.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IDE.Core.Adorners;

public class ArcAdorner : BaseCanvasItemAdorner
{
    Thumb startThumb;
    Thumb endThumb;

    IArcCanvasItem arc;
    XPoint? originalStartPoint;
    XPoint? originalEndPoint;

    public ArcAdorner(UIElement adornedElement) : base(adornedElement)
    {
        startThumb = new Thumb
        {
            Cursor = Cursors.SizeAll,
            Width = 4,
            Height = 4,
            Opacity = 0,
            Background = Brushes.Transparent
        };
        endThumb = new Thumb
        {
            Cursor = Cursors.SizeAll,
            Width = 4,
            Height = 4,
            Opacity = 0,
            Background = Brushes.Transparent
        };

        startThumb.DragDelta += StartThumb_DragDelta;
        startThumb.PreviewMouseDown += StartThumb_PreviewMouseDown;
        startThumb.PreviewMouseUp += StartThumb_PreviewMouseUp;

        endThumb.DragDelta += EndThumb_DragDelta;
        endThumb.PreviewMouseDown += EndThumb_PreviewMouseDown;
        endThumb.PreviewMouseUp += EndThumb_PreviewMouseUp;

        visualChildren.Add(startThumb);
        visualChildren.Add(endThumb);

        arc = ((FrameworkElement)AdornedElement).DataContext as IArcCanvasItem;
        arc.PropertyChanged += Arc_PropertyChanged;
    }

    private void EndThumb_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || canvasModel == null)
            return;

        var newEndPoint = new XPoint(arc.EndPointX, arc.EndPointY);
        var oldEndpoint = originalEndPoint.Value;
        originalEndPoint = null;

        canvasModel.RegisterUndoActionExecuted(
            undo: o =>
            {
                arc.EndPointX = oldEndpoint.X;
                arc.EndPointY = oldEndpoint.Y;
                return null;
            },
            redo: o =>
            {
                arc.EndPointX = newEndPoint.X;
                arc.EndPointY = newEndPoint.Y;
                return null;
            },
            null
            );
    }

    private void EndThumb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || canvasModel == null)
            return;

        if (originalEndPoint == null)
            originalEndPoint = new XPoint(arc.EndPointX, arc.EndPointY);
    }

    private void StartThumb_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || canvasModel == null)
            return;

        var newStartPoint = new XPoint(arc.StartPointX, arc.StartPointY);
        var oldStartpoint = originalStartPoint.Value;
        originalStartPoint = null;

        canvasModel.RegisterUndoActionExecuted(
            undo: o =>
            {
                arc.StartPointX = oldStartpoint.X;
                arc.StartPointY = oldStartpoint.Y;
                return null;
            },
            redo: o =>
            {
                arc.StartPointX = newStartPoint.X;
                arc.StartPointY = newStartPoint.Y;
                return null;
            },
            null
            );
    }

    private void StartThumb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || canvasModel == null)
            return;

        if (originalStartPoint == null)
            originalStartPoint = new XPoint(arc.StartPointX, arc.StartPointY);
    }

    void Arc_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(arc.IsSelected))
        {
            if (!arc.IsSelected)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(this);
                }
            }
        }
    }

    // Event for the Thumb Start Point
    private void StartThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (arc != null)
        {
            var position = canvasModel.SnapToGridFromDpi(Mouse.GetPosition(this).ToXPoint());

            arc.StartPointX = position.X;
            arc.StartPointY = position.Y;

            InvalidateVisual();
        }
    }

    // Event for the Thumb End Point
    private void EndThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (arc != null)
        {
            var position = canvasModel.SnapToGridFromDpi(Mouse.GetPosition(this).ToXPoint());

            arc.EndPointX = position.X;
            arc.EndPointY = position.Y;

            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // without a background the OnMouseMove event would not be fired !
        // Alternative: implement a Canvas as a child of this adorner, like
        // the ConnectionAdorner does.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        Pen renderPen = new Pen(new SolidColorBrush(Colors.White), 0);
        var sp = new Point(MilimetersToDpiHelper.ConvertToDpi(arc.StartPointX), MilimetersToDpiHelper.ConvertToDpi(arc.StartPointY));
        var ep = new Point(MilimetersToDpiHelper.ConvertToDpi(arc.EndPointX), MilimetersToDpiHelper.ConvertToDpi(arc.EndPointY));
        var radius = Math.Max(0.1, MilimetersToDpiHelper.ConvertToDpi(arc.BorderWidth / 2));

        dc.DrawEllipse(Brushes.White, renderPen, sp, radius, radius);
        dc.DrawEllipse(Brushes.White, renderPen, ep, radius, radius);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        //to update position of thumbs
        ArrangeDesignerItem();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        //to update position of thumbs
        ArrangeDesignerItem();
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);
        e.Handled = false;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ArrangeDesignerItem();

        return base.ArrangeOverride(finalSize);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var b = base.MeasureOverride(constraint);
        InvalidateVisual();
        return b;
    }

    protected override Geometry GetLayoutClip(Size layoutSlotSize)
    {
        var g = Geometry.Empty;
        var radius = Math.Max(0.1, MilimetersToDpiHelper.ConvertToDpi(arc.BorderWidth / 2) * 1.1);

        var sp = new Point(MilimetersToDpiHelper.ConvertToDpi(arc.StartPointX), MilimetersToDpiHelper.ConvertToDpi(arc.StartPointY));
        var ep = new Point(MilimetersToDpiHelper.ConvertToDpi(arc.EndPointX), MilimetersToDpiHelper.ConvertToDpi(arc.EndPointY));


        g = Geometry.Combine(g, new EllipseGeometry(sp, radius, radius), GeometryCombineMode.Union, null);
        g = Geometry.Combine(g, new EllipseGeometry(ep, radius, radius), GeometryCombineMode.Union, null);

        return g;
    }

    void ArrangeDesignerItem()
    {
        if (arc != null)
        {
            var spX = MilimetersToDpiHelper.ConvertToDpi(arc.StartPointX);
            var spY = MilimetersToDpiHelper.ConvertToDpi(arc.StartPointY);
            var epX = MilimetersToDpiHelper.ConvertToDpi(arc.EndPointX);
            var epY = MilimetersToDpiHelper.ConvertToDpi(arc.EndPointY);
            var width = MilimetersToDpiHelper.ConvertToDpi(Math.Max(0.1, arc.BorderWidth));
            var height = width;
            startThumb.Width = startThumb.Height = width;
            endThumb.Width = endThumb.Height = width;

            var startRect = new Rect(spX - (startThumb.Width / 2), spY - (startThumb.Width / 2), startThumb.Width, startThumb.Height);
            startThumb.Arrange(startRect);

            var endRect = new Rect(epX - (endThumb.Width / 2), epY - (endThumb.Height / 2), endThumb.Width, endThumb.Height);
            endThumb.Arrange(endRect);

        }

        InvalidateVisual();
    }
}
