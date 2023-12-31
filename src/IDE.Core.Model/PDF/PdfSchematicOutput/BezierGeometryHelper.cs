﻿using IDE.Core.Types.Media;
using System;
using System.Collections.Generic;
//using System.Windows;
//using System.Windows.Media;

namespace IDE.Core.PDF
{
    class BezierGeometryHelper
    {
        const double Deg2Rad = Math.PI / 180;

        /// <summary>
        /// Creates between 1 and 5 Béziers curves from parameters specified like in WPF.
        /// </summary>
        public static List<XPoint> BezierCurveFromArc(XPoint point1, XPoint point2, XSize size,
            double rotationAngle, bool isLargeArc, bool clockwise)
        {
            // See also http://www.charlespetzold.com/blog/blog.xml from January 2, 2008: 
            // http://www.charlespetzold.com/blog/2008/01/Mathematics-of-ArcSegment.html
            double δx = size.Width;
            double δy = size.Height;
           // Debug.Assert(δx * δy > 0);
            double factor = δy / δx;
            //bool isCounterclockwise = !clockwise;

            // Adjust for different radii and rotation angle.
            var t = new XTransformGroup();
            t.Children.Add(new XRotateTransform(-rotationAngle));
            t.Children.Add(new XScaleTransform(δy / δx, 1));
            var matrix = t.Value;
            //matrix.RotateAppend(-rotationAngle);
            //matrix.ScaleAppend(δy / δx, 1);
            var pt1 = matrix.Transform(point1);
            var pt2 = matrix.Transform(point2);

            // Get info about chord that connects both points.
            var midPoint = new XPoint((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);
            var vect = pt2 - pt1;
            double halfChord = vect.Length / 2;

            // Get vector from chord to center.
            XVector vectRotated;

            // (comparing two Booleans here!)
            if (isLargeArc == clockwise)
                vectRotated = new XVector(-vect.Y, vect.X);
            else
                vectRotated = new XVector(vect.Y, -vect.X);

            vectRotated.Normalize();

            // Distance from chord to center.
            double centerDistance = Math.Sqrt(δy * δy - halfChord * halfChord);
            if (double.IsNaN(centerDistance))
                centerDistance = 0;

            // Calculate center point.
            var center = midPoint + centerDistance * vectRotated;

            // Get angles from center to the two points.
            double α = Math.Atan2(pt1.Y - center.Y, pt1.X - center.X);
            double β = Math.Atan2(pt2.Y - center.Y, pt2.X - center.X);

            // (another comparison of two Booleans!)
            if (isLargeArc == (Math.Abs(β - α) < Math.PI))
            {
                if (α < β)
                    α += 2 * Math.PI;
                else
                    β += 2 * Math.PI;
            }

            // Invert matrix for final point calculation.
            matrix.Invert();
            double sweepAngle = β - α;

            // Let the algorithm of GDI+ DrawArc to Bézier curves do the rest of the job
            return BezierCurveFromArc(center.X - δx * factor, center.Y - δy, 2 * δx * factor, 2 * δy,
                                       α / Deg2Rad, sweepAngle / Deg2Rad, ref matrix);
        }

        /// <summary>
        /// Creates between 1 and 5 Béziers curves from parameters specified like in GDI+.
        /// </summary>
        public static List<XPoint> BezierCurveFromArc(double x, double y, double width, double height, double startAngle, double sweepAngle,
                                                     ref XMatrix matrix)
        {
            var points = new List<XPoint>();

            // Normalize the angles.
            double α = startAngle;
            if (α < 0)
                α = α + (1 + Math.Floor((Math.Abs(α) / 360))) * 360;
            else if (α > 360)
                α = α - Math.Floor(α / 360) * 360;
            //Debug.Assert(α >= 0 && α <= 360);

            double β = sweepAngle;
            if (β < -360)
                β = -360;
            else if (β > 360)
                β = 360;

            if (α == 0 && β < 0)
                α = 360;
            else if (α == 360 && β > 0)
                α = 0;

            // Is it possible that the arc is small starts and ends in same quadrant?
            bool smallAngle = Math.Abs(β) <= 90;

            β = α + β;
            if (β < 0)
                β = β + (1 + Math.Floor((Math.Abs(β) / 360))) * 360;

            bool clockwise = sweepAngle > 0;
            int startQuadrant = Quadrant(α, true, clockwise);
            int endQuadrant = Quadrant(β, false, clockwise);

            if (startQuadrant == endQuadrant && smallAngle)
                AppendPartialArcQuadrant(points, x, y, width, height, α, β, PathStart.MoveTo1st, matrix);
            else
            {
                int currentQuadrant = startQuadrant;
                bool firstLoop = true;
                do
                {
                    if (currentQuadrant == startQuadrant && firstLoop)
                    {
                        double ξ = currentQuadrant * 90 + (clockwise ? 90 : 0);
                        AppendPartialArcQuadrant(points, x, y, width, height, α, ξ, PathStart.MoveTo1st, matrix);
                    }
                    else if (currentQuadrant == endQuadrant)
                    {
                        double ξ = currentQuadrant * 90 + (clockwise ? 0 : 90);
                        AppendPartialArcQuadrant(points, x, y, width, height, ξ, β, PathStart.Ignore1st, matrix);
                    }
                    else
                    {
                        double ξ1 = currentQuadrant * 90 + (clockwise ? 0 : 90);
                        double ξ2 = currentQuadrant * 90 + (clockwise ? 90 : 0);
                        AppendPartialArcQuadrant(points, x, y, width, height, ξ1, ξ2, PathStart.Ignore1st, matrix);
                    }

                    // Don't stop immediately if arc is greater than 270 degrees.
                    if (currentQuadrant == endQuadrant && smallAngle)
                        break;
                    smallAngle = true;

                    if (clockwise)
                        currentQuadrant = currentQuadrant == 3 ? 0 : currentQuadrant + 1;
                    else
                        currentQuadrant = currentQuadrant == 0 ? 3 : currentQuadrant - 1;

                    firstLoop = false;
                } while (true);
            }
            return points;
        }

        /// <summary>
        /// Calculates the quadrant (0 through 3) of the specified angle. If the angle lies on an edge
        /// (0, 90, 180, etc.) the result depends on the details how the angle is used.
        /// </summary>
        static int Quadrant(double φ, bool start, bool clockwise)
        {
          //  Debug.Assert(φ >= 0);
            if (φ > 360)
                φ = φ - Math.Floor(φ / 360) * 360;

            int quadrant = (int)(φ / 90);
            if (quadrant * 90 == φ)
            {
                if ((start && !clockwise) || (!start && clockwise))
                    quadrant = quadrant == 0 ? 3 : quadrant - 1;
            }
            else
                quadrant = clockwise ? ((int)Math.Floor(φ / 90)) % 4 : (int)Math.Floor(φ / 90);
            return quadrant;
        }

        /// <summary>
        /// Appends a Bézier curve for an arc within a full quadrant.
        /// </summary>
        static void AppendPartialArcQuadrant(List<XPoint> points, double x, double y, double width, double height, double α, double β, PathStart pathStart, XMatrix matrix)
        {
            //Debug.Assert(α >= 0 && α <= 360);
            //Debug.Assert(β >= 0);
            if (β > 360)
                β = β - Math.Floor(β / 360) * 360;
            //Debug.Assert(Math.Abs(α - β) <= 90);

            // Scanling factor.
            double δx = width / 2;
            double δy = height / 2;

            // Center of ellipse.
            double x0 = x + δx;
            double y0 = y + δy;

            // We have the following quarters:
            //     |
            //   2 | 3
            // ----+-----
            //   1 | 0
            //     |
            // If the angles lie in quarter 2 or 3, their values are subtracted by 180 and the
            // resulting curve is reflected at the center. This algorithm works as expected (simply tried out).
            // There may be a mathematically more elegant solution...
            bool reflect = false;
            if (α >= 180 && β >= 180)
            {
                α -= 180;
                β -= 180;
                reflect = true;
            }

            double cosα, cosβ, sinα, sinβ;
            if (width == height)
            {
                // Circular arc needs no correction.
                α = α * Deg2Rad;
                β = β * Deg2Rad;
            }
            else
            {
                // Elliptic arc needs the angles to be adjusted such that the scaling transformation is compensated.
                α = α * Deg2Rad;
                sinα = Math.Sin(α);
                if (Math.Abs(sinα) > 1E-10)
                    α = Math.PI / 2 - Math.Atan(δy * Math.Cos(α) / (δx * sinα));
                β = β * Deg2Rad;
                sinβ = Math.Sin(β);
                if (Math.Abs(sinβ) > 1E-10)
                    β = Math.PI / 2 - Math.Atan(δy * Math.Cos(β) / (δx * sinβ));
            }

            double κ = 4 * (1 - Math.Cos((α - β) / 2)) / (3 * Math.Sin((β - α) / 2));
            sinα = Math.Sin(α);
            cosα = Math.Cos(α);
            sinβ = Math.Sin(β);
            cosβ = Math.Cos(β);

            //XPoint pt1, pt2, pt3;
            if (!reflect)
            {
                // Calculation for quarter 0 and 1.
                switch (pathStart)
                {
                    case PathStart.MoveTo1st:
                        points.Add(matrix.Transform(new XPoint(x0 + δx * cosα, y0 + δy * sinα)));
                        break;

                    case PathStart.LineTo1st:
                        points.Add(matrix.Transform(new XPoint(x0 + δx * cosα, y0 + δy * sinα)));
                        break;

                    case PathStart.Ignore1st:
                        break;
                }
                points.Add(matrix.Transform(new XPoint(x0 + δx * (cosα - κ * sinα), y0 + δy * (sinα + κ * cosα))));
                points.Add(matrix.Transform(new XPoint(x0 + δx * (cosβ + κ * sinβ), y0 + δy * (sinβ - κ * cosβ))));
                points.Add(matrix.Transform(new XPoint(x0 + δx * cosβ, y0 + δy * sinβ)));
            }
            else
            {
                // Calculation for quarter 2 and 3.
                switch (pathStart)
                {
                    case PathStart.MoveTo1st:
                        points.Add(matrix.Transform(new XPoint(x0 - δx * cosα, y0 - δy * sinα)));
                        break;

                    case PathStart.LineTo1st:
                        points.Add(matrix.Transform(new XPoint(x0 - δx * cosα, y0 - δy * sinα)));
                        break;

                    case PathStart.Ignore1st:
                        break;
                }
                points.Add(matrix.Transform(new XPoint(x0 - δx * (cosα - κ * sinα), y0 - δy * (sinα + κ * cosα))));
                points.Add(matrix.Transform(new XPoint(x0 - δx * (cosβ + κ * sinβ), y0 - δy * (sinβ - κ * cosβ))));
                points.Add(matrix.Transform(new XPoint(x0 - δx * cosβ, y0 - δy * sinβ)));
            }
        }


        /// <summary>
        /// Indicates how to handle the first point of a path.
        /// </summary>
        internal enum PathStart
        {
            /// <summary>
            /// Set the current position to the first point.
            /// </summary>
            MoveTo1st,

            /// <summary>
            /// Draws a line to the first point.
            /// </summary>
            LineTo1st,

            /// <summary>
            /// Ignores the first point.
            /// </summary>
            Ignore1st,
        }
    }
}