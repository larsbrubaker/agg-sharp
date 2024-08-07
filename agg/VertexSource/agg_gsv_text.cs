// <auto-generated>
// Hack to disable analyzers and their warnings - too many issues to address
// </auto-generated>
using MatterHackers.VectorMath;

//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
//
// Class gsv_text
//
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace MatterHackers.Agg.VertexSource
{
	[Obsolete("All of these should use the new font stuff.  You probably want a StringPrinter or a TextWidget in this spot.")]
	public sealed class gsv_text : IVertexSource
	{
		private enum status
		{
			initial,
			next_char,
			start_glyph,
			glyph
		};

		private double m_StartX;
		private double m_CurrentX;
		private double m_CurrentY;
		private double m_WidthRatioOfHeight;
		private double m_FontSize;
		private double m_SpaceBetweenCharacters;
		private double m_SpaceBetweenLines;
		private string m_Text;
		private int m_CurrentCharacterIndex;
		private byte[] m_font;
		private status m_status;
		private int m_StartOfIndicesIndex;
		private int m_StartOfGlyphsIndex;
		private int m_BeginGlyphIndex;
		private int m_EndGlyphIndex;
		private double m_WidthScaleRatio;
		private double m_HeightScaleRatio;

		public double FontSize
		{
			get
			{
				return m_FontSize;
			}
			set
			{
				m_FontSize = value;
				double base_height = translateIndex(4);
				m_HeightScaleRatio = m_FontSize / base_height;
				m_WidthScaleRatio = m_HeightScaleRatio * m_WidthRatioOfHeight;
			}
		}

		public double AscenderHeight
		{
			get
			{
				return m_FontSize * .15;
			}
		}

		public double DescenderHeight
		{
			get
			{
				return m_FontSize * .2;
			}
		}

		public gsv_text()
		{
			m_font = CGSVDefaultFont.gsv_default_font;
			m_CurrentX = 0.0;
			m_CurrentY = 0.0;
			m_StartX = 0.0;
			m_WidthRatioOfHeight = 1;
			FontSize = 0.0;
			m_SpaceBetweenCharacters = 0.0;
			m_status = status.initial;

			m_SpaceBetweenLines = 0.0;
		}

        /*
		public void font(void* font)
		{
			m_font = font;
			if(m_font == 0) m_font = &m_loaded_font[0];
		}
		 */

        public ulong GetLongHashCode(ulong hash = 14695981039346656037)
        {
            foreach (var vertex in this.Vertices())
            {
                hash = vertex.GetLongHashCode(hash);
            }

            return hash;
        }

        public void load_font(string file)
		{
			throw new System.NotImplementedException();
			/*
			m_loaded_font.resize(0);
			FILE* fd = fopen(file, "rb");
			if(fd)
			{
				int len;

				fseek(fd, 0l, SEEK_END);
				len = ftell(fd);
				fseek(fd, 0l, SEEK_SET);
				if(len > 0)
				{
					m_loaded_font.resize(len);
					fread(&m_loaded_font[0], 1, len, fd);
					m_font = &m_loaded_font[0];
				}
				fclose(fd);
			}
			 */
		}

		// This will set the desired height.  NOTE: The font may not render at the size that you say.
		// It depends on the way the font was originally created.  A 24 Point font may not actually be 24 points high
		public void SetFontSize(double fontSize)
		{
			SetFontSizeAndWidthRatio(fontSize, 1.0);
		}

		public void SetFontSizeAndWidthRatio(double fontSize, double widthRatioOfHeight)
		{
			if (fontSize == 0 || widthRatioOfHeight == 0)
			{
				throw new System.Exception("You can't have a font with 0 width or height.  Nothing will render.");
			}

			m_WidthRatioOfHeight = widthRatioOfHeight;
			FontSize = fontSize;

			m_SpaceBetweenLines = FontSize * 1.5;
		}

		public void SetSpaceBetweenCharacters(double spaceBetweenCharacters)
		{
			m_SpaceBetweenCharacters = spaceBetweenCharacters;
		}

		public void line_space(double spaceBetweenLines)
		{
			m_SpaceBetweenLines = spaceBetweenLines;
		}

		public void start_point(double x, double y)
		{
			m_CurrentX = m_StartX = x;
			m_CurrentY = y;
		}

		public string Text
		{
			get
			{
				return m_Text;
			}
			set
			{
				m_Text = value;
			}
		}

		public void text(string text)
		{
			m_Text = text;
		}

		private ushort translateIndex(int indicesIndex)
		{
			ushort v;
			v = m_font[indicesIndex + 0];
			v |= (ushort)(m_font[indicesIndex + 1] << 8);
			return v;
		}

		public IEnumerable<VertexData> Vertices()
		{
			throw new NotImplementedException();
		}

		public void Rewind(int nothing)
		{
			m_status = status.initial;
			if (m_font == null) return;

			m_StartOfIndicesIndex = translateIndex(0);

			m_StartOfGlyphsIndex = m_StartOfIndicesIndex + 257 * 2; // one for x one for y
			m_CurrentCharacterIndex = 0;
		}

		private void GetSize(char characterToMeasure, out double width, out double height)
		{
			width = 0;
			height = 0;
			if (m_font == null)
			{
				return;
			}

			int maskedChracter = (int)(characterToMeasure & 0xFF);
			if (maskedChracter == '\r' || maskedChracter == '\n')
			{
				height -= (FontSize + m_SpaceBetweenLines);
				return;
			}

			int maskedChracterGlyphIndex = maskedChracter * 2; // we have an x and y in the array so it's * 2.
			int BeginGlyphIndex = m_StartOfGlyphsIndex + translateIndex(m_StartOfIndicesIndex + maskedChracterGlyphIndex);
			int EndGlyphIndex = m_StartOfGlyphsIndex + translateIndex(m_StartOfIndicesIndex + maskedChracterGlyphIndex + 2);

			do
			{
				if (BeginGlyphIndex >= EndGlyphIndex)
				{
					return; // the character has no glyph
				}

				unchecked
				{
					int DeltaX = (sbyte)m_font[BeginGlyphIndex++];
					sbyte yc = (sbyte)m_font[BeginGlyphIndex++];

					yc <<= 1;
					yc >>= 1;
					int DeltaY = (int)(yc);
					width += (double)(DeltaX) * m_WidthScaleRatio;
					height += (double)(DeltaY) * m_HeightScaleRatio;
				}
			} while (true);
		}

		public int GetCharacterIndexToStartBefore(Vector2 position)
		{
			int clostestIndex = -1;
			double clostestDist = double.MaxValue;
			Vector2 offset;
			offset.X = 0;
			offset.Y = 0;
			int characterToMeasureStartIndexInclusive = 0;
			int characterToMeasureEndIndexInclusive = m_Text.Length - 1;
			if (m_Text.Length > 0)
			{
				characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, m_Text.Length - 1));
				characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, m_Text.Length - 1));
				for (int i = characterToMeasureStartIndexInclusive; i <= characterToMeasureEndIndexInclusive; i++)
				{
					Vector2 delta = offset - position;
					double distToChar = delta.Length;
					if (distToChar < clostestDist)
					{
						clostestDist = distToChar;
						clostestIndex = i;
					}

					char singleChar = m_Text[i];
					if (singleChar == '\r' || singleChar == '\n')
					{
						offset.X = 0;
						offset.Y -= FontSize + m_SpaceBetweenLines;
					}
					else
					{
						double sigleWidth;
						double sigleHeight;
						GetSize(singleChar, out sigleWidth, out sigleHeight);
						offset.X += sigleWidth + m_SpaceBetweenCharacters;
						offset.Y += sigleHeight;
					}
				}

				Vector2 lastDelta = offset - position;
				double lastDistToChar = lastDelta.Length;
				if (lastDistToChar < clostestDist)
				{
					clostestDist = lastDistToChar;
					// we need to start after the lats character, or before the character after the last.
					clostestIndex = characterToMeasureEndIndexInclusive + 1;
				}
			}

			return clostestIndex;
		}

		public void GetSize(out Vector2 pixelSize)
		{
			GetSize(0, m_Text.Length - 1, out pixelSize);
		}

		public void GetSize(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 pixelSize)
		{
			double currentX = 0;
			pixelSize.X = 0;
			pixelSize.Y = 0;
			if (m_Text.Length > 0)
			{
				characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, m_Text.Length - 1));
				characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, m_Text.Length - 1));
				for (int i = characterToMeasureStartIndexInclusive; i <= characterToMeasureEndIndexInclusive; i++)
				{
					char singleChar = m_Text[i];
					if (singleChar == '\r' || singleChar == '\n')
					{
						currentX = 0;
						pixelSize.Y -= FontSize + m_SpaceBetweenLines;
					}
					else
					{
						double sigleWidth;
						double sigleHeight;
						GetSize(singleChar, out sigleWidth, out sigleHeight);
						currentX += sigleWidth + m_SpaceBetweenCharacters;
						pixelSize.X = Math.Max(currentX, pixelSize.X);
						pixelSize.Y += sigleHeight;
					}
				}
			}
		}

		public FlagsAndCommand Vertex(out double x, out double y)
		{
			x = 0;
			y = 0;
			bool quit = false;

			while (!quit)
			{
				switch (m_status)
				{
					case status.initial:
						if (m_font == null)
						{
							quit = true;
							break;
						}
						m_status = status.next_char;
						goto case status.next_char;

					case status.next_char:
						if (m_CurrentCharacterIndex == m_Text.Length)
						{
							quit = true;
							break;
						}
						int maskedChracter = (int)((m_Text[m_CurrentCharacterIndex++]) & 0xFF);
						if (maskedChracter == '\r' || maskedChracter == '\n')
						{
							m_CurrentX = m_StartX;
							m_CurrentY -= FontSize + m_SpaceBetweenLines;
							break;
						}
						int maskedChracterGlyphIndex = maskedChracter * 2; // we have an x and y in the array so it's * 2.
						m_BeginGlyphIndex = m_StartOfGlyphsIndex + translateIndex(m_StartOfIndicesIndex + maskedChracterGlyphIndex);
						m_EndGlyphIndex = m_StartOfGlyphsIndex + translateIndex(m_StartOfIndicesIndex + maskedChracterGlyphIndex + 2);
						m_status = status.start_glyph;
						goto case status.start_glyph;

					case status.start_glyph:
						x = m_CurrentX;
						y = m_CurrentY;
						m_status = status.glyph;
						return FlagsAndCommand.MoveTo;

					case status.glyph:
						if (m_BeginGlyphIndex >= m_EndGlyphIndex)
						{
							m_status = status.next_char;
							m_CurrentX += m_SpaceBetweenCharacters;
							break;
						}

						sbyte IsAMoveTo_Flag;
						unchecked
						{
							int DeltaX = (sbyte)m_font[m_BeginGlyphIndex++];
							sbyte yc = (sbyte)m_font[m_BeginGlyphIndex++];

							IsAMoveTo_Flag = (sbyte)(yc & 0x80);
							yc <<= 1;
							yc >>= 1;
							int DeltaY = (int)(yc);
							m_CurrentX += (double)(DeltaX) * m_WidthScaleRatio;
							m_CurrentY += (double)(DeltaY) * m_HeightScaleRatio;
						}
						x = m_CurrentX;
						y = m_CurrentY;
						if (IsAMoveTo_Flag != 0)
						{
							return FlagsAndCommand.MoveTo;
						}

						return FlagsAndCommand.LineTo;

					default:
						throw new System.Exception("Unknown Status");
				}
			}

			return FlagsAndCommand.Stop;
		}
	};

	internal static class CGSVDefaultFont
	{
		static public byte[] gsv_default_font =
		{
			0x40,0x00,0x6c,0x0f,0x15,0x00,0x0e,0x00,0xf9,0xff,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x0d,0x0a,0x0d,0x0a,0x46,0x6f,0x6e,0x74,0x20,0x28,
			0x63,0x29,0x20,0x4d,0x69,0x63,0x72,0x6f,0x50,0x72,
			0x6f,0x66,0x20,0x32,0x37,0x20,0x53,0x65,0x70,0x74,
			0x65,0x6d,0x62,0x2e,0x31,0x39,0x38,0x39,0x00,0x0d,
			0x0a,0x0d,0x0a,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			0x02,0x00,0x12,0x00,0x34,0x00,0x46,0x00,0x94,0x00,
			0xd0,0x00,0x2e,0x01,0x3e,0x01,0x64,0x01,0x8a,0x01,
			0x98,0x01,0xa2,0x01,0xb4,0x01,0xba,0x01,0xc6,0x01,
			0xcc,0x01,0xf0,0x01,0xfa,0x01,0x18,0x02,0x38,0x02,
			0x44,0x02,0x68,0x02,0x98,0x02,0xa2,0x02,0xde,0x02,
			0x0e,0x03,0x24,0x03,0x40,0x03,0x48,0x03,0x52,0x03,
			0x5a,0x03,0x82,0x03,0xec,0x03,0xfa,0x03,0x26,0x04,
			0x4c,0x04,0x6a,0x04,0x7c,0x04,0x8a,0x04,0xb6,0x04,
			0xc4,0x04,0xca,0x04,0xe0,0x04,0xee,0x04,0xf8,0x04,
			0x0a,0x05,0x18,0x05,0x44,0x05,0x5e,0x05,0x8e,0x05,
			0xac,0x05,0xd6,0x05,0xe0,0x05,0xf6,0x05,0x00,0x06,
			0x12,0x06,0x1c,0x06,0x28,0x06,0x36,0x06,0x48,0x06,
			0x4e,0x06,0x60,0x06,0x6e,0x06,0x74,0x06,0x84,0x06,
			0xa6,0x06,0xc8,0x06,0xe6,0x06,0x08,0x07,0x2c,0x07,
			0x3c,0x07,0x68,0x07,0x7c,0x07,0x8c,0x07,0xa2,0x07,
			0xb0,0x07,0xb6,0x07,0xd8,0x07,0xec,0x07,0x10,0x08,
			0x32,0x08,0x54,0x08,0x64,0x08,0x88,0x08,0x98,0x08,
			0xac,0x08,0xb6,0x08,0xc8,0x08,0xd2,0x08,0xe4,0x08,
			0xf2,0x08,0x3e,0x09,0x48,0x09,0x94,0x09,0xc2,0x09,
			0xc4,0x09,0xd0,0x09,0xe2,0x09,0x04,0x0a,0x0e,0x0a,
			0x26,0x0a,0x34,0x0a,0x4a,0x0a,0x66,0x0a,0x70,0x0a,
			0x7e,0x0a,0x8e,0x0a,0x9a,0x0a,0xa6,0x0a,0xb4,0x0a,
			0xd8,0x0a,0xe2,0x0a,0xf6,0x0a,0x18,0x0b,0x22,0x0b,
			0x32,0x0b,0x56,0x0b,0x60,0x0b,0x6e,0x0b,0x7c,0x0b,
			0x8a,0x0b,0x9c,0x0b,0x9e,0x0b,0xb2,0x0b,0xc2,0x0b,
			0xd8,0x0b,0xf4,0x0b,0x08,0x0c,0x30,0x0c,0x56,0x0c,
			0x72,0x0c,0x90,0x0c,0xb2,0x0c,0xce,0x0c,0xe2,0x0c,
			0xfe,0x0c,0x10,0x0d,0x26,0x0d,0x36,0x0d,0x42,0x0d,
			0x4e,0x0d,0x5c,0x0d,0x78,0x0d,0x8c,0x0d,0x8e,0x0d,
			0x90,0x0d,0x92,0x0d,0x94,0x0d,0x96,0x0d,0x98,0x0d,
			0x9a,0x0d,0x9c,0x0d,0x9e,0x0d,0xa0,0x0d,0xa2,0x0d,
			0xa4,0x0d,0xa6,0x0d,0xa8,0x0d,0xaa,0x0d,0xac,0x0d,
			0xae,0x0d,0xb0,0x0d,0xb2,0x0d,0xb4,0x0d,0xb6,0x0d,
			0xb8,0x0d,0xba,0x0d,0xbc,0x0d,0xbe,0x0d,0xc0,0x0d,
			0xc2,0x0d,0xc4,0x0d,0xc6,0x0d,0xc8,0x0d,0xca,0x0d,
			0xcc,0x0d,0xce,0x0d,0xd0,0x0d,0xd2,0x0d,0xd4,0x0d,
			0xd6,0x0d,0xd8,0x0d,0xda,0x0d,0xdc,0x0d,0xde,0x0d,
			0xe0,0x0d,0xe2,0x0d,0xe4,0x0d,0xe6,0x0d,0xe8,0x0d,
			0xea,0x0d,0xec,0x0d,0x0c,0x0e,0x26,0x0e,0x48,0x0e,
			0x64,0x0e,0x88,0x0e,0x92,0x0e,0xa6,0x0e,0xb4,0x0e,
			0xd0,0x0e,0xee,0x0e,0x02,0x0f,0x16,0x0f,0x26,0x0f,
			0x3c,0x0f,0x58,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,
			0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,
			0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,
			0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x6c,0x0f,0x10,0x80,
			0x05,0x95,0x00,0x72,0x00,0xfb,0xff,0x7f,0x01,0x7f,
			0x01,0x01,0xff,0x01,0x05,0xfe,0x05,0x95,0xff,0x7f,
			0x00,0x7a,0x01,0x86,0xff,0x7a,0x01,0x87,0x01,0x7f,
			0xfe,0x7a,0x0a,0x87,0xff,0x7f,0x00,0x7a,0x01,0x86,
			0xff,0x7a,0x01,0x87,0x01,0x7f,0xfe,0x7a,0x05,0xf2,
			0x0b,0x95,0xf9,0x64,0x0d,0x9c,0xf9,0x64,0xfa,0x91,
			0x0e,0x00,0xf1,0xfa,0x0e,0x00,0x04,0xfc,0x08,0x99,
			0x00,0x63,0x04,0x9d,0x00,0x63,0x04,0x96,0xff,0x7f,
			0x01,0x7f,0x01,0x01,0x00,0x01,0xfe,0x02,0xfd,0x01,
			0xfc,0x00,0xfd,0x7f,0xfe,0x7e,0x00,0x7e,0x01,0x7e,
			0x01,0x7f,0x02,0x7f,0x06,0x7e,0x02,0x7f,0x02,0x7e,
			0xf2,0x89,0x02,0x7e,0x02,0x7f,0x06,0x7e,0x02,0x7f,
			0x01,0x7f,0x01,0x7e,0x00,0x7c,0xfe,0x7e,0xfd,0x7f,
			0xfc,0x00,0xfd,0x01,0xfe,0x02,0x00,0x01,0x01,0x01,
			0x01,0x7f,0xff,0x7f,0x10,0xfd,0x15,0x95,0xee,0x6b,
			0x05,0x95,0x02,0x7e,0x00,0x7e,0xff,0x7e,0xfe,0x7f,
			0xfe,0x00,0xfe,0x02,0x00,0x02,0x01,0x02,0x02,0x01,
			0x02,0x00,0x02,0x7f,0x03,0x7f,0x03,0x00,0x03,0x01,
			0x02,0x01,0xfc,0xf2,0xfe,0x7f,0xff,0x7e,0x00,0x7e,
			0x02,0x7e,0x02,0x00,0x02,0x01,0x01,0x02,0x00,0x02,
			0xfe,0x02,0xfe,0x00,0x07,0xf9,0x15,0x8d,0xff,0x7f,
			0x01,0x7f,0x01,0x01,0x00,0x01,0xff,0x01,0xff,0x00,
			0xff,0x7f,0xff,0x7e,0xfe,0x7b,0xfe,0x7d,0xfe,0x7e,
			0xfe,0x7f,0xfd,0x00,0xfd,0x01,0xff,0x02,0x00,0x03,
			0x01,0x02,0x06,0x04,0x02,0x02,0x01,0x02,0x00,0x02,
			0xff,0x02,0xfe,0x01,0xfe,0x7f,0xff,0x7e,0x00,0x7e,
			0x01,0x7d,0x02,0x7d,0x05,0x79,0x02,0x7e,0x03,0x7f,
			0x01,0x00,0x01,0x01,0x00,0x01,0xf1,0xfe,0xfe,0x01,
			0xff,0x02,0x00,0x03,0x01,0x02,0x02,0x02,0x00,0x86,
			0x01,0x7e,0x08,0x75,0x02,0x7e,0x02,0x7f,0x05,0x80,
			0x05,0x93,0xff,0x01,0x01,0x01,0x01,0x7f,0x00,0x7e,
			0xff,0x7e,0xff,0x7f,0x06,0xf1,0x0b,0x99,0xfe,0x7e,
			0xfe,0x7d,0xfe,0x7c,0xff,0x7b,0x00,0x7c,0x01,0x7b,
			0x02,0x7c,0x02,0x7d,0x02,0x7e,0xfe,0x9e,0xfe,0x7c,
			0xff,0x7d,0xff,0x7b,0x00,0x7c,0x01,0x7b,0x01,0x7d,
			0x02,0x7c,0x05,0x85,0x03,0x99,0x02,0x7e,0x02,0x7d,
			0x02,0x7c,0x01,0x7b,0x00,0x7c,0xff,0x7b,0xfe,0x7c,
			0xfe,0x7d,0xfe,0x7e,0x02,0x9e,0x02,0x7c,0x01,0x7d,
			0x01,0x7b,0x00,0x7c,0xff,0x7b,0xff,0x7d,0xfe,0x7c,
			0x09,0x85,0x08,0x95,0x00,0x74,0xfb,0x89,0x0a,0x7a,
			0x00,0x86,0xf6,0x7a,0x0d,0xf4,0x0d,0x92,0x00,0x6e,
			0xf7,0x89,0x12,0x00,0x04,0xf7,0x06,0x81,0xff,0x7f,
			0xff,0x01,0x01,0x01,0x01,0x7f,0x00,0x7e,0xff,0x7e,
			0xff,0x7f,0x06,0x84,0x04,0x89,0x12,0x00,0x04,0xf7,
			0x05,0x82,0xff,0x7f,0x01,0x7f,0x01,0x01,0xff,0x01,
			0x05,0xfe,0x00,0xfd,0x0e,0x18,0x00,0xeb,0x09,0x95,
			0xfd,0x7f,0xfe,0x7d,0xff,0x7b,0x00,0x7d,0x01,0x7b,
			0x02,0x7d,0x03,0x7f,0x02,0x00,0x03,0x01,0x02,0x03,
			0x01,0x05,0x00,0x03,0xff,0x05,0xfe,0x03,0xfd,0x01,
			0xfe,0x00,0x0b,0xeb,0x06,0x91,0x02,0x01,0x03,0x03,
			0x00,0x6b,0x09,0x80,0x04,0x90,0x00,0x01,0x01,0x02,
			0x01,0x01,0x02,0x01,0x04,0x00,0x02,0x7f,0x01,0x7f,
			0x01,0x7e,0x00,0x7e,0xff,0x7e,0xfe,0x7d,0xf6,0x76,
			0x0e,0x00,0x03,0x80,0x05,0x95,0x0b,0x00,0xfa,0x78,
			0x03,0x00,0x02,0x7f,0x01,0x7f,0x01,0x7d,0x00,0x7e,
			0xff,0x7d,0xfe,0x7e,0xfd,0x7f,0xfd,0x00,0xfd,0x01,
			0xff,0x01,0xff,0x02,0x11,0xfc,0x0d,0x95,0xf6,0x72,
			0x0f,0x00,0xfb,0x8e,0x00,0x6b,0x07,0x80,0x0f,0x95,
			0xf6,0x00,0xff,0x77,0x01,0x01,0x03,0x01,0x03,0x00,
			0x03,0x7f,0x02,0x7e,0x01,0x7d,0x00,0x7e,0xff,0x7d,
			0xfe,0x7e,0xfd,0x7f,0xfd,0x00,0xfd,0x01,0xff,0x01,
			0xff,0x02,0x11,0xfc,0x10,0x92,0xff,0x02,0xfd,0x01,
			0xfe,0x00,0xfd,0x7f,0xfe,0x7d,0xff,0x7b,0x00,0x7b,
			0x01,0x7c,0x02,0x7e,0x03,0x7f,0x01,0x00,0x03,0x01,
			0x02,0x02,0x01,0x03,0x00,0x01,0xff,0x03,0xfe,0x02,
			0xfd,0x01,0xff,0x00,0xfd,0x7f,0xfe,0x7e,0xff,0x7d,
			0x10,0xf9,0x11,0x95,0xf6,0x6b,0xfc,0x95,0x0e,0x00,
			0x03,0xeb,0x08,0x95,0xfd,0x7f,0xff,0x7e,0x00,0x7e,
			0x01,0x7e,0x02,0x7f,0x04,0x7f,0x03,0x7f,0x02,0x7e,
			0x01,0x7e,0x00,0x7d,0xff,0x7e,0xff,0x7f,0xfd,0x7f,
			0xfc,0x00,0xfd,0x01,0xff,0x01,0xff,0x02,0x00,0x03,
			0x01,0x02,0x02,0x02,0x03,0x01,0x04,0x01,0x02,0x01,
			0x01,0x02,0x00,0x02,0xff,0x02,0xfd,0x01,0xfc,0x00,
			0x0c,0xeb,0x10,0x8e,0xff,0x7d,0xfe,0x7e,0xfd,0x7f,
			0xff,0x00,0xfd,0x01,0xfe,0x02,0xff,0x03,0x00,0x01,
			0x01,0x03,0x02,0x02,0x03,0x01,0x01,0x00,0x03,0x7f,
			0x02,0x7e,0x01,0x7c,0x00,0x7b,0xff,0x7b,0xfe,0x7d,
			0xfd,0x7f,0xfe,0x00,0xfd,0x01,0xff,0x02,0x10,0xfd,
			0x05,0x8e,0xff,0x7f,0x01,0x7f,0x01,0x01,0xff,0x01,
			0x00,0xf4,0xff,0x7f,0x01,0x7f,0x01,0x01,0xff,0x01,
			0x05,0xfe,0x05,0x8e,0xff,0x7f,0x01,0x7f,0x01,0x01,
			0xff,0x01,0x01,0xf3,0xff,0x7f,0xff,0x01,0x01,0x01,
			0x01,0x7f,0x00,0x7e,0xff,0x7e,0xff,0x7f,0x06,0x84,
			0x14,0x92,0xf0,0x77,0x10,0x77,0x04,0x80,0x04,0x8c,
			0x12,0x00,0xee,0xfa,0x12,0x00,0x04,0xfa,0x04,0x92,
			0x10,0x77,0xf0,0x77,0x14,0x80,0x03,0x90,0x00,0x01,
			0x01,0x02,0x01,0x01,0x02,0x01,0x04,0x00,0x02,0x7f,
			0x01,0x7f,0x01,0x7e,0x00,0x7e,0xff,0x7e,0xff,0x7f,
			0xfc,0x7e,0x00,0x7d,0x00,0xfb,0xff,0x7f,0x01,0x7f,
			0x01,0x01,0xff,0x01,0x09,0xfe,0x12,0x8d,0xff,0x02,
			0xfe,0x01,0xfd,0x00,0xfe,0x7f,0xff,0x7f,0xff,0x7d,
			0x00,0x7d,0x01,0x7e,0x02,0x7f,0x03,0x00,0x02,0x01,
			0x01,0x02,0xfb,0x88,0xfe,0x7e,0xff,0x7d,0x00,0x7d,
			0x01,0x7e,0x01,0x7f,0x07,0x8b,0xff,0x78,0x00,0x7e,
			0x02,0x7f,0x02,0x00,0x02,0x02,0x01,0x03,0x00,0x02,
			0xff,0x03,0xff,0x02,0xfe,0x02,0xfe,0x01,0xfd,0x01,
			0xfd,0x00,0xfd,0x7f,0xfe,0x7f,0xfe,0x7e,0xff,0x7e,
			0xff,0x7d,0x00,0x7d,0x01,0x7d,0x01,0x7e,0x02,0x7e,
			0x02,0x7f,0x03,0x7f,0x03,0x00,0x03,0x01,0x02,0x01,
			0x01,0x01,0xfe,0x8d,0xff,0x78,0x00,0x7e,0x01,0x7f,
			0x08,0xfb,0x09,0x95,0xf8,0x6b,0x08,0x95,0x08,0x6b,
			0xf3,0x87,0x0a,0x00,0x04,0xf9,0x04,0x95,0x00,0x6b,
			0x00,0x95,0x09,0x00,0x03,0x7f,0x01,0x7f,0x01,0x7e,
			0x00,0x7e,0xff,0x7e,0xff,0x7f,0xfd,0x7f,0xf7,0x80,
			0x09,0x00,0x03,0x7f,0x01,0x7f,0x01,0x7e,0x00,0x7d,
			0xff,0x7e,0xff,0x7f,0xfd,0x7f,0xf7,0x00,0x11,0x80,
			0x12,0x90,0xff,0x02,0xfe,0x02,0xfe,0x01,0xfc,0x00,
			0xfe,0x7f,0xfe,0x7e,0xff,0x7e,0xff,0x7d,0x00,0x7b,
			0x01,0x7d,0x01,0x7e,0x02,0x7e,0x02,0x7f,0x04,0x00,
			0x02,0x01,0x02,0x02,0x01,0x02,0x03,0xfb,0x04,0x95,
			0x00,0x6b,0x00,0x95,0x07,0x00,0x03,0x7f,0x02,0x7e,
			0x01,0x7e,0x01,0x7d,0x00,0x7b,0xff,0x7d,0xff,0x7e,
			0xfe,0x7e,0xfd,0x7f,0xf9,0x00,0x11,0x80,0x04,0x95,
			0x00,0x6b,0x00,0x95,0x0d,0x00,0xf3,0xf6,0x08,0x00,
			0xf8,0xf5,0x0d,0x00,0x02,0x80,0x04,0x95,0x00,0x6b,
			0x00,0x95,0x0d,0x00,0xf3,0xf6,0x08,0x00,0x06,0xf5,
			0x12,0x90,0xff,0x02,0xfe,0x02,0xfe,0x01,0xfc,0x00,
			0xfe,0x7f,0xfe,0x7e,0xff,0x7e,0xff,0x7d,0x00,0x7b,
			0x01,0x7d,0x01,0x7e,0x02,0x7e,0x02,0x7f,0x04,0x00,
			0x02,0x01,0x02,0x02,0x01,0x02,0x00,0x03,0xfb,0x80,
			0x05,0x00,0x03,0xf8,0x04,0x95,0x00,0x6b,0x0e,0x95,
			0x00,0x6b,0xf2,0x8b,0x0e,0x00,0x04,0xf5,0x04,0x95,
			0x00,0x6b,0x04,0x80,0x0c,0x95,0x00,0x70,0xff,0x7d,
			0xff,0x7f,0xfe,0x7f,0xfe,0x00,0xfe,0x01,0xff,0x01,
			0xff,0x03,0x00,0x02,0x0e,0xf9,0x04,0x95,0x00,0x6b,
			0x0e,0x95,0xf2,0x72,0x05,0x85,0x09,0x74,0x03,0x80,
			0x04,0x95,0x00,0x6b,0x00,0x80,0x0c,0x00,0x01,0x80,
			0x04,0x95,0x00,0x6b,0x00,0x95,0x08,0x6b,0x08,0x95,
			0xf8,0x6b,0x08,0x95,0x00,0x6b,0x04,0x80,0x04,0x95,
			0x00,0x6b,0x00,0x95,0x0e,0x6b,0x00,0x95,0x00,0x6b,
			0x04,0x80,0x09,0x95,0xfe,0x7f,0xfe,0x7e,0xff,0x7e,
			0xff,0x7d,0x00,0x7b,0x01,0x7d,0x01,0x7e,0x02,0x7e,
			0x02,0x7f,0x04,0x00,0x02,0x01,0x02,0x02,0x01,0x02,
			0x01,0x03,0x00,0x05,0xff,0x03,0xff,0x02,0xfe,0x02,
			0xfe,0x01,0xfc,0x00,0x0d,0xeb,0x04,0x95,0x00,0x6b,
			0x00,0x95,0x09,0x00,0x03,0x7f,0x01,0x7f,0x01,0x7e,
			0x00,0x7d,0xff,0x7e,0xff,0x7f,0xfd,0x7f,0xf7,0x00,
			0x11,0xf6,0x09,0x95,0xfe,0x7f,0xfe,0x7e,0xff,0x7e,
			0xff,0x7d,0x00,0x7b,0x01,0x7d,0x01,0x7e,0x02,0x7e,
			0x02,0x7f,0x04,0x00,0x02,0x01,0x02,0x02,0x01,0x02,
			0x01,0x03,0x00,0x05,0xff,0x03,0xff,0x02,0xfe,0x02,
			0xfe,0x01,0xfc,0x00,0x03,0xef,0x06,0x7a,0x04,0x82,
			0x04,0x95,0x00,0x6b,0x00,0x95,0x09,0x00,0x03,0x7f,
			0x01,0x7f,0x01,0x7e,0x00,0x7e,0xff,0x7e,0xff,0x7f,
			0xfd,0x7f,0xf7,0x00,0x07,0x80,0x07,0x75,0x03,0x80,
			0x11,0x92,0xfe,0x02,0xfd,0x01,0xfc,0x00,0xfd,0x7f,
			0xfe,0x7e,0x00,0x7e,0x01,0x7e,0x01,0x7f,0x02,0x7f,
			0x06,0x7e,0x02,0x7f,0x01,0x7f,0x01,0x7e,0x00,0x7d,
			0xfe,0x7e,0xfd,0x7f,0xfc,0x00,0xfd,0x01,0xfe,0x02,
			0x11,0xfd,0x08,0x95,0x00,0x6b,0xf9,0x95,0x0e,0x00,
			0x01,0xeb,0x04,0x95,0x00,0x71,0x01,0x7d,0x02,0x7e,
			0x03,0x7f,0x02,0x00,0x03,0x01,0x02,0x02,0x01,0x03,
			0x00,0x0f,0x04,0xeb,0x01,0x95,0x08,0x6b,0x08,0x95,
			0xf8,0x6b,0x09,0x80,0x02,0x95,0x05,0x6b,0x05,0x95,
			0xfb,0x6b,0x05,0x95,0x05,0x6b,0x05,0x95,0xfb,0x6b,
			0x07,0x80,0x03,0x95,0x0e,0x6b,0x00,0x95,0xf2,0x6b,
			0x11,0x80,0x01,0x95,0x08,0x76,0x00,0x75,0x08,0x95,
			0xf8,0x76,0x09,0xf5,0x11,0x95,0xf2,0x6b,0x00,0x95,
			0x0e,0x00,0xf2,0xeb,0x0e,0x00,0x03,0x80,0x03,0x93,
			0x00,0x6c,0x01,0x94,0x00,0x6c,0xff,0x94,0x05,0x00,
			0xfb,0xec,0x05,0x00,0x02,0x81,0x00,0x95,0x0e,0x68,
			0x00,0x83,0x06,0x93,0x00,0x6c,0x01,0x94,0x00,0x6c,
			0xfb,0x94,0x05,0x00,0xfb,0xec,0x05,0x00,0x03,0x81,
			0x03,0x87,0x08,0x05,0x08,0x7b,0xf0,0x80,0x08,0x04,
			0x08,0x7c,0x03,0xf9,0x01,0x80,0x10,0x00,0x01,0x80,
			0x06,0x95,0xff,0x7f,0xff,0x7e,0x00,0x7e,0x01,0x7f,
			0x01,0x01,0xff,0x01,0x05,0xef,0x0f,0x8e,0x00,0x72,
			0x00,0x8b,0xfe,0x02,0xfe,0x01,0xfd,0x00,0xfe,0x7f,
			0xfe,0x7e,0xff,0x7d,0x00,0x7e,0x01,0x7d,0x02,0x7e,
			0x02,0x7f,0x03,0x00,0x02,0x01,0x02,0x02,0x04,0xfd,
			0x04,0x95,0x00,0x6b,0x00,0x8b,0x02,0x02,0x02,0x01,
			0x03,0x00,0x02,0x7f,0x02,0x7e,0x01,0x7d,0x00,0x7e,
			0xff,0x7d,0xfe,0x7e,0xfe,0x7f,0xfd,0x00,0xfe,0x01,
			0xfe,0x02,0x0f,0xfd,0x0f,0x8b,0xfe,0x02,0xfe,0x01,
			0xfd,0x00,0xfe,0x7f,0xfe,0x7e,0xff,0x7d,0x00,0x7e,
			0x01,0x7d,0x02,0x7e,0x02,0x7f,0x03,0x00,0x02,0x01,
			0x02,0x02,0x03,0xfd,0x0f,0x95,0x00,0x6b,0x00,0x8b,
			0xfe,0x02,0xfe,0x01,0xfd,0x00,0xfe,0x7f,0xfe,0x7e,
			0xff,0x7d,0x00,0x7e,0x01,0x7d,0x02,0x7e,0x02,0x7f,
			0x03,0x00,0x02,0x01,0x02,0x02,0x04,0xfd,0x03,0x88,
			0x0c,0x00,0x00,0x02,0xff,0x02,0xff,0x01,0xfe,0x01,
			0xfd,0x00,0xfe,0x7f,0xfe,0x7e,0xff,0x7d,0x00,0x7e,
			0x01,0x7d,0x02,0x7e,0x02,0x7f,0x03,0x00,0x02,0x01,
			0x02,0x02,0x03,0xfd,0x0a,0x95,0xfe,0x00,0xfe,0x7f,
			0xff,0x7d,0x00,0x6f,0xfd,0x8e,0x07,0x00,0x03,0xf2,
			0x0f,0x8e,0x00,0x70,0xff,0x7d,0xff,0x7f,0xfe,0x7f,
			0xfd,0x00,0xfe,0x01,0x09,0x91,0xfe,0x02,0xfe,0x01,
			0xfd,0x00,0xfe,0x7f,0xfe,0x7e,0xff,0x7d,0x00,0x7e,
			0x01,0x7d,0x02,0x7e,0x02,0x7f,0x03,0x00,0x02,0x01,
			0x02,0x02,0x04,0xfd,0x04,0x95,0x00,0x6b,0x00,0x8a,
			0x03,0x03,0x02,0x01,0x03,0x00,0x02,0x7f,0x01,0x7d,
			0x00,0x76,0x04,0x80,0x03,0x95,0x01,0x7f,0x01,0x01,
			0xff,0x01,0xff,0x7f,0x01,0xf9,0x00,0x72,0x04,0x80,
			0x05,0x95,0x01,0x7f,0x01,0x01,0xff,0x01,0xff,0x7f,
			0x01,0xf9,0x00,0x6f,0xff,0x7d,0xfe,0x7f,0xfe,0x00,
			0x09,0x87,0x04,0x95,0x00,0x6b,0x0a,0x8e,0xf6,0x76,
			0x04,0x84,0x07,0x78,0x02,0x80,0x04,0x95,0x00,0x6b,
			0x04,0x80,0x04,0x8e,0x00,0x72,0x00,0x8a,0x03,0x03,
			0x02,0x01,0x03,0x00,0x02,0x7f,0x01,0x7d,0x00,0x76,
			0x00,0x8a,0x03,0x03,0x02,0x01,0x03,0x00,0x02,0x7f,
			0x01,0x7d,0x00,0x76,0x04,0x80,0x04,0x8e,0x00,0x72,
			0x00,0x8a,0x03,0x03,0x02,0x01,0x03,0x00,0x02,0x7f,
			0x01,0x7d,0x00,0x76,0x04,0x80,0x08,0x8e,0xfe,0x7f,
			0xfe,0x7e,0xff,0x7d,0x00,0x7e,0x01,0x7d,0x02,0x7e,
			0x02,0x7f,0x03,0x00,0x02,0x01,0x02,0x02,0x01,0x03,
			0x00,0x02,0xff,0x03,0xfe,0x02,0xfe,0x01,0xfd,0x00,
			0x0b,0xf2,0x04,0x8e,0x00,0x6b,0x00,0x92,0x02,0x02,
			0x02,0x01,0x03,0x00,0x02,0x7f,0x02,0x7e,0x01,0x7d,
			0x00,0x7e,0xff,0x7d,0xfe,0x7e,0xfe,0x7f,0xfd,0x00,
			0xfe,0x01,0xfe,0x02,0x0f,0xfd,0x0f,0x8e,0x00,0x6b,
			0x00,0x92,0xfe,0x02,0xfe,0x01,0xfd,0x00,0xfe,0x7f,
			0xfe,0x7e,0xff,0x7d,0x00,0x7e,0x01,0x7d,0x02,0x7e,
			0x02,0x7f,0x03,0x00,0x02,0x01,0x02,0x02,0x04,0xfd,
			0x04,0x8e,0x00,0x72,0x00,0x88,0x01,0x03,0x02,0x02,
			0x02,0x01,0x03,0x00,0x01,0xf2,0x0e,0x8b,0xff,0x02,
			0xfd,0x01,0xfd,0x00,0xfd,0x7f,0xff,0x7e,0x01,0x7e,
			0x02,0x7f,0x05,0x7f,0x02,0x7f,0x01,0x7e,0x00,0x7f,
			0xff,0x7e,0xfd,0x7f,0xfd,0x00,0xfd,0x01,0xff,0x02,
			0x0e,0xfd,0x05,0x95,0x00,0x6f,0x01,0x7d,0x02,0x7f,
			0x02,0x00,0xf8,0x8e,0x07,0x00,0x03,0xf2,0x04,0x8e,
			0x00,0x76,0x01,0x7d,0x02,0x7f,0x03,0x00,0x02,0x01,
			0x03,0x03,0x00,0x8a,0x00,0x72,0x04,0x80,0x02,0x8e,
			0x06,0x72,0x06,0x8e,0xfa,0x72,0x08,0x80,0x03,0x8e,
			0x04,0x72,0x04,0x8e,0xfc,0x72,0x04,0x8e,0x04,0x72,
			0x04,0x8e,0xfc,0x72,0x07,0x80,0x03,0x8e,0x0b,0x72,
			0x00,0x8e,0xf5,0x72,0x0e,0x80,0x02,0x8e,0x06,0x72,
			0x06,0x8e,0xfa,0x72,0xfe,0x7c,0xfe,0x7e,0xfe,0x7f,
			0xff,0x00,0x0f,0x87,0x0e,0x8e,0xf5,0x72,0x00,0x8e,
			0x0b,0x00,0xf5,0xf2,0x0b,0x00,0x03,0x80,0x09,0x99,
			0xfe,0x7f,0xff,0x7f,0xff,0x7e,0x00,0x7e,0x01,0x7e,
			0x01,0x7f,0x01,0x7e,0x00,0x7e,0xfe,0x7e,0x01,0x8e,
			0xff,0x7e,0x00,0x7e,0x01,0x7e,0x01,0x7f,0x01,0x7e,
			0x00,0x7e,0xff,0x7e,0xfc,0x7e,0x04,0x7e,0x01,0x7e,
			0x00,0x7e,0xff,0x7e,0xff,0x7f,0xff,0x7e,0x00,0x7e,
			0x01,0x7e,0xff,0x8e,0x02,0x7e,0x00,0x7e,0xff,0x7e,
			0xff,0x7f,0xff,0x7e,0x00,0x7e,0x01,0x7e,0x01,0x7f,
			0x02,0x7f,0x05,0x87,0x04,0x95,0x00,0x77,0x00,0xfd,
			0x00,0x77,0x04,0x80,0x05,0x99,0x02,0x7f,0x01,0x7f,
			0x01,0x7e,0x00,0x7e,0xff,0x7e,0xff,0x7f,0xff,0x7e,
			0x00,0x7e,0x02,0x7e,0xff,0x8e,0x01,0x7e,0x00,0x7e,
			0xff,0x7e,0xff,0x7f,0xff,0x7e,0x00,0x7e,0x01,0x7e,
			0x04,0x7e,0xfc,0x7e,0xff,0x7e,0x00,0x7e,0x01,0x7e,
			0x01,0x7f,0x01,0x7e,0x00,0x7e,0xff,0x7e,0x01,0x8e,
			0xfe,0x7e,0x00,0x7e,0x01,0x7e,0x01,0x7f,0x01,0x7e,
			0x00,0x7e,0xff,0x7e,0xff,0x7f,0xfe,0x7f,0x09,0x87,
			0x03,0x86,0x00,0x02,0x01,0x03,0x02,0x01,0x02,0x00,
			0x02,0x7f,0x04,0x7d,0x02,0x7f,0x02,0x00,0x02,0x01,
			0x01,0x02,0xee,0xfe,0x01,0x02,0x02,0x01,0x02,0x00,
			0x02,0x7f,0x04,0x7d,0x02,0x7f,0x02,0x00,0x02,0x01,
			0x01,0x03,0x00,0x02,0x03,0xf4,0x10,0x80,0x03,0x80,
			0x07,0x15,0x08,0x6b,0xfe,0x85,0xf5,0x00,0x10,0xfb,
			0x0d,0x95,0xf6,0x00,0x00,0x6b,0x0a,0x00,0x02,0x02,
			0x00,0x08,0xfe,0x02,0xf6,0x00,0x0e,0xf4,0x03,0x80,
			0x00,0x15,0x0a,0x00,0x02,0x7e,0x00,0x7e,0x00,0x7d,
			0x00,0x7e,0xfe,0x7f,0xf6,0x00,0x0a,0x80,0x02,0x7e,
			0x01,0x7e,0x00,0x7d,0xff,0x7d,0xfe,0x7f,0xf6,0x00,
			0x10,0x80,0x03,0x80,0x00,0x15,0x0c,0x00,0xff,0x7e,
			0x03,0xed,0x03,0xfd,0x00,0x03,0x02,0x00,0x00,0x12,
			0x02,0x03,0x0a,0x00,0x00,0x6b,0x02,0x00,0x00,0x7d,
			0xfe,0x83,0xf4,0x00,0x11,0x80,0x0f,0x80,0xf4,0x00,
			0x00,0x15,0x0c,0x00,0xff,0xf6,0xf5,0x00,0x0f,0xf5,
			0x04,0x95,0x07,0x76,0x00,0x0a,0x07,0x80,0xf9,0x76,
			0x00,0x75,0xf8,0x80,0x07,0x0c,0x09,0xf4,0xf9,0x0c,
			0x09,0xf4,0x03,0x92,0x02,0x03,0x07,0x00,0x03,0x7d,
			0x00,0x7b,0xfc,0x7e,0x04,0x7d,0x00,0x7a,0xfd,0x7e,
			0xf9,0x00,0xfe,0x02,0x06,0x89,0x02,0x00,0x06,0xf5,
			0x03,0x95,0x00,0x6b,0x0c,0x15,0x00,0x6b,0x02,0x80,
			0x03,0x95,0x00,0x6b,0x0c,0x15,0x00,0x6b,0xf8,0x96,
			0x03,0x00,0x07,0xea,0x03,0x80,0x00,0x15,0x0c,0x80,
			0xf7,0x76,0xfd,0x00,0x03,0x80,0x0a,0x75,0x03,0x80,
			0x03,0x80,0x07,0x13,0x02,0x02,0x03,0x00,0x00,0x6b,
			0x02,0x80,0x03,0x80,0x00,0x15,0x09,0x6b,0x09,0x15,
			0x00,0x6b,0x03,0x80,0x03,0x80,0x00,0x15,0x00,0xf6,
			0x0d,0x00,0x00,0x8a,0x00,0x6b,0x03,0x80,0x07,0x80,
			0xfd,0x00,0xff,0x03,0x00,0x04,0x00,0x07,0x00,0x04,
			0x01,0x02,0x03,0x01,0x06,0x00,0x03,0x7f,0x01,0x7e,
			0x01,0x7c,0x00,0x79,0xff,0x7c,0xff,0x7d,0xfd,0x00,
			0xfa,0x00,0x0e,0x80,0x03,0x80,0x00,0x15,0x0c,0x00,
			0x00,0x6b,0x02,0x80,0x03,0x80,0x00,0x15,0x0a,0x00,
			0x02,0x7f,0x01,0x7d,0x00,0x7b,0xff,0x7e,0xfe,0x7f,
			0xf6,0x00,0x10,0xf7,0x11,0x8f,0xff,0x03,0xff,0x02,
			0xfe,0x01,0xfa,0x00,0xfd,0x7f,0xff,0x7e,0x00,0x7c,
			0x00,0x79,0x00,0x7b,0x01,0x7e,0x03,0x00,0x06,0x00,
			0x02,0x00,0x01,0x03,0x01,0x02,0x03,0xfb,0x03,0x95,
			0x0c,0x00,0xfa,0x80,0x00,0x6b,0x09,0x80,0x03,0x95,
			0x00,0x77,0x06,0x7a,0x06,0x06,0x00,0x09,0xfa,0xf1,
			0xfa,0x7a,0x0e,0x80,0x03,0x87,0x00,0x0b,0x02,0x02,
			0x03,0x00,0x02,0x7e,0x01,0x02,0x04,0x00,0x02,0x7e,
			0x00,0x75,0xfe,0x7e,0xfc,0x00,0xff,0x01,0xfe,0x7f,
			0xfd,0x00,0xfe,0x02,0x07,0x8e,0x00,0x6b,0x09,0x80,
			0x03,0x80,0x0e,0x15,0xf2,0x80,0x0e,0x6b,0x03,0x80,
			0x03,0x95,0x00,0x6b,0x0e,0x00,0x00,0x7d,0xfe,0x98,
			0x00,0x6b,0x05,0x80,0x03,0x95,0x00,0x75,0x02,0x7d,
			0x0a,0x00,0x00,0x8e,0x00,0x6b,0x02,0x80,0x03,0x95,
			0x00,0x6b,0x10,0x00,0x00,0x15,0xf8,0x80,0x00,0x6b,
			0x0a,0x80,0x03,0x95,0x00,0x6b,0x10,0x00,0x00,0x15,
			0xf8,0x80,0x00,0x6b,0x0a,0x00,0x00,0x7d,0x02,0x83,
			0x10,0x80,0x03,0x95,0x00,0x6b,0x09,0x00,0x03,0x02,
			0x00,0x08,0xfd,0x02,0xf7,0x00,0x0e,0x89,0x00,0x6b,
			0x03,0x80,0x03,0x95,0x00,0x6b,0x09,0x00,0x03,0x02,
			0x00,0x08,0xfd,0x02,0xf7,0x00,0x0e,0xf4,0x03,0x92,
			0x02,0x03,0x07,0x00,0x03,0x7d,0x00,0x70,0xfd,0x7e,
			0xf9,0x00,0xfe,0x02,0x03,0x89,0x09,0x00,0x02,0xf5,
			0x03,0x80,0x00,0x15,0x00,0xf5,0x07,0x00,0x00,0x08,
			0x02,0x03,0x06,0x00,0x02,0x7d,0x00,0x70,0xfe,0x7e,
			0xfa,0x00,0xfe,0x02,0x00,0x08,0x0c,0xf6,0x0f,0x80,
			0x00,0x15,0xf6,0x00,0xfe,0x7d,0x00,0x79,0x02,0x7e,
			0x0a,0x00,0xf4,0xf7,0x07,0x09,0x07,0xf7,0x03,0x8c,
			0x01,0x02,0x01,0x01,0x05,0x00,0x02,0x7f,0x01,0x7e,
			0x00,0x74,0x00,0x86,0xff,0x01,0xfe,0x01,0xfb,0x00,
			0xff,0x7f,0xff,0x7f,0x00,0x7c,0x01,0x7e,0x01,0x00,
			0x05,0x00,0x02,0x00,0x01,0x02,0x03,0xfe,0x04,0x8e,
			0x02,0x01,0x04,0x00,0x02,0x7f,0x01,0x7e,0x00,0x77,
			0xff,0x7e,0xfe,0x7f,0xfc,0x00,0xfe,0x01,0xff,0x02,
			0x00,0x09,0x01,0x02,0x02,0x02,0x03,0x01,0x02,0x01,
			0x01,0x01,0x01,0x02,0x02,0xeb,0x03,0x80,0x00,0x15,
			0x03,0x00,0x02,0x7e,0x00,0x7b,0xfe,0x7e,0xfd,0x00,
			0x03,0x80,0x04,0x00,0x03,0x7e,0x00,0x78,0xfd,0x7e,
			0xf9,0x00,0x0c,0x80,0x03,0x8c,0x02,0x02,0x02,0x01,
			0x03,0x00,0x02,0x7f,0x01,0x7d,0xfe,0x7e,0xf9,0x7d,
			0xff,0x7e,0x00,0x7d,0x03,0x7f,0x02,0x00,0x03,0x01,
			0x02,0x01,0x02,0xfe,0x0d,0x8c,0xff,0x02,0xfe,0x01,
			0xfc,0x00,0xfe,0x7f,0xff,0x7e,0x00,0x77,0x01,0x7e,
			0x02,0x7f,0x04,0x00,0x02,0x01,0x01,0x02,0x00,0x0f,
			0xff,0x02,0xfe,0x01,0xf9,0x00,0x0c,0xeb,0x03,0x88,
			0x0a,0x00,0x00,0x02,0x00,0x03,0xfe,0x02,0xfa,0x00,
			0xff,0x7e,0xff,0x7d,0x00,0x7b,0x01,0x7c,0x01,0x7f,
			0x06,0x00,0x02,0x02,0x03,0xfe,0x03,0x8f,0x06,0x77,
			0x06,0x09,0xfa,0x80,0x00,0x71,0xff,0x87,0xfb,0x79,
			0x07,0x87,0x05,0x79,0x02,0x80,0x03,0x8d,0x02,0x02,
			0x06,0x00,0x02,0x7e,0x00,0x7d,0xfc,0x7d,0x04,0x7e,
			0x00,0x7d,0xfe,0x7e,0xfa,0x00,0xfe,0x02,0x04,0x85,
			0x02,0x00,0x06,0xf9,0x03,0x8f,0x00,0x73,0x01,0x7e,
			0x07,0x00,0x02,0x02,0x00,0x0d,0x00,0xf3,0x01,0x7e,
			0x03,0x80,0x03,0x8f,0x00,0x73,0x01,0x7e,0x07,0x00,
			0x02,0x02,0x00,0x0d,0x00,0xf3,0x01,0x7e,0xf8,0x90,
			0x03,0x00,0x08,0xf0,0x03,0x80,0x00,0x15,0x00,0xf3,
			0x02,0x00,0x06,0x07,0xfa,0xf9,0x07,0x78,0x03,0x80,
			0x03,0x80,0x04,0x0c,0x02,0x03,0x04,0x00,0x00,0x71,
			0x02,0x80,0x03,0x80,0x00,0x0f,0x06,0x77,0x06,0x09,
			0x00,0x71,0x02,0x80,0x03,0x80,0x00,0x0f,0x0a,0xf1,
			0x00,0x0f,0xf6,0xf8,0x0a,0x00,0x02,0xf9,0x05,0x80,
			0xff,0x01,0xff,0x04,0x00,0x05,0x01,0x03,0x01,0x02,
			0x06,0x00,0x02,0x7e,0x00,0x7d,0x00,0x7b,0x00,0x7c,
			0xfe,0x7f,0xfa,0x00,0x0b,0x80,0x03,0x80,0x00,0x0f,
			0x00,0xfb,0x01,0x03,0x01,0x02,0x05,0x00,0x02,0x7e,
			0x01,0x7d,0x00,0x76,0x03,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,0x10,0x80,
			0x10,0x80,0x0a,0x8f,0x02,0x7f,0x01,0x7e,0x00,0x76,
			0xff,0x7f,0xfe,0x7f,0xfb,0x00,0xff,0x01,0xff,0x01,
			0x00,0x0a,0x01,0x02,0x01,0x01,0x05,0x00,0xf9,0x80,
			0x00,0x6b,0x0c,0x86,0x0d,0x8a,0xff,0x03,0xfe,0x02,
			0xfb,0x00,0xff,0x7e,0xff,0x7d,0x00,0x7b,0x01,0x7c,
			0x01,0x7f,0x05,0x00,0x02,0x01,0x01,0x03,0x03,0xfc,
			0x03,0x80,0x00,0x0f,0x00,0xfb,0x01,0x03,0x01,0x02,
			0x04,0x00,0x01,0x7e,0x01,0x7d,0x00,0x76,0x00,0x8a,
			0x01,0x03,0x02,0x02,0x03,0x00,0x02,0x7e,0x01,0x7d,
			0x00,0x76,0x03,0x80,0x03,0x8f,0x00,0x74,0x01,0x7e,
			0x02,0x7f,0x04,0x00,0x02,0x01,0x01,0x01,0x00,0x8d,
			0x00,0x6e,0xff,0x7e,0xfe,0x7f,0xfb,0x00,0xfe,0x01,
			0x0c,0x85,0x03,0x8d,0x01,0x02,0x03,0x00,0x02,0x7e,
			0x01,0x02,0x03,0x00,0x02,0x7e,0x00,0x74,0xfe,0x7f,
			0xfd,0x00,0xff,0x01,0xfe,0x7f,0xfd,0x00,0xff,0x01,
			0x00,0x0c,0x06,0x82,0x00,0x6b,0x08,0x86,0x03,0x80,
			0x0a,0x0f,0xf6,0x80,0x0a,0x71,0x03,0x80,0x03,0x8f,
			0x00,0x73,0x01,0x7e,0x07,0x00,0x02,0x02,0x00,0x0d,
			0x00,0xf3,0x01,0x7e,0x00,0x7e,0x03,0x82,0x03,0x8f,
			0x00,0x79,0x02,0x7e,0x08,0x00,0x00,0x89,0x00,0x71,
			0x02,0x80,0x03,0x8f,0x00,0x73,0x01,0x7e,0x03,0x00,
			0x02,0x02,0x00,0x0d,0x00,0xf3,0x01,0x7e,0x03,0x00,
			0x02,0x02,0x00,0x0d,0x00,0xf3,0x01,0x7e,0x03,0x80,
			0x03,0x8f,0x00,0x73,0x01,0x7e,0x03,0x00,0x02,0x02,
			0x00,0x0d,0x00,0xf3,0x01,0x7e,0x03,0x00,0x02,0x02,
			0x00,0x0d,0x00,0xf3,0x01,0x7e,0x00,0x7e,0x03,0x82,
			0x03,0x8d,0x00,0x02,0x02,0x00,0x00,0x71,0x08,0x00,
			0x02,0x02,0x00,0x06,0xfe,0x02,0xf8,0x00,0x0c,0xf6,
			0x03,0x8f,0x00,0x71,0x07,0x00,0x02,0x02,0x00,0x06,
			0xfe,0x02,0xf9,0x00,0x0c,0x85,0x00,0x71,0x02,0x80,
			0x03,0x8f,0x00,0x71,0x07,0x00,0x03,0x02,0x00,0x06,
			0xfd,0x02,0xf9,0x00,0x0c,0xf6,0x03,0x8d,0x02,0x02,
			0x06,0x00,0x02,0x7e,0x00,0x75,0xfe,0x7e,0xfa,0x00,
			0xfe,0x02,0x04,0x85,0x06,0x00,0x02,0xf9,0x03,0x80,
			0x00,0x0f,0x00,0xf8,0x04,0x00,0x00,0x06,0x02,0x02,
			0x04,0x00,0x02,0x7e,0x00,0x75,0xfe,0x7e,0xfc,0x00,
			0xfe,0x02,0x00,0x05,0x0a,0xf9,0x0d,0x80,0x00,0x0f,
			0xf7,0x00,0xff,0x7e,0x00,0x7b,0x01,0x7e,0x09,0x00,
			0xf6,0xfa,0x04,0x06,0x08,0xfa
		};
	};
}