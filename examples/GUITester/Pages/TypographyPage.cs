//BSD, 2017, WinterDev
/*
Copyright (c) 2013, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/



using System;
using System.Collections.Generic;
using System.IO;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

//
using Typography.OpenFont;
using Typography.TextLayout;
using Typography.Rendering;

namespace MatterHackers.Agg
{
    public class TypographyTestWidget : GuiWidget
    {
        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private NOpenFontTypeFace openTypeTypeFace;

        public TypographyTestWidget()
        {
            AnchorAll();

            string fontToLoad = "LiberationSans-Regular2.ttf";
            //string fontToLoad = "ARDESTINE.ttf";             
            openTypeTypeFace = NOpenFontTypeFace.LoadTTF(fontToLoad);
        }

        public override void OnDraw(Graphics2D graphics2D)
        {
            double textY = 200;
            int pointSize = 12;

            base.OnDraw(graphics2D);


            graphics2D.DrawString(alphabet, 20, textY, pointSize, color: RGBA_Bytes.Green);
            graphics2D.DrawString(alphabet.ToLower(), 310, textY, pointSize, color: RGBA_Bytes.Green);

            var openTypeStyliedTypeFace = new StyledTypeFace(openTypeTypeFace, pointSize);
            var openTypePrinter = new TypeFacePrinter(alphabet, openTypeStyliedTypeFace, new Vector2(20, textY + 20));
            openTypePrinter.Render(graphics2D, RGBA_Bytes.Red);
            openTypePrinter.Text = alphabet.ToLower();
            openTypePrinter.Origin = new Vector2(310, textY + 20);
            openTypePrinter.Render(graphics2D, RGBA_Bytes.Red);

            textY = 260;
            openTypePrinter.Text = alphabet;
            graphics2D.DrawString(alphabet, 20, textY, pointSize, color: new RGBA_Bytes(RGBA_Bytes.Green, 128));
            graphics2D.DrawString(alphabet.ToLower(), 310, textY, pointSize, color: new RGBA_Bytes(RGBA_Bytes.Green, 128));

            openTypePrinter.Origin = new Vector2(20, textY);
            openTypePrinter.Render(graphics2D, new RGBA_Bytes(RGBA_Bytes.Red, 128));
            openTypePrinter.Text = alphabet.ToLower();
            openTypePrinter.Origin = new Vector2(310, textY);
            openTypePrinter.Render(graphics2D, new RGBA_Bytes(RGBA_Bytes.Red, 128));
        }
    }


    //===============================================================================
    /// <summary>
    /// Typography's NOpenFontTypeface
    /// </summary>
    public class NOpenFontTypeFace : ITypeFace
    {

        Typeface typeface;
        public int Ascent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public RectangleInt BoundingBox
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Cap_height
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Descent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string fontFamily
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Underline_position
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Underline_thickness
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int UnitsPerEm
        {
            get
            {
                return typeface.UnitsPerEm;
            }
        }

        public int X_height
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public int GetAdvanceForCharacter(char character)
        {
            var vertexSource = GetGlyphForCharacter(character);
            return typeface.GetAdvanceWidth(character);
        }
        public int GetAdvanceForCharacter(char character, char nextCharacterToKernWith)
        {
            return GetAdvanceForCharacter(character);
        }
        public static NOpenFontTypeFace LoadTTF(String filename)
        {
            NOpenFontTypeFace fontUnderConstruction = new NOpenFontTypeFace();
            var reader = new OpenFontReader();
            using (var fs = File.OpenRead(filename))
            {
                fontUnderConstruction.typeface = reader.Read(fs);
                fontUnderConstruction._currentGlyphPathBuilder = new GlyphPathBuilder(fontUnderConstruction.typeface);

            }

            return fontUnderConstruction;
        }


        Dictionary<char, PathStorage> glyphCache = new Dictionary<char, PathStorage>();
        //
        //from https://github.com/LayoutFarm/Typography/blob/master/Demo/Windows/GdiPlusSample.WinForms/GdiPlus/DevGdiTextPrinter.cs
        //
        GlyphTranslatorToPathStorage glyphToPathStorage = new GlyphTranslatorToPathStorage();
        GlyphPathBuilder _currentGlyphPathBuilder;//for current typeface
        public IVertexSource GetGlyphForCharacter(char character)
        {

            if (!glyphCache.ContainsKey(character))
            {
                //1. get glyph index
                ushort glyphIndex = typeface.LookupIndex(character);
                Glyph glyph = typeface.GetGlyphByIndex(glyphIndex);
                //2. set some props of the builder,
                //if you want to use TrueType Instruction Interpreter, use must specific font size,
                //if you don't want to use 

                //3. build it, if you want to use TrueType Instruction interpreter
                //we must specific size for a glyph but
                //if we not use the interpreter=> we not need to specific size 
                _currentGlyphPathBuilder.SetHintTechnique(HintTechnique.None);
                _currentGlyphPathBuilder.BuildFromGlyphIndex(glyphIndex, -1);//-1 
                //3. read 
                _currentGlyphPathBuilder.ReadShapes(glyphToPathStorage);
                glyphCache.Add(character, glyphToPathStorage.ResultGraphicsPath);

            }

            return glyphCache[character];
        }


        /// <summary>
        /// read result as agg-sharp's PathStorage
        /// </summary>
        class GlyphTranslatorToPathStorage : IGlyphTranslator
        {
            PathStorage ps;
            float lastMoveX;
            float lastMoveY;
            float lastX;
            float lastY;
            public GlyphTranslatorToPathStorage()
            {
            }
            public void BeginRead(int countourCount)
            {
                Reset();
                ps = new PathStorage();
            }
            public void EndRead()
            {
                ps.EndPolygon();
            }
            public void MoveTo(float x0, float y0)
            {
                lastX = lastMoveX = x0;
                lastY = lastMoveY = y0;
                ps.MoveTo(x0, y0);
            }
            public void CloseContour()
            {
                ps.ClosePolygon();
            }
            public void Curve3(float x1, float y1, float x2, float y2)
            {
                ps.Curve3(x1, y1, lastX = x2, lastY = y2);
            }
            public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                ps.Curve4(x1, y1, x2, y2, lastX = x3, lastY = y3);
            }

            public void LineTo(float x1, float y1)
            {
                ps.LineTo(lastX = x1, lastY = y1);
            }

            public void Reset()
            {
                ps = null;
                lastMoveX = lastMoveY = lastX = lastY = 0;
            }
            public PathStorage ResultGraphicsPath { get { return this.ps; } }

        }
    }
}


namespace Typography.Rendering
{

    //-----------------------------------
    //sample GlyphPathBuilder :
    //for your flexiblity of glyph path builder.
    //-----------------------------------
    public class GlyphPathBuilder : GlyphPathBuilderBase
    {
        //from https://github.com/LayoutFarm/Typography/blob/master/Demo/Windows/GdiPlusSample.WinForms/GlyphPathBuilder.cs
        public GlyphPathBuilder(Typography.OpenFont.Typeface typeface) : base(typeface) { }
    }

    public abstract class GlyphPathBuilderBase
    {
        readonly Typeface _typeface;
        TrueTypeInterpreter _trueTypeInterpreter;
        protected GlyphPointF[] _outputGlyphPoints;
        protected ushort[] _outputContours;
        float _recentPixelScale;
        bool _useInterpreter;


        public GlyphPathBuilderBase(Typeface typeface)
        {

            _typeface = typeface;
            this.UseTrueTypeInstructions = false;//default?
            _trueTypeInterpreter = new TrueTypeInterpreter();
            _trueTypeInterpreter.SetTypeFace(typeface);
            _recentPixelScale = 1;
        }
        public Typeface Typeface { get { return _typeface; } }


        /// <summary>
        /// use Maxim's Agg Vertical Hinting
        /// </summary>
        public bool UseVerticalHinting { get; set; }
        /// <summary>
        /// process glyph with true type instructions
        /// </summary>
        public bool UseTrueTypeInstructions
        {
            get { return _useInterpreter; }
            set
            {
                _useInterpreter = value;
            }
        }
        public void BuildFromGlyphIndex(ushort glyphIndex, float sizeInPoints)
        {
            //
            Glyph glyph = _typeface.GetGlyphByIndex(glyphIndex);
            //
            this._outputGlyphPoints = glyph.GlyphPoints;
            this._outputContours = glyph.EndPoints;
            //

            if (sizeInPoints > 0)
            {
                _recentPixelScale = this._typeface.CalculateFromPointToPixelScale(sizeInPoints); //***
                FitCurrentGlyph(glyphIndex, glyph, sizeInPoints);
            }
            else
            {
                _recentPixelScale = 1;
            }
        }
        protected virtual void FitCurrentGlyph(ushort glyphIndex, Glyph glyph, float sizeInPoints)
        {
            //2. process glyph points
            if (UseTrueTypeInstructions &&
                this._typeface.HasPrepProgramBuffer &&
                glyph.HasGlyphInstructions)
            {
                _trueTypeInterpreter.UseVerticalHinting = this.UseVerticalHinting;
                //output as points,
                this._outputGlyphPoints = _trueTypeInterpreter.HintGlyph(glyphIndex, sizeInPoints);
                //all points are scaled from _trueTypeInterpreter, 
                //so not need further scale.=> set _recentPixelScale=1
                _recentPixelScale = 1;
            }
        }
        public virtual void ReadShapes(IGlyphTranslator tx)
        {
            //read output from glyph points
            tx.Read(this._outputGlyphPoints, this._outputContours, _recentPixelScale);
        }
        protected float RecentPixelScale { get { return _recentPixelScale; } }
    }
    public static class GlyphPathBuilderExtensions
    {
        public static void Build(this GlyphPathBuilder builder, char c, float sizeInPoints)
        {
            builder.BuildFromGlyphIndex((ushort)builder.Typeface.LookupIndex(c), sizeInPoints);
        }
        public static void SetHintTechnique(this GlyphPathBuilder builder, HintTechnique hintTech)
        {

            builder.UseTrueTypeInstructions = false;//reset
            builder.UseVerticalHinting = false;//reset
            switch (hintTech)
            {
                case HintTechnique.TrueTypeInstruction:
                    builder.UseTrueTypeInstructions = true;
                    break;
                case HintTechnique.TrueTypeInstruction_VerticalOnly:
                    builder.UseTrueTypeInstructions = true;
                    builder.UseVerticalHinting = true;
                    break;
                case HintTechnique.CustomAutoFit:
                    //custom agg autofit 
                    builder.UseVerticalHinting = true;
                    break;
            }
        }
    }


    public enum HintTechnique : byte
    {
        /// <summary>
        /// no hinting
        /// </summary>
        None,
        /// <summary>
        /// truetype instruction
        /// </summary>
        TrueTypeInstruction,
        /// <summary>
        /// truetype instruction vertical only
        /// </summary>
        TrueTypeInstruction_VerticalOnly,
        /// <summary>
        /// custom hint
        /// </summary>
        CustomAutoFit
    }
}
