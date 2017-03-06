//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007-2011
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
//
// Class TypeFace.cs
//
//----------------------------------------------------------------------------
using System;
using System.Globalization;
using System.IO;

namespace MatterHackers.Agg.Font
{
	public static class SVGTypeFaceExtensions
	{
		public static TypeFace LoadFrom(string content)
		{
			TypeFace fontUnderConstruction = new TypeFace();
			fontUnderConstruction.ReadSVG(content);

			return fontUnderConstruction;
		}

		public static TypeFace LoadSVG(String filename)
		{
			TypeFace fontUnderConstruction = new TypeFace();

			string svgContent = "";
			using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (StreamReader reader = new StreamReader(fileStream))
				{
					svgContent = reader.ReadToEnd();
				}
			}
			fontUnderConstruction.ReadSVG(svgContent);

			return fontUnderConstruction;
		}

		public static void ReadSVG(this TypeFace typeFace, String svgContent)
		{
			int readValue = 0;
			int startIndex = 0;
			String fontElementString = GetSubString(svgContent, "<font", ">", ref startIndex);
			string fontId = GetStringValue(fontElementString, "id");
			GetIntValue(fontElementString, "horiz-adv-x", out typeFace.horiz_adv_x);

			String fontFaceString = GetSubString(svgContent, "<font-face", "/>", ref startIndex);
			typeFace.fontFamily = GetStringValue(fontFaceString, "font-family");
			GetIntValue(fontFaceString, "font-weight", out typeFace.font_weight);
			typeFace.font_stretch = GetStringValue(fontFaceString, "font-stretch");
			GetIntValue(fontFaceString, "units-per-em", out readValue); typeFace.UnitsPerEm = readValue;
			Panos_1 panose_1 = new Panos_1(GetStringValue(fontFaceString, "panose-1"));
			int ascent;
			GetIntValue(fontFaceString, "ascent", out ascent);
			typeFace.Ascent = ascent;
			GetIntValue(fontFaceString, "descent", out typeFace.descent);
			GetIntValue(fontFaceString, "x-height", out typeFace.x_height);
			int cap_height;
			GetIntValue(fontFaceString, "cap-height", out cap_height);
			typeFace.Cap_height = cap_height;

			String bboxString = GetStringValue(fontFaceString, "bbox");
			String[] valuesString = bboxString.Split(' ');
			int.TryParse(valuesString[0], out typeFace.boundingBox.Left);
			int.TryParse(valuesString[1], out typeFace.boundingBox.Bottom);
			int.TryParse(valuesString[2], out typeFace.boundingBox.Right);
			int.TryParse(valuesString[3], out typeFace.boundingBox.Top);

			GetIntValue(fontFaceString, "underline-thickness", out typeFace.underline_thickness);
			GetIntValue(fontFaceString, "underline-position", out typeFace.underline_position);
			typeFace.unicode_range = GetStringValue(fontFaceString, "unicode-range");

			String missingGlyphString = GetSubString(svgContent, "<missing-glyph", "/>", ref startIndex);
			typeFace.missingGlyph = typeFace.CreateGlyphFromSVGGlyphData(missingGlyphString);

			String nextGlyphString = GetSubString(svgContent, "<glyph", "/>", ref startIndex);
			while (nextGlyphString != null)
			{
				// get the data and put it in the glyph dictionary
				TypeFace.Glyph newGlyph = typeFace.CreateGlyphFromSVGGlyphData(nextGlyphString);
				if (newGlyph.unicode > 0)
				{
					typeFace.glyphs.Add(newGlyph.unicode, newGlyph);
				}

				nextGlyphString = GetSubString(svgContent, "<glyph", "/>", ref startIndex);
			}
		}

		private static TypeFace.Glyph CreateGlyphFromSVGGlyphData(this TypeFace typeFace, String SVGGlyphData)
		{
			TypeFace.Glyph newGlyph = new TypeFace.Glyph();
			if (!GetIntValue(SVGGlyphData, "horiz-adv-x", out newGlyph.horiz_adv_x))
			{
				newGlyph.horiz_adv_x = typeFace.horiz_adv_x;
			}

			newGlyph.glyphName = GetStringValue(SVGGlyphData, "glyph-name");
			String unicodeString = GetStringValue(SVGGlyphData, "unicode");

			if (unicodeString != null)
			{
				if (unicodeString.Length == 1)
				{
					newGlyph.unicode = (int)unicodeString[0];
				}
				else
				{
					if (unicodeString.Split(';').Length > 1 && unicodeString.Split(';')[1].Length > 0)
					{
						throw new NotImplementedException("We do not currently support glyphs longer than one character.  You need to write the search so that it will find them if you want to support this");
					}

					if (int.TryParse(unicodeString, NumberStyles.Number, null, out newGlyph.unicode) == false)
					{
						// see if it is a unicode
						String hexNumber = GetSubString(unicodeString, "&#x", ";");
						int.TryParse(hexNumber, NumberStyles.HexNumber, null, out newGlyph.unicode);
					}
				}
			}

			String dString = GetStringValue(SVGGlyphData, "d");

			if (dString == null || dString.Length == 0)
			{
				return newGlyph;
			}

			newGlyph.glyphData.ParseSvgDString(dString);

			return newGlyph;
		}

		private static bool GetIntValue(String source, String name, out int outValue, ref int startIndex)
		{
			String element = GetSubString(source, name + "=\"", "\"", ref startIndex);
			if (int.TryParse(element, NumberStyles.Number, null, out outValue))
			{
				return true;
			}

			return false;
		}

		private static bool GetIntValue(String source, String name, out int outValue)
		{
			int startIndex = 0;
			return GetIntValue(source, name, out outValue, ref startIndex);
		}

		private static String GetStringValue(String source, String name)
		{
			String element = GetSubString(source, name + "=\"", "\"");
			return element;
		}

		private static String GetSubString(String source, String start, String end)
		{
			int startIndex = 0;
			return GetSubString(source, start, end, ref startIndex);
		}

		private static String GetSubString(String source, String start, String end, ref int startIndex)
		{
			int startPos = source.IndexOf(start, startIndex);
			if (startPos >= 0)
			{
				int endPos = source.IndexOf(end, startPos + start.Length);

				int length = endPos - (startPos + start.Length);
				startIndex = endPos + end.Length; // advance our start position to the last position used
				return source.Substring(startPos + start.Length, length);
			}

			return null;
		}
	}

	public class Panos_1
	{
		private Arm_Style armStyle;

		private Contrast contrast;

		private Family family;

		private Letterform letterform;

		private Midline midline;

		private Proportion proportion;

		private Serif_Style serifStyle;

		private Stroke_Variation strokeVariation;

		private Weight weight;

		private XHeight xHeight;

		public Panos_1(String SVGPanos1String)
		{
			int tempInt;
			String[] valuesString = SVGPanos1String.Split(' ');
			if (int.TryParse(valuesString[0], out tempInt))
				family = (Family)tempInt;
			if (int.TryParse(valuesString[1], out tempInt))
				serifStyle = (Serif_Style)tempInt;
			if (int.TryParse(valuesString[2], out tempInt))
				weight = (Weight)tempInt;
			if (int.TryParse(valuesString[3], out tempInt))
				proportion = (Proportion)tempInt;
			if (int.TryParse(valuesString[4], out tempInt))
				contrast = (Contrast)tempInt;
			if (int.TryParse(valuesString[5], out tempInt))
				strokeVariation = (Stroke_Variation)tempInt;
			if (int.TryParse(valuesString[6], out tempInt))
				armStyle = (Arm_Style)tempInt;
			if (int.TryParse(valuesString[7], out tempInt))
				letterform = (Letterform)tempInt;
			if (int.TryParse(valuesString[8], out tempInt))
				midline = (Midline)tempInt;
			if (int.TryParse(valuesString[0], out tempInt))
				xHeight = (XHeight)tempInt;
		}

		private enum Arm_Style { Any, No_Fit, Straight_Arms_Horizontal, Straight_Arms_Wedge, Straight_Arms_Vertical, Straight_Arms_Single_Serif, Straight_Arms_Double_Serif, Non_Straight_Arms_Horizontal, Non_Straight_Arms_Wedge, Non_Straight_Arms_Vertical_90, Non_Straight_Arms_Single_Serif, Non_Straight_Arms_Double_Serif };

		private enum Contrast { Any, No_Fit, None, Very_Low, Low, Medium_Low, Medium, Medium_High, High, Very_High };

		// these are defined in the order in which they are present in the panos-1 attribute.
		private enum Family { Any, No_Fit, Latin_Text_and_Display, Latin_Script, Latin_Decorative, Latin_Pictorial };

		private enum Letterform { Any, No_Fit, Normal_Contact, Normal_Weighted, Normal_Boxed, Normal_Flattened, Normal_Rounded, Normal_Off_Center, Normal_Square, Oblique_Contact, Oblique_Weighted, Oblique_Boxed, Oblique_Flattened, Oblique_Rounded, Oblique_Off_Center, Oblique_Square };

		private enum Midline { Any, No_Fit, Standard_Trimmed, Standard_Pointed, Standard_Serifed, High_Trimmed, High_Pointed, High_Serifed, Constant_Trimmed, Constant_Pointed, Constant_Serifed, Low_Trimmed, Low_Pointed, Low_Serifed };

		private enum Proportion { Any, No_Fit, Old_Style, Modern, Even_Width, Expanded, Condensed, Very_Expanded, Very_Condensed, Monospaced };

		private enum Serif_Style { Any, No_Fit, Cove, Obtuse_Cove, Square_Cove, Obtuse_Square_Cove, Square, Thin, Bone, Exaggerated, Triangle, Normal_Sans, Obtuse_Sans, Perp_Sans, Flared, Rounded };

		private enum Stroke_Variation { Any, No_Fit, No_Variation, Gradual_Diagonal, Gradual_Transitional, Gradual_Vertical, Gradual_Horizontal, Rapid_Vertical, Rapid_Horizontal, Instant_Horizontal, Instant_Vertical };

		private enum Weight { Any, No_Fit, Very_Light_100, Light_200, Thin_300, Book_400_same_as_CSS1_normal, Medium_500, Demi_600, Bold_700_same_as_CSS1_bold, Heavy_800, Black_900, Extra_Black_Nord_900_force_mapping_to_CSS1_100_900_scale };

		private enum XHeight { Any, No_Fit, Constant_Small, Constant_Standard, Constant_Large, Ducking_Small, Ducking_Standard, Ducking_Large };
	}
}