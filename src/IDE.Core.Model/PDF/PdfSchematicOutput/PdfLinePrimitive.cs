﻿using System.Linq;
using System.Collections.Generic;
using IDE.Core.Interfaces;
using IDE.Core.Types.Media;

namespace IDE.Core.PDF
{
    internal class PdfLinePrimitive : PdfPrimitive
    {
        public XPoint StartPoint { get; set; }
        public XPoint EndPoint { get; set; }
        public double Width { get; set; }

        public LineStyle LineStyle { get; set; } = LineStyle.Solid;

        public override void WriteTo(IPdfDocument pdfDoc)
        {
            double[] dashArray = null;
            switch (LineStyle)
            {
                case LineStyle.Dash:
                    dashArray = new[] { 2.0, 3.0 };
                    break;

                case LineStyle.Dot:
                    dashArray = new[] { 0.0, 2 };
                    break;

                case LineStyle.DashDot:
                    dashArray = new[] { 2.0, 2.0, 0.0, 2.0 };
                    break;
            }

            pdfDoc.DrawLine(new List<XPoint> { StartPoint, EndPoint }, Color, Width, dashArray);
        }
    }
}