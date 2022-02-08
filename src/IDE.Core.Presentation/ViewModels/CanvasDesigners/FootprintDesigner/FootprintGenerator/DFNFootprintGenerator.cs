﻿using IDE.Core.Common;
using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Storage;
using IDE.Core.Types.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDE.Documents.Views
{
    public class DFNFootprintGenerator : FootprintGenerator
    {
        public DFNFootprintGenerator(ILayeredViewModel doc)
                  : base(doc)
        {

        }

        public override string Name => "DFN";

        int numberPads = 8;
        /// <summary>
        /// Pin pitch
        /// </summary>
        public int NumberPads
        {
            get { return numberPads; }
            set
            {
                numberPads = value;
                OnPropertyChanged(nameof(NumberPads));
            }
        }

        double e = 3d;
        /// <summary>
        /// Body width
        /// </summary>
        public double E
        {
            get { return e; }
            set
            {
                e = value;
                OnPropertyChanged(nameof(E));
            }
        }

        double d = 3d;
        /// <summary>
        /// Length of the package. Includes the length of the pads
        /// </summary>
        public double D
        {
            get { return d; }
            set
            {
                d = value;
                OnPropertyChanged(nameof(D));
            }
        }



        double ee = 0.5d;
        /// <summary>
        /// e - pin pitch
        /// </summary>
        public double EE//e
        {
            get { return ee; }
            set
            {
                ee = value;
                OnPropertyChanged(nameof(EE));
            }
        }

        double l = 0.5d;
        /// <summary>
        /// Length of landing pad in mm
        /// </summary>
        public double L
        {
            get { return l; }
            set
            {
                l = value;
                OnPropertyChanged(nameof(L));
            }
        }

        double b = 0.3d;
        /// <summary>
        /// Pad width in mm
        /// </summary>
        public double B
        {
            get { return b; }
            set
            {
                b = value;
                OnPropertyChanged(nameof(B));
            }
        }

        public override Task<List<BaseCanvasItem>> GenerateFootprint()
        {
            return Task.Run(() =>
            {
                var newItems = new List<BaseCanvasItem>();

                var origin = new XPoint(D, D);
                var edgeSize = NumberPads / 2;//8/2=4
                var padOffset = 0.5 * (D - (edgeSize - 1) * EE);

                for (int padIndex = 0; padIndex < NumberPads; padIndex++)
                {

                    var padEdgeIndex = padIndex / edgeSize;
                    var indexInEdge = padIndex % edgeSize;


                    var x = 0.0d;
                    var y = 0.0d;

                    switch (padEdgeIndex)
                    {
                        case 0://vertical -down
                            {
                                x = -0.5 * (E - L - 0.01);
                                y = -0.5 * D + padOffset + indexInEdge * EE;
                                break;
                            }
                        case 1://vertical up
                            {
                                x = 0.5 * (E - L - 0.01);
                                y = -0.5 * D + padOffset + (edgeSize - 1 - indexInEdge) * EE;
                                break;
                            }
                    }

                    var padPrimitive = new Smd
                    {
                        x = origin.X + x,
                        y = origin.Y + y,
                        Width = L,
                        Height = b,
                        number = (padIndex + 1).ToString(),

                    };


                    var pad = (BoardCanvasItemViewModel)padPrimitive.CreateDesignerItem();
                    (pad as SingleLayerBoardCanvasItem).LayerId = LayerConstants.SignalTopLayerId;
                    newItems.Add(pad);
                }

                //body
                CreateBodySilk(newItems, origin);



                return newItems;
            });
        }

        void CreateBodySilk(List<BaseCanvasItem> newItems, XPoint origin)
        {
            var primitives = new List<LayerPrimitive>();
            primitives.Add(new LineBoard
            {
                x1 = origin.X - 0.5 * e,
                y1 = origin.Y - 0.5 * d,
                x2 = origin.X + 0.5 * e,
                y2 = origin.Y - 0.5 * d,
            });

            primitives.Add(new LineBoard
            {
                x1 = origin.X - 0.5 * e,
                y1 = origin.Y + 0.5 * d,
                x2 = origin.X + 0.5 * e,
                y2 = origin.Y + 0.5 * d,
            });

            primitives.Add(new LineBoard
            {
                x1 = origin.X - 0.5 * e,
                y1 = origin.Y - 0.5 * d,
                x2 = origin.X - 0.5 * e,
                y2 = origin.Y + 0.5 * d,
            });

            primitives.Add(new LineBoard
            {
                x1 = origin.X + 0.5 * e,
                y1 = origin.Y - 0.5 * d,
                x2 = origin.X + 0.5 * e,
                y2 = origin.Y + 0.5 * d,
            });


            foreach (var primitive in primitives)
            {
                var line = (BoardCanvasItemViewModel)primitive.CreateDesignerItem();
                (line as SingleLayerBoardCanvasItem).LayerId = LayerConstants.SilkscreenTopLayerId;
                newItems.Add(line);
            }
        }
    }
}
