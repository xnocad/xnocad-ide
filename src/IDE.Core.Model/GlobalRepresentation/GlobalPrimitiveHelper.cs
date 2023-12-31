﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDE.Core.Interfaces;
using IDE.Core.Model.GlobalRepresentation.Primitives;
using IDE.Core.Types.Media;

namespace IDE.Core.Model.GlobalRepresentation
{
    public class GlobalPrimitiveHelper
    {
        private GlobalPrimitive GetGlobalPrimitive(IRegionItem item, XPoint startPoint, double width)
        {
            if (item != null)
            {
                if (item is ILineRegionItem line)
                    return GetLinePrimitive(line, startPoint, width);
                else if (item is IArcRegionItem arc)
                    return GetArcPrimitive(arc, startPoint, width);
            }

            return null;
        }

        public GlobalPrimitive GetGlobalPrimitive(ICanvasItem canvasItem)
        {
            if (canvasItem == null)
                return null;

            switch (canvasItem)
            {
                case ILineCanvasItem line:
                    return GetLinePrimitive(line);

                case IPolylineCanvasItem polyline:
                    return GetPolylinePrimitive(polyline);

                case IRectangleCanvasItem rectangle:
                    return GetRectanglePrimitive(rectangle);

                case ICircleCanvasItem circle:
                    return GetCirclePrimitive(circle);

                case IHoleCanvasItem hole:
                    return GetHolePrimitive(hole);

                case IViaCanvasItem via:
                    return GetViaPrimitive(via);

                case IPadCanvasItem pad:
                    return GetPadPrimitive(pad);

                case IPolygonBoardCanvasItem poly:
                    return GetPolygonPrimitive(poly);

                case IArcCanvasItem arc:
                    return GetArcPrimitive(arc);

                case ITextCanvasItem text:
                    return GetTextPrimitive(text);

                case ITextMonoLineCanvasItem textMono:
                    return GetTextPrimitive(textMono);

                case IRegionCanvasItem region:
                    return GetRegionPrimitive(region);

                case IPlaneBoardCanvasItem plane:
                    return GetPlanePrimitive(plane);
            }

            return null;
        }

        private GlobalPrimitive GetRectanglePrimitive(IRectangleCanvasItem item)
        {
            var t = item.GetTransform();
            var position = new XPoint(t.Value.OffsetX, t.Value.OffsetY);
            var placement = FootprintPlacement.Top;
            if (item.ParentObject is IFootprintBoardCanvasItem fp)
                placement = fp.Placement;
            var rot = GetWorldRotation(t, placement);

            var rect = new GlobalRectanglePrimitive
            {
                X = position.X,
                Y = position.Y,
                IsFilled = item.IsFilled,
                Width = item.Width,
                Height = item.Height,
                CornerRadius = item.CornerRadius,
                BorderWidth = item.BorderWidth,
                Rot = rot,
            };

            var partName = GetPartName(item);
            if (!string.IsNullOrEmpty(partName))
            {
                rect.Tags[nameof(GlobalStandardPrimitiveTag.PartName)] = partName;
            }

            return rect;
        }

        private GlobalPrimitive GetCirclePrimitive(ICircleCanvasItem item)
        {
            var t = item.GetTransform();
            var position = new XPoint(t.Value.OffsetX, t.Value.OffsetY);

            var circle = new GlobalCirclePrimitive
            {
                X = position.X,
                Y = position.Y,
                Diameter = item.Diameter,
                BorderWidth = item.BorderWidth,
                IsFilled = item.IsFilled
            };

            var partName = GetPartName(item);
            if (!string.IsNullOrEmpty(partName))
            {
                circle.Tags[nameof(GlobalStandardPrimitiveTag.PartName)] = partName;
            }

            return circle;
        }

        private GlobalPrimitive GetHolePrimitive(IHoleCanvasItem item)
        {
            var t = item.GetTransform();
            var position = new XPoint(t.Value.OffsetX, t.Value.OffsetY);

            if (item.DrillType == DrillType.Drill)
            {
                return new GlobalHolePrimitive
                {
                    X = position.X,
                    Y = position.Y,
                    Drill = item.Drill
                };
            }
            else
            {
                //var placement = FootprintPlacement.Top;
                //var fp = item.ParentObject as IFootprintBoardCanvasItem;
                //if (fp != null)
                //    placement = fp.Placement;

                //var rot = GetWorldRotation(t, placement);

                //return new GlobalRectanglePrimitive
                //{
                //    X = position.X,
                //    Y = position.Y,
                //    Width = item.Drill,
                //    Height = item.Height,
                //    CornerRadius = item.Drill * 0.5,
                //    Rot = rot
                //};

                var startPoint = new XPoint(0, -0.5 * item.Height + 0.5 * item.Drill);
                var endPoint = new XPoint(0, 0.5 * item.Height - 0.5 * item.Drill);
                startPoint = t.Transform(startPoint);
                endPoint = t.Transform(endPoint);

                return new GlobalLinePrimitive
                {
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    Width = item.Drill
                };
            }
        }

        private GlobalPrimitive GetViaPrimitive(IViaCanvasItem item)
        {
            var via = new GlobalViaPrimitive
            {
                X = item.X,
                Y = item.Y,
                PadDiameter = item.Diameter,
                Drill = item.Drill,
            };

            via.Tags[nameof(GlobalStandardPrimitiveTag.Role)] = GlobalStandardPrimitiveRole.Via;

            var netName = item.Signal?.Name;
            if (!string.IsNullOrEmpty(netName))
                via.Tags[nameof(GlobalStandardPrimitiveTag.NetName)] = netName;

            return via;
        }

        private GlobalPrimitive GetPadPrimitive(IPadCanvasItem item)
        {
            var t = item.GetTransform();
            var position = new XPoint(t.Value.OffsetX, t.Value.OffsetY);
            var placement = FootprintPlacement.Top;
            if (item.ParentObject is IFootprintBoardCanvasItem fp)
                placement = fp.Placement;
            var rot = GetWorldRotation(t, placement);

            var pad = new GlobalRectanglePrimitive
            {
                X = position.X,
                Y = position.Y,
                Width = item.Width,
                Height = item.Height,
                CornerRadius = item.CornerRadius,
                Rot = rot
            };

            if (item is IPadSmdCanvasItem)
                pad.Tags[nameof(GlobalStandardPrimitiveTag.Role)] = GlobalStandardPrimitiveRole.PadSmd;

            if (item is IPadThtCanvasItem)
                pad.Tags[nameof(GlobalStandardPrimitiveTag.Role)] = GlobalStandardPrimitiveRole.PadTht;

            var netName = item.Signal?.Name;
            if (!string.IsNullOrEmpty(netName))
                pad.Tags[nameof(GlobalStandardPrimitiveTag.NetName)] = netName;

            var partName = GetPartName(item);
            if (!string.IsNullOrEmpty(partName))
            {
                pad.Tags[nameof(GlobalStandardPrimitiveTag.PartName)] = partName;
                pad.Tags[nameof(GlobalStandardPrimitiveTag.PinNumber)] = item.Number;
            }
            return pad;
        }

        private string GetPartName(ISelectableItem item)
        {
            if (item.ParentObject is IFootprintBoardCanvasItem footprint && footprint != null)
            {
                return footprint.PartName;
            }

            return null;
        }

        private GlobalPrimitive GetPolygonPrimitive(IPolygonBoardCanvasItem item)
        {
            //we want a poured poly when filled and we want a clearance of filled primitive when adding clearance

            //todo: pour poly only when on a signal or plane (copper) layer
            if (item.Layer != null && ( item.Layer.LayerType == LayerType.Signal || item.Layer.LayerType == LayerType.Plane ))
            {
                var proc = new GlobalPrimitivePourProcessor();
                var globalPrimitive = proc.GetPrimitive(item);
                return globalPrimitive;
            }

            var t = item.GetTransform();
            var poly = new GlobalPolygonPrimitive
            {
                Points = item.PolygonPoints.Select(p => t.Transform(p)).ToList(),
                BorderWidth = item.BorderWidth,
                IsFilled = item.IsFilled,
            };

            var netName = item.Signal?.Name;
            if (!string.IsNullOrEmpty(netName))
                poly.Tags[nameof(GlobalStandardPrimitiveTag.NetName)] = netName;

            return poly;
        }

        private GlobalPrimitive GetLinePrimitive(ILineRegionItem item, XPoint startPoint, double width)
        {
            return new GlobalLinePrimitive
            {
                StartPoint = startPoint,
                EndPoint = item.EndPoint,
                Width = width
            };
        }

        private GlobalPrimitive GetArcPrimitive(IArcRegionItem item, XPoint startPoint, double width)
        {
            return new GlobalArcPrimitive
            {
                StartPoint = startPoint,
                EndPoint = item.EndPoint,
                Width = width,
                SizeDiameter = 2 * item.SizeDiameter,
                SweepDirection = item.SweepDirection
            };
        }

        private GlobalPrimitive GetArcPrimitive(IArcCanvasItem item)
        {
            var sp = new XPoint(item.StartPointX, item.StartPointY);
            var ep = new XPoint(item.EndPointX, item.EndPointY);
            var center = item.GetCenter();

            var t = item.GetTransform();
            sp = GetPointTransform(t, sp);
            ep = GetPointTransform(t, ep);
            center = GetPointTransform(t, center);

            var arc = new GlobalArcPrimitive
            {
                StartPoint = sp,
                EndPoint = ep,
                Width = item.BorderWidth,
                SizeDiameter = 2 * item.Radius,
                IsLargeArc = item.IsLargeArc,
                Center = center,
                RotationAngle = item.RotationAngle,
                IsMirrored = item.IsMirrored(),
                SweepDirection = item.IsMirrored() ? (XSweepDirection)( 1 - (int)item.SweepDirection ) : item.SweepDirection
            };

            var partName = GetPartName(item);
            if (!string.IsNullOrEmpty(partName))
            {
                arc.Tags[nameof(GlobalStandardPrimitiveTag.PartName)] = partName;
            }

            return arc;
        }

        private GlobalPrimitive GetTextPrimitive(ITextCanvasItem item)
        {
            var t = item.GetTransform();
            var position = new XPoint(t.Value.OffsetX, t.Value.OffsetY);
            var placement = FootprintPlacement.Top;
            if (item.ParentObject is IFootprintBoardCanvasItem fp)
                placement = fp.Placement;
            var rot = GetWorldRotation(t, placement);

            return new GlobalTextPrimitive
            {
                X = position.X,
                Y = position.Y,
                Text = item.Text,
                Width = item.Width,
                Rot = rot,
                Bold = item.Bold,
                Italic = item.Italic,
                FontFamily = item.FontFamily,
                FontSize = item.FontSize,
            };
        }

        private GlobalPrimitive GetTextPrimitive(ITextMonoLineCanvasItem item)
        {
            var t = item.GetTransform();

            var figure = new GlobalFigurePrimitive();
            double lx = 0;
            foreach (var letter in item.LetterItems)
            {
                var local = new XTranslateTransform(lx, 0);
                foreach (var li in letter.Items.Select(l => (ISelectableItem)l.Clone()))
                {
                    li.TransformBy(local.Value * t.Value);

                    var primitive = GetGlobalPrimitive(li);
                    figure.FigureItems.Add(primitive);
                }

                lx += item.FontSize;
            }

            return figure;
        }

        private GlobalPrimitive GetRegionPrimitive(IRegionCanvasItem item)
        {
            //var figure = new GlobalFigurePrimitive();

            //var startPoint = item.StartPoint;

            //foreach (var regionItem in item.Items)
            //{
            //    var primitive = GetGlobalPrimitive(regionItem, startPoint, item.Width);
            //    startPoint = regionItem.EndPoint;
            //    figure.FigureItems.Add(primitive);
            //}

            //return figure;

            var region = new GlobalRegionPrimitive();
            region.Width = item.Width;
            var startPoint = item.StartPoint;
            region.StartPoint = startPoint;

            foreach (var regionItem in item.Items)
            {
                var primitive = GetGlobalPrimitive(regionItem, startPoint, item.Width);
                startPoint = regionItem.EndPoint;
                region.Items.Add(primitive);
            }

            return region;

        }

        private GlobalPrimitive GetPlanePrimitive(IPlaneBoardCanvasItem item)
        {
            return null;
        }

        private GlobalPrimitive GetPolylinePrimitive(IPolylineCanvasItem item)
        {
            //not needed to be transformed...
            var polyline = new GlobalPolylinePrimitive
            {
                Width = item.Width,
                Points = item.Points.ToList()
            };

            if (item is ITrackBoardCanvasItem track)
            {
                polyline.Tags[nameof(GlobalStandardPrimitiveTag.Role)] = GlobalStandardPrimitiveRole.Track;
                polyline.Tags[nameof(GlobalStandardPrimitiveTag.NetName)] = track.Signal?.Name;
            }

            return polyline;
        }

        private GlobalPrimitive GetLinePrimitive(ILineCanvasItem item)
        {
            var sp = new XPoint(item.X1, item.Y1);
            var ep = new XPoint(item.X2, item.Y2);

            var t = item.GetTransform();
            sp = GetPointTransform(t, sp);
            ep = GetPointTransform(t, ep);

            var line = new GlobalLinePrimitive
            {
                StartPoint = sp,
                EndPoint = ep,
                Width = item.Width,
            };

            var partName = GetPartName(item);
            if (!string.IsNullOrEmpty(partName))
            {
                line.Tags[nameof(GlobalStandardPrimitiveTag.PartName)] = partName;
            }

            return line;
        }

        private XPoint GetPointTransform(XTransform transform, XPoint p)
        {
            p = transform.Transform(p);
            return p;
        }

        double GetWorldRotation(XTransform transform, FootprintPlacement placement)
        {
            var rotAngle = GetRotationTransform(transform);
            var myRot = rotAngle;

            if (placement == FootprintPlacement.Bottom)
            {
                myRot = 180.0d - myRot;
            }

            return myRot;
        }

        double GetRotationTransform(XTransform transform)
        {
            return Geometry2DHelper.GetRotationAngleFromMatrix(transform.Value);
        }
    }
}
