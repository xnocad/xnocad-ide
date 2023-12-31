﻿using IDE.Core.Interfaces;
using IDE.Core.Presentation.Utilities;
using IDE.Core.Storage;
using IDE.Core.Types.Attributes;
using IDE.Core.Types.Media;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IDE.Core.Designers
{
    public class TextBoardCanvasItem : SingleLayerBoardCanvasItem, ITextCanvasItem, IPlainDesignerItem
    {
        public TextBoardCanvasItem() : base()
        {
            //should default to current layer

            Text = "Text";
            Width = 5.08;

            TextDecoration = TextDecorationEnum.None;
            FontSize = 24;
            FontFamily = "Segoe UI";//new FontFamily("Segoe UI");
        }

        string fontFamily;

        [Display(Order = 2)]
        [Editor(EditorNames.FontFamilyEditor, EditorNames.FontFamilyEditor)]
        [MarksDirty]
        public string FontFamily
        {
            get { return fontFamily; }
            set
            {
                fontFamily = value;
                OnPropertyChanged(nameof(FontFamily));
            }
        }

        double fontSize;

        [Display(Order = 3)]
        [MarksDirty]
        public double FontSize
        {
            get { return fontSize; }
            set
            {
                fontSize = value;
                OnPropertyChanged(nameof(FontSize));
            }
        }

        bool bold;

        [Display(Order = 4)]
        [MarksDirty]
        public bool Bold
        {
            get { return bold; }
            set
            {
                bold = value;
                OnPropertyChanged(nameof(Bold));
            }
        }

        bool italic;

        [Display(Order = 5)]
        [MarksDirty]
        public bool Italic
        {
            get { return italic; }
            set
            {
                italic = value;
                OnPropertyChanged(nameof(Italic));
            }
        }

        TextDecorationEnum textDecoration;

        [Display(Order = 6)]
        [MarksDirty]
        public TextDecorationEnum TextDecoration
        {
            get
            {
                return textDecoration;
            }
            set
            {
                textDecoration = value;
                OnPropertyChanged(nameof(TextDecoration));
            }
        }

        XTextAlignment textAlign;

        [Display(Order = 7)]
        [MarksDirty]
        public XTextAlignment TextAlign
        {
            get { return textAlign; }
            set
            {
                textAlign = value;
                OnPropertyChanged(nameof(TextAlign));
            }
        }

        bool wordWrap;
        [Display(Order = 8)]
        public bool WordWrap
        {
            get { return wordWrap; }

            set
            {
                wordWrap = value;
                if (wordWrap == false)
                    Width = double.NaN;
                OnPropertyChanged(nameof(WordWrap));
            }
        }

        string text;

        [Editor(EditorNames.MultilineTextEditor, EditorNames.MultilineTextEditor)]
        [Display(Order = 9)]
        [MarksDirty]
        public string Text
        {
            get { return text; }
            set
            {
                text = value;

                OnPropertyChanged(nameof(Text));
            }
        }

        double width;

        [Editor(EditorNames.SizeMilimetersUnitsEditor, EditorNames.SizeMilimetersUnitsEditor)]
        [Display(Order = 10)]
        [MarksDirty]
        public double Width
        {
            get { return width; }
            set
            {
                width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        double x;

        /// <summary>
        /// Location X of rectangle in current coordinate system
        /// </summary>
        [Editor(EditorNames.PositionXUnitsEditor, EditorNames.PositionXUnitsEditor)]
        [Display(Order = 11)]
        [MarksDirty]
        public double X
        {
            get { return x; }
            set
            {
                x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        double y;

        /// <summary>
        /// Location X of rectangle in current coordinate system
        /// </summary>
        [Editor(EditorNames.PositionYUnitsEditor, EditorNames.PositionYUnitsEditor)]
        [Display(Order = 12)]
        [MarksDirty]
        public double Y
        {
            get { return y; }
            set
            {
                y = value;
                OnPropertyChanged(nameof(Y));
            }
        }



        double rot;
        [Display(Order = 13)]
        [MarksDirty]
        public double Rot
        {
            get { return rot; }
            set
            {
                rot = value;
                OnPropertyChanged(nameof(Rot));
            }
        }


        public override void Translate(double dx, double dy)
        {
            X += dx;
            Y += dy;
        }

        public override XRect GetBoundingRectangle()
        {
            //var bounds = GeometryHelper.GetGeometryBounds(this);
            var wrapWidth = -1.0d;
            if (wordWrap)
            {
                wrapWidth = width;
            }
            var rect = TextHelper.MeasureString(Text, fontFamily, fontSize, wrapWidth: wrapWidth);

            if (rect.IsEmpty)
                return rect;
            rect.Offset(x, y);

            //if we word wrap, then we have the same width as item.width (when not NaN)
            if (wordWrap && !double.IsNaN(width))
            {
                rect.Width = width;
            }

            rect.Width = Math.Round(rect.Width, 4);
            rect.Height = Math.Round(rect.Height, 4);

            return rect;
        }

        protected override void LoadFromPrimitiveInternal(IPrimitive primitive)
        {
            var t = (TextBoard)primitive;

            Text = t.Value;
            Width = t.Width;
            WordWrap = t.WordWrap;
            TextAlign = t.TextAlign;
            X = t.x;
            Y = t.y;
            Rot = t.rot;
            Bold = t.Bold;
            Italic = t.Italic;
            FontFamily = t.FontFamily;
            FontSize = t.FontSize;
            LayerId = t.layerId;
            IsLocked = t.IsLocked;
        }

        public override IPrimitive SaveToPrimitive()
        {
            var t = new TextBoard();

            t.Value = Text;
            t.Width = Width.GetValueOrDefault();
            t.WordWrap = WordWrap;
            t.TextAlign = TextAlign;
            t.x = X;
            t.y = Y;
            t.rot = Rot;
            t.Bold = Bold;
            t.Italic = Italic;
            t.FontFamily = FontFamily;
            t.FontSize = FontSize;
            t.layerId = (Layer?.LayerId).GetValueOrDefault();
            t.IsLocked = IsLocked;

            return t;
        }

        //public override Adorner CreateAdorner(UIElement element)
        //{
        //    return new TextAdorner(element);
        //}

        public override void MirrorX()
        {
            ScaleX *= -1;
        }

        public override void MirrorY()
        {
            ScaleY *= -1;
        }

        public override void TransformBy(XMatrix matrix)
        {
            var p = new XPoint(x, y);
            p = matrix.Transform(p);
            X = p.X;
            Y = p.Y;

            //var rotAngle = GetRotationAngleFromMatrix(matrix);
            //Rot = RotateSafe(Rot, rotAngle);

            //Rot = GetWorldRotation(rot, matrix);
        }

        protected override XTransform GetLocalRotationTransform()
        {
            return new XRotateTransform(Rot);
        }

        protected override XTransform GetLocalTranslationTransform()
        {
            return new XTranslateTransform(X, Y);
        }

        protected override XTransform GetLocalMirrorTransform()
        {
            if (ParentObject == null && IsMirrored())
            {
                return new XScaleTransform
                {
                    ScaleX = -1,
                };
            }

            return base.GetLocalMirrorTransform();
        }

        protected override XTransform GetGlobalMirror(IContainerSelectableItem parentObject)
        {
            if (IsMirrored())
            {
                return new XScaleTransform
                {
                    ScaleX = -1,
                };
            }


            return XTransform.Identity;
        }

        public override void Rotate(double angle = 90)
        {
            Rot = RotateSafe(Rot, angle);
        }

        public override string ToString()
        {
            //trim the text to show for a single line
            var maxLen = 10;
            var t = Text.Replace(Environment.NewLine, " ");
            if (t.Length > maxLen)
                t = Text.Substring(0, maxLen);

            return $"Text ({t})";
        }
    }
}
