﻿using IDE.Core.Designers;
using IDE.Core.Types.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace IDE.Documents.Views
{
    public class DFNPackageGenerator : PackageGenerator
    {
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

        double a = 0.8;
        /// <summary>
        /// Height of the package
        /// </summary>
        public double A
        {
            get { return a; }
            set
            {
                a = value;
                OnPropertyChanged(nameof(A));
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


        public override Task<List<BaseMeshItem>> GeneratePackage()
        {
            return Task.Run(() =>
            {
                var meshItems = new List<BaseMeshItem>();

                //body
                meshItems.Add(new BoxMeshItem
                {
                    X = 0,
                    Y = 0,
                    Z = 0.5 * A + 0.01,
                    Width = D,
                    Length = E,
                    Height = A,
                    CanEdit = false,
                    IsPlaced = false,
                    //RotationZ = 90,
                    FillColor = XColors.Black
                });

                var edgeSize = NumberPads / 2;//8/2=4
                var padOffset = 0.5 * (D - (edgeSize - 1) * EE);

                for (int padIndex = 0; padIndex < NumberPads; padIndex++)
                {

                    var padEdgeIndex = padIndex / edgeSize;
                    var indexInEdge = padIndex % edgeSize;


                    var x = 0.0d;
                    var y = 0.0d;
                    // var z = 0.0d;
                    // var rot = 0.0d;

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
                                //rot = 180;
                                x = 0.5 * (E - L - 0.01);
                                y = -0.5 * D + padOffset + (edgeSize - 1 - indexInEdge) * EE;
                                break;
                            }
                    }

                    var pad = new BoxMeshItem
                    {
                        FillColor = XColors.Silver,
                        PadNumber = (padIndex + 1),
                        // RotationZ = rot,
                        X = x,
                        Y = y,
                        Z = 0.1,
                        Length = L,
                        Width = B,
                        Height = 0.2,
                        IsPlaced = false,
                        CanEdit = false
                    };

                    meshItems.Add(pad);
                }

                //pin 1 mark
                meshItems.Add(new CylinderMeshItem
                {
                    X = -0.5 * E + 0.5 * padOffset,
                    Y = -0.5 * D + padOffset,
                    Z = A - 0.1,
                    Radius = 0.2,
                    Height = 0.15,
                    CanEdit = false,
                    IsPlaced = false,
                    FillColor = XColors.WhiteSmoke
                });

                return meshItems;
            });
        }
    }
}
