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

using System;
using System.Text;

namespace MatterHackers.Agg.UI
{
	/// <summary>A text field holds only '\n' line ends; these turn "\r\n" and a lone '\r' into one.</summary>
	internal static class TextLineEndings
	{
		public static string Normalize(string text)
		{
			return string.IsNullOrEmpty(text)
				? ""
				: text.Replace("\r\n", "\n").Replace('\r', '\n');
		}

		/// <summary>Normalizes <paramref name="text"/> and moves <paramref name="charIndex"/> to the same character in the result.</summary>
		public static string Normalize(string text, int charIndex, out int normalizedCharIndex)
		{
			if (string.IsNullOrEmpty(text))
			{
				normalizedCharIndex = 0;
				return "";
			}

			int rawLimit = Math.Max(0, Math.Min(charIndex, text.Length));
			int normalizedIndex = 0;
			var builder = new StringBuilder(text.Length);

			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\r')
				{
					builder.Append('\n');
					if (i < rawLimit)
					{
						normalizedIndex++;
					}

					if (i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}
				}
				else
				{
					builder.Append(text[i]);
					if (i < rawLimit)
					{
						normalizedIndex++;
					}
				}
			}

			normalizedCharIndex = normalizedIndex;
			return builder.ToString();
		}
	}
}
