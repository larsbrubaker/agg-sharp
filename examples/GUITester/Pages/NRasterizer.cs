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

using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using System;
using System.IO;
using NRasterizer;

namespace MatterHackers.Agg
{
	public static class OpenTypeTypeFaceExtensions
	{
		public static TypeFace LoadTTF(String filename)
		{
			TypeFace fontUnderConstruction = new TypeFace();

			var reader = new OpenTypeReader();
			using (var fs = File.OpenRead(filename))
			{
				var typeface = reader.Read(fs);
				for(int  i=0; i<typeface.Glyphs.Count; i++)
				{
					var glyph = typeface.Glyphs[i];
					CreateGlyphFromGlyph(glyph);
				}
			}

			return fontUnderConstruction;
		}

		private static void CreateGlyphFromGlyph(Glyph glyph)
		{
			int x = 0;//glyphLayout.TopLeft.X;
			int y = 0;// glyphLayout.TopLeft.Y;

			#if false
			var rasterizer = new ToPixelRasterizer(x, y, scalingFactor, FontToPixelDivisor, _rasterizer);

			ushort[] contours = glyph.EndPoints;
			short[] xs = glyph.X;
			short[] ys = glyph.Y;
			bool[] onCurves = glyph.On;

			int npoints = xs.Length;
			int startContour = 0;
			int cpoint_index = 0;

			rasterizer.BeginRead(contours.Length);

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
										rasterizer.Curve3(
											secondControlPoint.x,
											secondControlPoint.y,
											vpoint_x,
											vpoint_y);
									}
									break;
								case 2:
									{
										rasterizer.Curve4(
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
								rasterizer.MoveTo(lastMoveX, lastMoveY);
							}
							else
							{
								rasterizer.LineTo(vpoint_x, vpoint_y);
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
									Point<int> mid = GetMidPoint(secondControlPoint, vpoint_x, vpoint_y);
									//----------
									//generate curve3
									rasterizer.Curve3(
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
								rasterizer.Curve3(
									secondControlPoint.x, secondControlPoint.y,
									lastMoveX, lastMoveY);
							}
							break;
						case 2:
							{
								rasterizer.Curve4(
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
				rasterizer.CloseFigure();
				//--------                   
				startContour++;
			}
			rasterizer.EndRead();
			#endif
		}
	}

	public class NRasterizerWidget : GuiWidget
	{
		string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		TypeFace openTypeTypeFace;

		public NRasterizerWidget()
		{
			AnchorAll();

			string fontToLoad = "LiberationSans-Regular.ttf";
			fontToLoad = "ARDESTINE.ttf";
			fontToLoad = "OpenSans-Regular.ttf";
			TypeFace openTypeTypeFace = OpenTypeTypeFaceExtensions.LoadTTF(fontToLoad);
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			var openTypeStyliedTypeFace = new StyledTypeFace(openTypeTypeFace, 12);
			//var openTypePrinter = new TypeFacePrinter(alphabet, StyledTypeFace typeFaceStyle, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)

			double textY = 200;

			base.OnDraw(graphics2D);

			graphics2D.DrawString(alphabet, 20, textY);
			graphics2D.DrawString(alphabet.ToLower(), 310, textY);
		}
	}
}