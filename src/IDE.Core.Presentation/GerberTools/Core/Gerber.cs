﻿using ClipperLib;
using GerberLibrary.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
//using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GerberLibrary.Core;
using IDE.Core.Types.Media;
using System.IO.Compression;

namespace GerberLibrary
{

    public class BoardRenderColorSet
    {
        public Color BoardRenderBaseMaterialColor = ParseColor("#808080");
        public Color BoardRenderColor = ParseColor("green");
        public Color BoardRenderCopperColor = Color.FromArgb(219, 125, 104);
        public Color BoardRenderPadColor = ParseColor("gold");
        public Color BoardRenderSilkColor = ParseColor("white");

        public Color BackgroundColor = Color.FromArgb(10, 10, 40);
        public Color BoardRenderTraceColor = ParseColor("green");
        public void SetupColors(string SolderMaskColor, string SilkScreenColor, string TracesColor = "auto", string CopperColor = "gold")
        {
            BoardRenderColor = ParseColor(SolderMaskColor);
            BoardRenderSilkColor = ParseColor(SilkScreenColor);
            BoardRenderPadColor = ParseColor(CopperColor);
            BoardRenderTraceColor = ParseColor(TracesColor);
        }
        public Color GetDefaultColor(BoardLayer layer, BoardSide side)
        {
            switch (layer)
            {
                case BoardLayer.Drill: return BackgroundColor;
                case BoardLayer.Copper: return BoardRenderCopperColor;
                case BoardLayer.Outline: return BoardRenderColor;
                case BoardLayer.SolderMask: return MathHelpers.Interpolate(BoardRenderColor, BoardRenderBaseMaterialColor, 0.2f);
                case BoardLayer.Silk: return BoardRenderSilkColor;
            }
            return Color.FromArgb(100, 255, 255, 255);
        }

        static Color ParseColor(string color)
        {
            if (color == null)
            {
                Console.WriteLine("Error: Null color! Defaulting to lime!");
                return Color.Lime;
            }

            switch (color.ToLower())
            {
                case "blue": return Color.FromArgb(0, 40, 74);
                case "yellow": return Color.FromArgb(234, 206, 39);
                case "green": return Color.FromArgb(68, 105, 80);
                case "black": return Color.FromArgb(5, 5, 5);
                case "white": return Color.FromArgb(250, 250, 250);
                case "red": return Color.FromArgb(192, 43, 43);
                case "silver": return Color.FromArgb(160, 160, 160);
                case "gold": return Color.FromArgb(239, 205, 85);
            }

            try
            {
                var xcolor = XColor.FromHexString(color);
                return Color.FromArgb(xcolor.R, xcolor.G, xcolor.B);
            }
            catch (Exception)
            {
                // unknown colors end up here... no need to worry, just pass it on to the default color handler which returns 0,0,0 as error-color if it too cant find anything.
            }

            return Color.FromName(color);
        }
    }
    public static class Gerber
    {
        #region GERBERPROCESSINGDEFAULTS
        public static double ArcQualityScaleFactor = 20;

        public static bool DirectlyShowGeneratedBoardImages = true;
        public static bool DumpSanitizedOutput = false;
        public static string EOF = "M02*";
        public static bool ExtremelyVerbose = false;
        public static bool GerberRenderBumpMapOutput = true;
        public static string INCH = "%MOIN*%";
        public static string LinearInterpolation = "G01*";
        public static string LineEnding = "\n";
        public static string MM = "%MOMM*%";
        public static bool SaveDebugImageOutput = false;
        public static bool SaveIntermediateImages = false;
        public static bool SaveOutlineImages = false;
        public static bool ShowProgress = false;
        public static string StartRegion = "G36*";
        public static string StopRegion = "G37*";
        public static bool WaitForKey = false;
        public static bool WriteSanitized = false;
        #endregion

        public static void ZipGerberFolderToFactoryFolder(string Name, string BoardGerbersFolder, string BoardFactoryFolder)
        {
            if (Directory.Exists(BoardGerbersFolder))
            {
                string targetZip = Path.Combine(BoardFactoryFolder, Name + "_gerbers.zip");
                if (File.Exists(targetZip)) File.Delete(targetZip);
                Console.WriteLine("Zipping gerbers to {0}", targetZip);
                using (var fs = new FileStream(targetZip, FileMode.Create))
                {
                    using (var zipFile = new ZipArchive(fs, ZipArchiveMode.Create, false))
                    {
                        foreach (var F in Directory.GetFiles(BoardGerbersFolder))
                        {
                            bool addToZip = false;
                            var T = GerberLibrary.Gerber.FindFileType(F);
                            if (T == BoardFileType.Drill)
                            {
                                addToZip = true;
                            }
                            else
                            {
                                GerberLibrary.Gerber.DetermineBoardSideAndLayer(F, out BoardSide Side, out BoardLayer Layer);
                                switch (Layer)
                                {
                                    case BoardLayer.Carbon:
                                    case BoardLayer.Mill:
                                    case BoardLayer.Outline:
                                    case BoardLayer.Paste:
                                    case BoardLayer.Silk:
                                    case BoardLayer.SolderMask:
                                    case BoardLayer.Copper:
                                    case BoardLayer.Drill:
                                        addToZip = true;
                                        break;
                                    case BoardLayer.Assembly:
                                        var TargetGerb = Path.Combine(BoardFactoryFolder, Name + "_" + Path.GetFileName(F));
                                        File.Copy(F, TargetGerb, true);
                                        break;
                                }
                            }
                            if (addToZip)
                            {
                                Console.WriteLine("Adding {0} to zip.", F);
                                var entry = zipFile.CreateEntryFromFile(F, Path.GetFileName(F));
                            }
                        }

                    }
                  //  Z.Save(targetZip);
                }

            }
        }
        public static int GetDefaultSortOrder(BoardSide side, BoardLayer layer)
        {
            int R = 0;
            switch (layer)
            {

                case BoardLayer.Mill: R = 11; break;
                case BoardLayer.Silk: R = 101; break;
                case BoardLayer.Paste: R = 10; break;
                case BoardLayer.SolderMask: R = 102; break;
                case BoardLayer.Copper: R = 100; break;
                case BoardLayer.Carbon: R = 103; break;
            }
            if (side == BoardSide.Bottom) R *= -1;
            return R;
        }

        private static readonly Regex rxScientific = new Regex(@"^(?<sign>-?)(?<head>\d+)(\.(?<tail>\d*?)0*)?E(?<exponent>[+\-]\d+)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static bool SkipEagleDrillFix = false;

        public static bool ThrowExceptions = false; // set to true to make the debugger die in useful places.

        public static List<PointD> CreateCurvePoints(double LastX, double LastY, double X, double Y, double I, double J, InterpolationMode mode, GerberQuadrantMode qmode)
        {
            //   Console.WriteLine("Current curve mode: {0}", qmode);
            List<PointD> R = new List<PointD>();

            double Radius = Math.Sqrt(I * I + J * J);
            double CX = LastX + I;
            double CY = LastY + J;

            Quadrant Q = Quadrant.xposypos;

            double HS = Math.Atan2(LastY - CY, LastX - CX);
            double HE = Math.Atan2(Y - CY, X - CX);
            if (qmode == GerberQuadrantMode.Multi)
            {

                if (mode == InterpolationMode.ClockWise)
                {
                    while (HS <= HE) HS += Math.PI * 2;
                }
                else
                {
                    while (HS >= HE) HS -= Math.PI * 2;
                }

            }
            else
            {
                double LastDiff = Math.PI * 2;
                List<QuadR> qR = new List<QuadR>();
                qR.Add(new QuadR() { CX = LastX + I, CY = LastY + J });
                qR.Add(new QuadR() { CX = LastX - I, CY = LastY + J });
                qR.Add(new QuadR() { CX = LastX - I, CY = LastY - J });
                qR.Add(new QuadR() { CX = LastX + I, CY = LastY - J });

                foreach (var a in qR) a.Calc(LastX, LastY, X, Y);
                int candidates = 0;


                if (Gerber.ExtremelyVerbose)
                {
                    var DX = LastX - X;

                    var DY = LastY - Y;
                    var L = Math.Sqrt(DX * DX + DY * DY);
                    if (L < 1.0)
                    {


                        R.Add(new PointD(X, Y));

                        return R;
                    }

                    Console.WriteLine("length: {0}", L);
                }
                if (mode == InterpolationMode.CounterClockwise)
                {

                    double LastRat = 10;
                    foreach (var a in qR) a.FixCounterClockwise();
                    foreach (var a in qR)
                    {
                        if (a.Diff <= Math.PI / 2.0)
                        {
                            candidates++;
                            if (Math.Abs(1 - a.DRat) < LastRat)
                            {
                                CX = a.CX;
                                CY = a.CY;
                                HS = a.S;
                                HE = a.E;
                                LastRat = Math.Abs(1 - a.DRat);
                            }

                            if (Gerber.ExtremelyVerbose) Console.WriteLine("candidate: {0:N1} - {1:N1} - {2:N1}", RadToDeg(a.S), RadToDeg(a.E), RadToDeg(a.Diff));
                        }
                    }
                    /*
                                        HS = qR[3].S;
                                        CX = qR[3].CX;
                                        CY = qR[3].CY;
                                        HE = qR[3].E;
                                        */

                }
                else
                {

                    foreach (var a in qR) a.FixClockwise();

                    foreach (var a in qR)
                    {
                        if (a.Diff >= 0 && a.Diff <= Math.PI / 2.0 + 0.00001)
                        {
                            candidates++;
                            if (Math.Abs(a.Diff) < LastDiff)
                            {
                                CX = a.CX;
                                CY = a.CY;
                                HS = a.S;
                                HE = a.E;
                                LastDiff = Math.Abs(a.Diff);
                            }

                            if (Gerber.ExtremelyVerbose) Console.WriteLine("candidate: {0} - {1} - {2}", a.S, a.E, a.Diff);
                        }
                        if (Gerber.ExtremelyVerbose) Console.WriteLine("selected : {0} - {1} - {2}", HS, HE, LastDiff);

                    }

                }

                if (candidates == 0 && Gerber.ExtremelyVerbose)
                {
                    foreach (var a in qR)
                    {
                        Console.WriteLine("no candidate: {0} - {1} - {2}  ( should be smaller than {3}) ", a.S, a.E, a.Diff, Math.PI / 2.0);
                    }
                }

            }
            if (Gerber.ExtremelyVerbose)
            {
                Console.WriteLine("HS {0:N1}  HE {1:N1} DIFF {2:N1} QUAD {3} CX {4} CY {5}", RadToDeg(HS), RadToDeg(HE), RadToDeg(HE - HS), Q, CX, CY);


            }
            int segs = (int)(Gerber.ArcQualityScaleFactor * Math.Max(2.0, Radius) * Math.Abs(HS - HE) / (Math.PI * 2));

            if (segs < 10) segs = 10;

            double HEdeg = RadToDeg(HE);

            double HSdeg = RadToDeg(HS);
            for (int i = 0; i <= segs; i++)
            {
                double P = ((double)i / (double)segs) * (HE - HS) + HS;
                double nx = Math.Cos(P) * Radius + CX;
                double ny = Math.Sin(P) * Radius + CY;
                R.Add(new PointD(nx, ny));
            }

            //    R.Add(new PointD(X, Y));

            return R;
        }

        public static void DetermineBoardSideAndLayer(string gerberfile, out BoardSide Side, out BoardLayer Layer)
        {
            Side = BoardSide.Unknown;
            Layer = BoardLayer.Unknown;
            string[] filesplit = Path.GetFileName(gerberfile).Split('.');
            string ext = filesplit[filesplit.Count() - 1].ToLower();
            switch (ext)
            {
                case "art": // ORCAD RELATED TYPES
                    {

                        switch (Path.GetFileNameWithoutExtension(gerberfile).ToUpper())
                        {
                            case "PMT": Side = BoardSide.Top; Layer = BoardLayer.Paste; break;
                            case "PMB": Side = BoardSide.Bottom; Layer = BoardLayer.Paste; break;
                            case "TOP": Side = BoardSide.Top; Layer = BoardLayer.Copper; break;
                            case "BOTTOM": Side = BoardSide.Bottom; Layer = BoardLayer.Copper; break;
                            case "SMBOT": Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; break;
                            case "SMTOP": Side = BoardSide.Top; Layer = BoardLayer.SolderMask; break;
                            case "SSBOT": Side = BoardSide.Bottom; Layer = BoardLayer.Silk; break;
                            case "SSTOP": Side = BoardSide.Top; Layer = BoardLayer.Silk; break;

                            case "DRILLING": Side = BoardSide.Both; Layer = BoardLayer.Drill; break;
                                //                            case "KEEPOUT": Side = BoardSide.Both; Layer = BoardLayer.Outline; break;
                        }
                        break;
                    }
                case "slices": Side = BoardSide.Both; Layer = BoardLayer.Utility; break;
                case "copper_bottom": Side = BoardSide.Bottom; Layer = BoardLayer.Copper; break;
                case "copper_top": Side = BoardSide.Top; Layer = BoardLayer.Copper; break;
                case "silk_bottom": Side = BoardSide.Bottom; Layer = BoardLayer.Silk; break;
                case "silk_top": Side = BoardSide.Top; Layer = BoardLayer.Silk; break;
                case "paste_bottom": Side = BoardSide.Bottom; Layer = BoardLayer.Paste; break;
                case "paste_top": Side = BoardSide.Top; Layer = BoardLayer.Paste; break;
                case "soldermask_bottom": Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; break;
                case "soldermask_top": Side = BoardSide.Top; Layer = BoardLayer.SolderMask; break;
                case "drill_both": Side = BoardSide.Both; Layer = BoardLayer.Drill; break;
                case "outline_both": Side = BoardSide.Both; Layer = BoardLayer.Outline; break;
                case "png":
                    {
                        Side = BoardSide.Both;
                        Layer = BoardLayer.Silk;
                    }
                    break;

                case "assemblytop":
                    Layer = BoardLayer.Assembly;
                    Side = BoardSide.Top;
                    break;
                case "assemblybottom":
                    Layer = BoardLayer.Assembly;
                    Side = BoardSide.Bottom;
                    break;
                case "gbr":

                    switch (Path.GetFileNameWithoutExtension(gerberfile).ToLower())
                    {
                        case "boardoutline":
                            Side = BoardSide.Both;
                            Layer = BoardLayer.Outline;
                            break;
                        case "outline":
                            Side = BoardSide.Both;
                            Layer = BoardLayer.Outline;
                            break;

                        case "board":
                            Side = BoardSide.Both;
                            Layer = BoardLayer.Outline;
                            break;
                        case "bottom":
                            Side = BoardSide.Bottom;
                            Layer = BoardLayer.Copper;
                            break;
                        case "bottommask":
                            Side = BoardSide.Bottom;
                            Layer = BoardLayer.SolderMask;
                            break;
                        case "bottompaste":
                            Side = BoardSide.Bottom;
                            Layer = BoardLayer.Paste;
                            break;
                        case "bottomsilk":
                            Side = BoardSide.Bottom;
                            Layer = BoardLayer.Silk;
                            break;
                        case "top":
                            Side = BoardSide.Top;
                            Layer = BoardLayer.Copper;
                            break;
                        case "topmask":
                            Side = BoardSide.Top;
                            Layer = BoardLayer.SolderMask;
                            break;
                        case "toppaste":
                            Side = BoardSide.Top;
                            Layer = BoardLayer.Paste;
                            break;
                        case "topsilk":
                            Side = BoardSide.Top;
                            Layer = BoardLayer.Silk;
                            break;
                        case "inner1":
                            Side = BoardSide.Internal1;
                            Layer = BoardLayer.Copper;
                            break;
                        case "inner2":
                            Side = BoardSide.Internal2;
                            Layer = BoardLayer.Copper;
                            break;

                        default:

                            {
                                string lcase = gerberfile.ToLower();
                                if (lcase.Contains("board outline")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; };

                                if (lcase.Contains("copper bottom")) { Side = BoardSide.Bottom; Layer = BoardLayer.Copper; };
                                if (lcase.Contains("silkscreen bottom")) { Side = BoardSide.Bottom; Layer = BoardLayer.Silk; };
                                if (lcase.Contains("copper top")) { Side = BoardSide.Top; Layer = BoardLayer.Copper; };
                                if (lcase.Contains("silkscreen top")) { Side = BoardSide.Top; Layer = BoardLayer.Silk; };

                                if (lcase.Contains("solder mask bottom")) { Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; };
                                if (lcase.Contains("solder mask top")) { Side = BoardSide.Top; Layer = BoardLayer.SolderMask; };

                                if (lcase.Contains("drill-copper top-copper bottom")) { Side = BoardSide.Both; Layer = BoardLayer.Drill; };

                                if (lcase.Contains("outline")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; }
                                if (lcase.Contains("-edge_cuts")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; }
                                if (lcase.Contains("-b_cu")) { Side = BoardSide.Bottom; Layer = BoardLayer.Copper; }
                                if (lcase.Contains("-f_cu")) { Side = BoardSide.Top; Layer = BoardLayer.Copper; }
                                if (lcase.Contains("-b_silks")) { Side = BoardSide.Bottom; Layer = BoardLayer.Silk; }
                                if (lcase.Contains("-f_silks")) { Side = BoardSide.Top; Layer = BoardLayer.Silk; }
                                if (lcase.Contains("-b_mask")) { Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; }
                                if (lcase.Contains("-f_mask")) { Side = BoardSide.Top; Layer = BoardLayer.SolderMask; }
                                if (lcase.Contains("-b_paste")) { Side = BoardSide.Bottom; Layer = BoardLayer.Paste; }
                                if (lcase.Contains("-f_paste")) { Side = BoardSide.Top; Layer = BoardLayer.Paste; }
                            }
                            break;


                    }
                    break;

                case "ger":
                    {
                        string l = gerberfile.ToLower();
                        List<boardset> bs = new List<boardset>();
                        bs.Add(new boardset() { name = ".topsoldermask", side = BoardSide.Top, layer = BoardLayer.SolderMask });
                        bs.Add(new boardset() { name = ".topsilkscreen", side = BoardSide.Top, layer = BoardLayer.Silk });
                        bs.Add(new boardset() { name = ".toplayer", side = BoardSide.Top, layer = BoardLayer.Copper });
                        bs.Add(new boardset() { name = ".tcream", side = BoardSide.Top, layer = BoardLayer.Paste });
                        bs.Add(new boardset() { name = ".boardoutline", side = BoardSide.Both, layer = BoardLayer.Outline });
                        bs.Add(new boardset() { name = ".bcream", side = BoardSide.Bottom, layer = BoardLayer.SolderMask });
                        bs.Add(new boardset() { name = ".bottomsoldermask", side = BoardSide.Bottom, layer = BoardLayer.SolderMask });
                        bs.Add(new boardset() { name = ".bottomsilkscreen", side = BoardSide.Bottom, layer = BoardLayer.Silk });
                        bs.Add(new boardset() { name = ".bottomlayer", side = BoardSide.Bottom, layer = BoardLayer.Copper });
                        bs.Add(new boardset() { name = ".bcream", side = BoardSide.Bottom, layer = BoardLayer.Paste });

                        bs.Add(new boardset() { name = ".internalplane1", side = BoardSide.Internal1, layer = BoardLayer.Copper });
                        bs.Add(new boardset() { name = ".internalplane2", side = BoardSide.Internal2, layer = BoardLayer.Copper });

                        foreach (var a in bs)
                        {
                            if (l.Contains(a.name))
                            {
                                Side = a.side;
                                Layer = a.layer;
                            }
                        }
                    }
                    break;

                case "gml":
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Mill;
                    break;
                case "fabrd":
                case "oln":
                case "gko":
                case "gm1":
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Outline;
                    break;
                case "l2":
                case "gl1":
                    Side = BoardSide.Internal1;
                    Layer = BoardLayer.Copper;
                    break;
                case "adtop":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Assembly;
                    break;
                case "adbottom":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Assembly;
                    break;
                case "notes":
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Notes;
                    break;
                case "l3":

                case "gl2":
                    Side = BoardSide.Internal2;
                    Layer = BoardLayer.Copper;
                    break;

                case "l4":
                case "gbl":
                case "l2m":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Copper;
                    break;

                case "l1":
                case "l1m":
                case "gtl":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Copper;
                    break;

                case "gbp":
                case "spbottom":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Paste;
                    break;

                case "gtp":
                case "sptop":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Paste;
                    break;

                case "gbo":
                case "ss2":
                case "ssbottom":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Silk;
                    break;

                case "gto":
                case "ss1":
                case "sstop":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Silk;
                    break;

                case "gbs":
                case "sm2":
                case "smbottom":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.SolderMask;
                    break;

                case "gts":
                case "sm1":
                case "smtop":

                    Side = BoardSide.Top;
                    Layer = BoardLayer.SolderMask;
                    break;

                case "gb3": // oshstencils bottom outline
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Outline;
                    break;

                case "gt3": // oshstencils top outline
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Outline;
                    break;

                case "top":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Copper;
                    break;

                case "bottom":
                case "bot":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Copper;
                    break;

                case "smb":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.SolderMask;
                    break;

                case "smt":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.SolderMask;
                    break;

                case "slk":
                case "sst":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Silk;
                    break;

                case "bsk":
                case "ssb":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Silk;
                    break;

                case "spt":
                    Side = BoardSide.Top;
                    Layer = BoardLayer.Paste;
                    break;

                case "spb":
                    Side = BoardSide.Bottom;
                    Layer = BoardLayer.Paste;
                    break;
                case "drill_TOP_BOTTOM":
                case "drl":
                case "drill":
                case "drillnpt":
                    Side = BoardSide.Both;
                    Layer = BoardLayer.Drill;
                    break;

            }
        }

        public static BoardFileType FindFileType(string filename)
        {
            //filename = filename.ToLower();
            List<string> unsupported = new List<string>() { "config", "exe", "dll", "png", "zip", "gif", "jpeg", "doc", "docx", "jpg", "bmp" };
            string[] filesplit = filename.Split('.');
            string ext = filesplit[filesplit.Count() - 1].ToLower();
            foreach (var s in unsupported)
            {
                if (ext == s)
                {

                    return BoardFileType.Unsupported;
                }
            }
            try
            {
                // var F = File.OpenText(a);
                var F = File.ReadAllLines(filename);
                for (int i = 0; i < F.Count(); i++)
                {
                    string L = F[i];
                    if (L.Contains("%FS")) return BoardFileType.Gerber;
                    if (L.Contains("M48")) return BoardFileType.Drill;
                };


            }
            catch (Exception E)
            {
                if (Gerber.ExtremelyVerbose)
                {
                    Console.WriteLine("Exception determining filetype: {0}", E.Message);
                }
                return BoardFileType.Unsupported;
            }

            return BoardFileType.Unsupported;


        }

        public static BoardFileType FindFileTypeFromStream(StreamReader l, string filename)
        {
            filename = filename.ToLower();
            List<string> unsupported = new List<string>() { "config", "exe", "dll", "png", "zip", "gif", "jpeg", "doc", "docx", "jpg", "bmp" };
            string[] filesplit = filename.Split('.');
            string ext = filesplit[filesplit.Count() - 1].ToLower();
            foreach (var s in unsupported)
            {
                if (ext == s)
                {

                    return BoardFileType.Unsupported;
                }
            }
            try
            {
                // var F = File.OpenText(a);
                List<string> lines = new List<string>();
                while (!l.EndOfStream)
                {
                    lines.Add(l.ReadLine());
                }
                //var F = File.ReadAllLines(filename);


                for (int i = 0; i < lines.Count(); i++)
                {
                    string L = lines[i];
                    if (L.Contains("%FS")) return BoardFileType.Gerber;
                    if (L.Contains("M48")) return BoardFileType.Drill;
                };


            }
            catch (Exception)
            {
                return BoardFileType.Unsupported;
            }

            return BoardFileType.Unsupported;

        }

        public static Bounds GetBoundingBox(List<string> generatedFiles)
        {
            Bounds A = new Bounds();

            foreach (var a in generatedFiles)
            {
                ParsedGerber PLS = PolyLineSet.LoadGerberFile(a, State: new GerberParserState() { PreCombinePolygons = false });
                A.AddBox(PLS.BoundingBox);
            }
            return A;
        }




        public static double RadToDeg(double inp)
        {
            return inp * 360.0 / (Math.PI * 2.0);
        }


        public static string ToFloatingPointString(double value)
        {
            return ToFloatingPointString(value, NumberFormatInfo.CurrentInfo);
        }

        public static string ToFloatingPointString(double value, NumberFormatInfo formatInfo)
        {
            string result = value.ToString("r", NumberFormatInfo.InvariantInfo);
            Match match = rxScientific.Match(result);
            if (match.Success)
            {
                Debug.WriteLine("Found scientific format: {0} => [{1}] [{2}] [{3}] [{4}]", result, match.Groups["sign"], match.Groups["head"], match.Groups["tail"], match.Groups["exponent"]);
                int exponent = int.Parse(match.Groups["exponent"].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
                StringBuilder builder = new StringBuilder(result.Length + Math.Abs(exponent));
                builder.Append(match.Groups["sign"].Value);
                if (exponent >= 0)
                {
                    builder.Append(match.Groups["head"].Value);
                    string tail = match.Groups["tail"].Value;
                    if (exponent < tail.Length)
                    {
                        builder.Append(tail, 0, exponent);
                        builder.Append(formatInfo.NumberDecimalSeparator);
                        builder.Append(tail, exponent, tail.Length - exponent);
                    }
                    else
                    {
                        builder.Append(tail);
                        builder.Append('0', exponent - tail.Length);
                    }
                }
                else
                {
                    builder.Append('0');
                    builder.Append(formatInfo.NumberDecimalSeparator);
                    builder.Append('0', (-exponent) - 1);
                    builder.Append(match.Groups["head"].Value);
                    builder.Append(match.Groups["tail"].Value);
                }
                result = builder.ToString();
            }
            return result;
        }

        public static void WriteAllLines(string filename, List<string> lines)
        {

            File.WriteAllText(filename, string.Join(Gerber.LineEnding, lines));


        }

        internal static double ParseDouble(string inp)
        {
            return double.Parse(inp, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        internal static bool TryParseDouble(string inp, out double N)
        {
            inp = inp.Replace("*", "");
            return double.TryParse(inp, NumberStyles.Any, CultureInfo.InvariantCulture, out N);
        }

        private static double LimitPos2PI(double dA)
        {
            while (dA < -Math.PI / 2) dA += Math.PI * 2;
            while (dA >= Math.PI / 2) dA -= Math.PI * 2;
            return dA;
        }

        class boardset
        {
            public BoardLayer layer;
            public string name;
            public BoardSide side;
        };

        class QuadR
        {
            public double CX;
            public double CY;
            public double D1 = 0;
            public double D2 = 0;
            public double Diff;
            public double DRat = 0;
            public double E;
            public double S;
            internal void Calc(double LastX, double LastY, double X, double Y)
            {
                double CX1 = LastX - CX;
                double CX2 = X - CX;
                double CY1 = LastY - CY;
                double CY2 = Y - CY;

                D1 = Math.Sqrt(CX1 * CX1 + CY1 * CY1);
                D2 = Math.Sqrt(CX2 * CX2 + CY2 * CY2);
                if (D2 != 0) DRat = D1 / D2;

                S = Math.Atan2(LastY - CY, LastX - CX);
                E = Math.Atan2(Y - CY, X - CX);

            }

            internal void FixClockwise()
            {
                //       while (S < E) S += Math.PI * 2;
                Diff = S - E;
                while (Diff > Math.PI) Diff -= Math.PI * 2;
                //              Console.WriteLine("clock: {0:N2}", Gerber.RadToDeg(Diff));
            }

            internal void FixCounterClockwise()
            {
                while (S > E) S -= Math.PI * 2;
                while (S < 0)
                {
                    S += Math.PI * 2.0;
                    E += Math.PI * 2.0;
                }
                Diff = E - S;

                // while (Diff < 0) Diff += Math.PI * 2.0;
                //                Console.WriteLine("counterclock: {0:N2}", Gerber.RadToDeg(Diff));

            }
        }
        #region GERBERCOMMANDSTRINGS
        public static string BuildOutlineApertureMacro(string name, List<PointD> Vertices, GerberNumberFormat format)
        {
            string res = "%AM" + name + "*" + Gerber.LineEnding;
            res += String.Format("4,1,{0}," + Gerber.LineEnding, (Vertices.Count - 2));
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                res += String.Format("{0},{1}," + Gerber.LineEnding, Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].X)).Replace(',', '.'), Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].Y)).Replace(',', '.'));
            }

            res += "0*" + Gerber.LineEnding + "%" + Gerber.LineEnding;
            return res;
        }

        public static string Flash(PointD t, GerberNumberFormat GNF)
        {
            return String.Format("X{0}Y{1}D03*", GNF.Format(GNF._ScaleMMToFile(t.X)), GNF.Format(GNF._ScaleMMToFile(t.Y)));
        }

        public static string LineTo(PointD t, GerberNumberFormat GNF)
        {
            return String.Format("X{0}Y{1}D01*", GNF.Format(GNF._ScaleMMToFile(t.X)), GNF.Format(GNF._ScaleMMToFile(t.Y)));
        }

        public static string MoveTo(PointD t, GerberNumberFormat GNF)
        {
            return String.Format("X{0}Y{1}D02*", GNF.Format(GNF._ScaleMMToFile(t.X)), GNF.Format(GNF._ScaleMMToFile(t.Y)));
        }
        public static string WriteMacroEnd()
        {
            return "" + Gerber.LineEnding + "%" + Gerber.LineEnding;

        }

        public static string WriteMacroPartVertices(List<PointD> Vertices, GerberNumberFormat format)
        {
            string res = "";
            res += String.Format("4,1,{0}," + Gerber.LineEnding, (Vertices.Count - 2));
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                res += String.Format("{0},{1}," + Gerber.LineEnding, Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].X)).Replace(',', '.'), Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].Y)).Replace(',', '.'));
            }
            res += "0*";
            return res;
        }

        public static string WriteMacroStart(string name)
        {
            return "%AM" + name + "*" + Gerber.LineEnding;
        }
        #endregion
    }
}
