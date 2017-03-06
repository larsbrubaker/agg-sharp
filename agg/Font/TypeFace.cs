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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg.Font
{
	public class TypeFace
	{
		internal RectangleInt boundingBox;

		internal int descent;

		internal String font_stretch;

		internal int font_weight;

		internal String fontFamily;

		internal Dictionary<int, Glyph> glyphs = new Dictionary<int, Glyph>();

		internal int horiz_adv_x;

		internal Glyph missingGlyph;

		internal int underline_position;

		internal int underline_thickness;

		internal String unicode_range;

		internal int x_height;

		// a glyph is indexed by the string it represents, usually one character, but sometimes multiple
		private Dictionary<Char, Dictionary<Char, int>> HKerns = new Dictionary<char, Dictionary<char, int>>();

		public int Ascent { get; internal set; }

		public RectangleInt BoundingBox { get { return boundingBox; } }

		public int Cap_height { get; internal set; }

		public int Descent { get { return descent; } }

		public int Underline_position { get { return underline_position; } }

		public int Underline_thickness { get { return underline_thickness; } }

		public int UnitsPerEm { get; set; }

		public int X_height { get { return x_height; } }

		public void ShowDebugInfo(Graphics2D graphics2D)
		{
			StyledTypeFace typeFaceNameStyle = new StyledTypeFace(this, 30);
			TypeFacePrinter fontNamePrinter = new TypeFacePrinter(this.fontFamily + " - 30 point", typeFaceNameStyle);

			RectangleDouble bounds = typeFaceNameStyle.BoundingBoxInPixels;
			double origX = 10 - bounds.Left;
			double x = origX;
			double y = 10 - typeFaceNameStyle.DescentInPixels;
			int width = 50;
			RGBA_Bytes boundingBoxColor = new RGBA_Bytes(0, 0, 0);
			RGBA_Bytes originColor = new RGBA_Bytes(0, 0, 0);
			RGBA_Bytes ascentColor = new RGBA_Bytes(255, 0, 0);
			RGBA_Bytes descentColor = new RGBA_Bytes(255, 0, 0);
			RGBA_Bytes xHeightColor = new RGBA_Bytes(12, 25, 200);
			RGBA_Bytes capHeightColor = new RGBA_Bytes(12, 25, 200);
			RGBA_Bytes underlineColor = new RGBA_Bytes(0, 150, 55);

			// the origin
			graphics2D.Line(x, y, x + width, y, originColor);

			graphics2D.Rectangle(x + bounds.Left, y + bounds.Bottom, x + bounds.Right, y + bounds.Top, boundingBoxColor);

			x += typeFaceNameStyle.BoundingBoxInPixels.Width * 1.5;

			width = width * 3;

			double temp = typeFaceNameStyle.AscentInPixels;
			graphics2D.Line(x, y + temp, x + width, y + temp, ascentColor);

			temp = typeFaceNameStyle.DescentInPixels;
			graphics2D.Line(x, y + temp, x + width, y + temp, descentColor);

			temp = typeFaceNameStyle.XHeightInPixels;
			graphics2D.Line(x, y + temp, x + width, y + temp, xHeightColor);

			temp = typeFaceNameStyle.CapHeightInPixels;
			graphics2D.Line(x, y + temp, x + width, y + temp, capHeightColor);

			temp = typeFaceNameStyle.UnderlinePositionInPixels;
			graphics2D.Line(x, y + temp, x + width, y + temp, underlineColor);

			Affine textTransform;
			textTransform = Affine.NewIdentity();
			textTransform *= Affine.NewTranslation(10, origX);

			VertexSourceApplyTransform transformedText = new VertexSourceApplyTransform(textTransform);
			fontNamePrinter.Render(graphics2D, RGBA_Bytes.Black, transformedText);

			graphics2D.Render(transformedText, RGBA_Bytes.Black);

			// render the legend
			StyledTypeFace legendFont = new StyledTypeFace(this, 12);
			Vector2 textPos = new Vector2(x + width / 2, y + typeFaceNameStyle.EmSizeInPixels * 1.5);
			graphics2D.Render(new TypeFacePrinter("Descent"), textPos, descentColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("Underline"), textPos, underlineColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("X Height"), textPos, xHeightColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("CapHeight"), textPos, capHeightColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("Ascent"), textPos, ascentColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("Origin"), textPos, originColor); textPos.y += legendFont.EmSizeInPixels;
			graphics2D.Render(new TypeFacePrinter("Bounding Box"), textPos, boundingBoxColor);
		}

		internal int GetAdvanceForCharacter(char character, char nextCharacterToKernWith)
		{
			// TODO: check for kerning and adjust
			Glyph glyph;
			if (glyphs.TryGetValue(character, out glyph))
			{
				return glyph.horiz_adv_x;
			}

			return 0;
		}

		internal int GetAdvanceForCharacter(char character)
		{
			Glyph glyph;
			if (glyphs.TryGetValue(character, out glyph))
			{
				return glyph.horiz_adv_x;
			}

			return 0;
		}

		internal IVertexSource GetGlyphForCharacter(char character)
		{
			// TODO: check for multi character glyphs (we don't currently support them in the reader).
			Glyph glyph;
			if (glyphs.TryGetValue(character, out glyph))
			{
				PathStorage writeableGlyph = new PathStorage();
				writeableGlyph.ShareVertexData(glyph.glyphData);
				return writeableGlyph;
			}

			return null;
		}

		public class Glyph
		{
			public PathStorage glyphData = new PathStorage();
			public string glyphName;
			public int horiz_adv_x;
			public int unicode;
		}
	}
}