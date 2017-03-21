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
using NRasterizer;

namespace MatterHackers.Agg
{
    public class NRasterizerWidget : GuiWidget
    {
        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private OpenTypeTypeFace openTypeTypeFace;

        public NRasterizerWidget()
        {
            AnchorAll();

            string fontToLoad = "LiberationSans-Regular.ttf";
            //fontToLoad = "ARDESTINE.ttf";
            //fontToLoad = "OpenSans-Regular.ttf";
            openTypeTypeFace = OpenTypeTypeFace.LoadTTF(fontToLoad);
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

    public class OpenTypeTypeFace : ITypeFace
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

        public static OpenTypeTypeFace LoadTTF(String filename)
        {
            OpenTypeTypeFace fontUnderConstruction = new OpenTypeTypeFace();

            var reader = new OpenTypeReader();
            using (var fs = File.OpenRead(filename))
            {
                fontUnderConstruction.typeface = reader.Read(fs);
            }

            return fontUnderConstruction;
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

        Dictionary<char, PathStorage> glyphCache = new Dictionary<char, PathStorage>();
        public IVertexSource GetGlyphForCharacter(char character)
        {
            if (!glyphCache.ContainsKey(character))
            {
                PathStorage newGlyphPath = new PathStorage();

                var glyph = typeface.Lookup(character);

                ushort[] contours = glyph.EndPoints;
                short[] xs = glyph.X;
                short[] ys = glyph.Y;
                bool[] onCurves = glyph.On;

                int npoints = xs.Length;
                int startContour = 0;
                int cpoint_index = 0;

                int lastMoveX = 0;
                int lastMoveY = 0;

                int controlPointCount = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    int nextContour = contours[startContour] + 1;
                    bool isFirstPoint = true;
                    Point<int> secondControlPoint = new Point<int>();
                    Point<int> thirdControlPoint = new Point<int>();
                    bool justFromCurveMode = false;

                    for (; cpoint_index < nextContour; ++cpoint_index)
                    {
                        short vpoint_x = xs[cpoint_index];
                        short vpoint_y = ys[cpoint_index];
                        if (onCurves[cpoint_index])
                        {
                            //on curve
                            if (justFromCurveMode)
                            {
                                switch (controlPointCount)
                                {
                                    case 1:
                                        {
                                            newGlyphPath.Curve3(
                                                secondControlPoint.x,
                                                secondControlPoint.y,
                                                vpoint_x,
                                                vpoint_y);
                                        }
                                        break;

                                    case 2:
                                        {
                                            newGlyphPath.Curve4(
                                                    secondControlPoint.x, secondControlPoint.y,
                                                    thirdControlPoint.x, thirdControlPoint.y,
                                                    vpoint_x, vpoint_y);
                                        }
                                        break;

                                    default:
                                        {
                                            throw new NotSupportedException();
                                        }
                                }
                                controlPointCount = 0;
                                justFromCurveMode = false;
                            }
                            else
                            {
                                if (isFirstPoint)
                                {
                                    isFirstPoint = false;
                                    lastMoveX = vpoint_x;
                                    lastMoveY = vpoint_y;
                                    newGlyphPath.MoveTo(lastMoveX, lastMoveY);
                                }
                                else
                                {
                                    newGlyphPath.LineTo(vpoint_x, vpoint_y);
                                }
                            }
                        }
                        else
                        {
                            switch (controlPointCount)
                            {
                                case 0:
                                    {
                                        secondControlPoint = new Point<int>(vpoint_x, vpoint_y);
                                    }
                                    break;

                                case 1:
                                    {
                                        //we already have prev second control point
                                        //so auto calculate line to
                                        //between 2 point
                                        Point<int> mid = new Point<int>((secondControlPoint.X + vpoint_x) / 2, (secondControlPoint.Y + vpoint_y) / 2);
                                        //----------
                                        //generate curve3
                                        newGlyphPath.Curve3(
                                            secondControlPoint.x, secondControlPoint.y,
                                            mid.x, mid.y);
                                        //------------------------
                                        controlPointCount--;
                                        //------------------------
                                        //printf("[%d] bzc2nd,  x: %d,y:%d \n", mm, vpoint.x, vpoint.y);
                                        secondControlPoint = new Point<int>(vpoint_x, vpoint_y);
                                    }
                                    break;

                                default:
                                    {
                                        throw new NotSupportedException("Too many control points");
                                    }
                            }

                            controlPointCount++;
                            justFromCurveMode = true;
                        }
                    }
                    //--------
                    //close figure
                    //if in curve mode
                    if (justFromCurveMode)
                    {
                        switch (controlPointCount)
                        {
                            case 0: break;
                            case 1:
                                {
                                    newGlyphPath.Curve3(
                                        secondControlPoint.x, secondControlPoint.y,
                                        lastMoveX, lastMoveY);
                                }
                                break;

                            case 2:
                                {
                                    newGlyphPath.Curve4(
                                        secondControlPoint.x, secondControlPoint.y,
                                        thirdControlPoint.x, thirdControlPoint.y,
                                        lastMoveX, lastMoveY);
                                }
                                break;

                            default:
                                { throw new NotSupportedException("Too many control points"); }
                        }
                        justFromCurveMode = false;
                        controlPointCount = 0;
                    }
                    newGlyphPath.ClosePolygon();
                    //--------
                    startContour++;
                }
                newGlyphPath.EndPolygon();

                glyphCache.Add(character, newGlyphPath);
            }

            return glyphCache[character];
        }
    }
}