/*
Copyright (c) 2026, Lars Brubaker
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

using System.Collections.Generic;
using System.Text;
using MatterHackers.Agg.Font;

namespace MatterHackers.Agg.UI
{
	/// <summary>
	/// The visual lines of a word-wrapped text field, and the mapping between an index into the field's text and an
	/// index into the text it draws. Wrapping is display only: the field's Text never gains a newline.
	/// </summary>
	/// <remarks>
	/// A line that breaks at a space draws that space as its newline, so the drawn text is the same length as the
	/// real text up to there. A word too long for a line has a newline inserted inside it, which shifts every later
	/// drawn index by one; <see cref="insertedBreaks"/> records those. The real text index just after an inserted
	/// break has two drawn positions - the end of the upper line and the start of the lower one - and maps to the
	/// start of the lower one; the field keeps its own flag for a caret that End put on the upper one.
	/// </remarks>
	internal sealed class TextEditWordWrap
	{
		/// <summary>The layout of a field that does not wrap: drawn and real indices are the same.</summary>
		public static readonly TextEditWordWrap Unwrapped = new TextEditWordWrap(null, new List<int>());

		/// <summary>Drawn indices of the newlines inserted inside over-long words, ascending.</summary>
		private readonly List<int> insertedBreaks;

		private TextEditWordWrap(string displayText, List<int> insertedBreaks)
		{
			DisplayText = displayText;
			this.insertedBreaks = insertedBreaks;
		}

		/// <summary>The text to draw, with a newline at every wrap. Null for <see cref="Unwrapped"/>.</summary>
		public string DisplayText { get; }

		public bool IsWrapped => DisplayText != null;

		/// <summary>
		/// Wraps <paramref name="text"/> to <paramref name="width"/> pixels in <paramref name="style"/>: at spaces where
		/// it can, inside a word that is wider than a line where it cannot. Re-run for every edit and width change; the
		/// cost is a measure of every character, which is fine for a chat box and slow for a book.
		/// </summary>
		public static TextEditWordWrap Wrap(string text, StyledTypeFace style, double width)
		{
			var wrapper = new EnglishTextWrapping(style);
			var display = new StringBuilder(text.Length + 8);
			var inserted = new List<int>();
			var paragraphs = text.Split('\n');
			for (int i = 0; i < paragraphs.Length; i++)
			{
				if (i > 0)
				{
					display.Append('\n');
				}

				AppendParagraph(display, inserted, paragraphs[i], wrapper.WrapSingleLineOnWidth(paragraphs[i], width));
			}

			return new TextEditWordWrap(display.ToString(), inserted);
		}

		/// <summary>
		/// Lays the lines the wrapper split <paramref name="paragraph"/> into back over it. The wrapper drops the one
		/// space it broke at (only after a line longer than one character, which is its rule too) - that space is drawn
		/// as the newline. Lines that do not lay back exactly leave the paragraph unwrapped rather than misplace the caret.
		/// </summary>
		private static void AppendParagraph(StringBuilder display, List<int> inserted, string paragraph, List<string> lines)
		{
			int displayStart = display.Length;
			int insertedStart = inserted.Count;
			int pos = 0;
			bool spaceConsumed = false;
			for (int i = 0; i < lines.Count; i++)
			{
				var line = lines[i];
				if (i > 0)
				{
					if (spaceConsumed)
					{
						pos++;
					}
					else
					{
						inserted.Add(display.Length);
					}

					display.Append('\n');
				}

				if (pos + line.Length > paragraph.Length
					|| string.CompareOrdinal(paragraph, pos, line, 0, line.Length) != 0)
				{
					pos = -1;
					break;
				}

				display.Append(line);
				pos += line.Length;
				spaceConsumed = line.Length > 1 && pos < paragraph.Length && paragraph[pos] == ' ';
			}

			if (pos >= 0 && spaceConsumed)
			{
				// the paragraph ended in the space it broke at; the caret after it starts the next line
				display.Append('\n');
				pos++;
			}

			if (pos != paragraph.Length)
			{
				display.Length = displayStart;
				inserted.RemoveRange(insertedStart, inserted.Count - insertedStart);
				display.Append(paragraph);
			}
		}

		/// <summary>The drawn index of real text index <paramref name="actualIndex"/>, at the start of a wrapped line.</summary>
		public int ToDisplay(int actualIndex)
		{
			int shift = 0;
			while (shift < insertedBreaks.Count && insertedBreaks[shift] - shift <= actualIndex)
			{
				shift++;
			}

			return actualIndex + shift;
		}

		/// <summary>The real text index of drawn index <paramref name="displayIndex"/>; an inserted newline maps to the
		/// character after it.</summary>
		public int ToActual(int displayIndex)
		{
			int shift = 0;
			while (shift < insertedBreaks.Count && insertedBreaks[shift] < displayIndex)
			{
				shift++;
			}

			return displayIndex - shift;
		}

		/// <summary>Whether drawn index <paramref name="displayIndex"/> is a newline inserted inside a word.</summary>
		public bool IsInsertedBreak(int displayIndex) => insertedBreaks.BinarySearch(displayIndex) >= 0;
	}
}
