﻿using IDE.Core.Types.Media;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDE.Core.Model.Gerber.Primitives.Apertures;


namespace IDE.Core.Gerber
{
    public class GerberLinePrimitive : GerberPrimitive
    {
        public XPoint StartPoint { get; set; }

        public XPoint EndPoint { get; set; }

        public double Width { get; set; }

        public GerberLineCap LineCap { get; set; } = GerberLineCap.Round;

        protected override void CreateApertures()
        {
            switch (LineCap)
            {
                case GerberLineCap.Round:
                    cachedApertures.Add(new ApertureDefinitionCircle { Diameter = Width });
                    break;

                case GerberLineCap.Square:
                    cachedApertures.Add(new ApertureDefinitionRectangle { Width = Width, Height = Width });
                    break;
            }

        }

        public override void WriteGerber(Gerber274XWriter gerberWriter)
        {
            gerberWriter.SelectAperture(cachedApertures[0].Number);
            gerberWriter.SetLevelPolarity(Polarity);

            WriteGerberShape(gerberWriter);
        }

        internal override void WriteGerberShape(Gerber274XWriter gerberWriter)
        {
            gerberWriter.SetLinearInterpolation();
            gerberWriter.MoveTo(StartPoint.X, StartPoint.Y);
            gerberWriter.InterpolateTo(EndPoint.X, EndPoint.Y);
        }
    }

    public enum GerberLineCap
    {
        Round,
        Square
    }
}
