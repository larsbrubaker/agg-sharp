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
using MatterHackers.Agg.VertexSource;

namespace MatterHackers.Agg.Font
{
	public interface ITypeFace
	{
		int Ascent { get; }
		RectangleInt BoundingBox { get; }
		int Cap_height { get; }
		int Descent { get; }
		String fontFamily { get; }
		int Underline_position { get; }
		int Underline_thickness { get; }
		int UnitsPerEm { get; }
		int X_height { get; }

		int GetAdvanceForCharacter(char character);

		int GetAdvanceForCharacter(char character, char nextCharacterToKernWith);

		IVertexSource GetGlyphForCharacter(char character);
	}

	public class SvgTypeFace : ITypeFace
	{
		internal RectangleInt boundingBox;
		internal int descent;
		internal String font_stretch;
		internal int font_weight;
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
		public String fontFamily { get; internal set; }
		public int Underline_position { get { return underline_position; } }

		public int Underline_thickness { get { return underline_thickness; } }

		public int UnitsPerEm { get; set; }

		public int X_height { get { return x_height; } }

		public int GetAdvanceForCharacter(char character, char nextCharacterToKernWith)
		{
			// TODO: check for kerning and adjust
			Glyph glyph;
			if (glyphs.TryGetValue(character, out glyph))
			{
				return glyph.horiz_adv_x;
			}

			return 0;
		}

		public int GetAdvanceForCharacter(char character)
		{
			Glyph glyph;
			if (glyphs.TryGetValue(character, out glyph))
			{
				return glyph.horiz_adv_x;
			}

			return 0;
		}

		public IVertexSource GetGlyphForCharacter(char character)
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