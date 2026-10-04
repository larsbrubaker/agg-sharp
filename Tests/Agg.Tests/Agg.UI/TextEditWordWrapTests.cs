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

using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// <see cref="TextEditWidget.WordWrap"/>: a multi-line field that wraps its lines at its width instead of
	/// scrolling sideways, while its Text stays exactly what was typed.
	/// </summary>
	/// <remarks>
	/// What the field draws is read from its <see cref="TextEditWidget.Printer"/>, whose text is the wrapped one;
	/// each newline in it that the real text lacks is a wrap. Keyed on the keyboard because the keys read the
	/// shared Shift state.
	/// </remarks>
	[NotInParallel(MatterHackers.Agg.Tests.SharedStateKeys.UiThreadAndKeyboard)]
	public class TextEditWordWrapTests
	{
		private const string ChatText = "abcdefghijk lmnopqrstuvwxyz123456789";

		private const string LongWord = "abcdefghijklmnopqrstuvwxyz0123456789";

		[Test]
		public async Task TypingIntoANarrowWrappingFieldWrapsInsteadOfScrollingSideways()
		{
			var field = NarrowField(wordWrap: true);
			foreach (var c in ChatText)
			{
				field.InternalTextEditWidget.OnKeyPress(new KeyPressEventArgs(c));
			}

			await Assert.That(VisualLines(field).Length).IsGreaterThan(1)
				.Because("a line wider than the field has to continue on the next line");
			await Assert.That(field.TopLeftOffset.X).IsEqualTo(0.0)
				.Because("a wrapping field never scrolls sideways, so the start of the line stays in view");
			await Assert.That(field.Text).IsEqualTo(ChatText)
				.Because("wrapping is display only; no newline is put into the text");
			await Assert.That(VisualLines(field)[0]).IsEqualTo("abcdefghijk")
				.Because("the line breaks at the space, which is drawn as the line end");
		}

		[Test]
		public async Task AWordTooLongForALineBreaksWhereItOverflows()
		{
			var field = NarrowField(wordWrap: true);
			field.Text = LongWord;

			var lines = VisualLines(field);
			await Assert.That(lines.Length).IsGreaterThan(1);
			await Assert.That(string.Concat(lines)).IsEqualTo(LongWord)
				.Because("an unbroken word breaks between characters and loses none of them");
			await Assert.That(field.Text).IsEqualTo(LongWord);
		}

		[Test]
		public async Task EndGoesToTheEndOfTheVisualLine()
		{
			var field = NarrowField(wordWrap: true);
			field.Text = ChatText;
			var edit = field.InternalTextEditWidget;
			edit.SetCursorPosition(0);
			var firstLineY = edit.InsertBarPosition.Y;

			edit.OnKeyDown(new KeyEventArgs(Keys.End));

			await Assert.That(edit.CharIndexToInsertBefore).IsEqualTo("abcdefghijk".Length)
				.Because("End stops at the end of the first visual line, before the space it wrapped at");
			await Assert.That(edit.InsertBarPosition.Y).IsEqualTo(firstLineY);
		}

		[Test]
		public async Task EndOnALineThatBreaksInsideAWordStaysOnThatLine()
		{
			var field = NarrowField(wordWrap: true);
			field.Text = LongWord;
			var edit = field.InternalTextEditWidget;
			edit.SetCursorPosition(0);
			var firstLineY = edit.InsertBarPosition.Y;

			edit.OnKeyDown(new KeyEventArgs(Keys.End));

			await Assert.That(edit.CharIndexToInsertBefore).IsEqualTo(VisualLines(field)[0].Length);
			await Assert.That(edit.InsertBarPosition.Y).IsEqualTo(firstLineY)
				.Because("the caret is drawn at the end of the line End was pressed on, not the start of the next");
			await Assert.That(edit.InsertBarPosition.X).IsGreaterThan(0.0);
		}

		[Test]
		public async Task UpAndDownMoveBetweenVisualLines()
		{
			var field = NarrowField(wordWrap: true);
			field.Text = ChatText;
			var edit = field.InternalTextEditWidget;
			edit.SetCursorPosition(2);
			var firstLineY = edit.InsertBarPosition.Y;

			edit.OnKeyDown(new KeyEventArgs(Keys.Down));

			await Assert.That(edit.CharIndexToInsertBefore).IsGreaterThan("abcdefghijk".Length)
				.Because("Down moves onto the second visual line, though the text has no second line");
			await Assert.That(edit.InsertBarPosition.Y).IsLessThan(firstLineY);

			edit.OnKeyDown(new KeyEventArgs(Keys.Up));

			await Assert.That(edit.CharIndexToInsertBefore).IsEqualTo(2)
				.Because("Up comes back to the same place on the first line");
		}

		[Test]
		public async Task ResizingTheFieldRewrapsItWithoutChangingTheText()
		{
			var field = NarrowField(wordWrap: true);
			field.Text = ChatText;
			int textChanges = 0;
			field.TextChanged += (s, e) => textChanges++;

			field.Width = 1000;
			await Assert.That(VisualLines(field).Length).IsEqualTo(1)
				.Because("a field wide enough for the whole line shows it on one line");

			field.Width = 100;
			await Assert.That(VisualLines(field).Length).IsGreaterThan(1);
			await Assert.That(textChanges).IsEqualTo(0)
				.Because("re-wrapping changes what is drawn, not the text");
		}

		[Test]
		public async Task WithoutWordWrapTheFieldScrollsSidewaysAsBefore()
		{
			var field = NarrowField(wordWrap: false);
			foreach (var c in ChatText)
			{
				field.InternalTextEditWidget.OnKeyPress(new KeyPressEventArgs(c));
			}

			await Assert.That(field.Printer.Text).IsEqualTo(ChatText);
			await Assert.That(field.TopLeftOffset.X).IsLessThan(0.0)
				.Because("an unwrapped line wider than the field scrolls to keep the caret in view");

			field.InternalTextEditWidget.SetCursorPosition(0);
			field.InternalTextEditWidget.OnKeyDown(new KeyEventArgs(Keys.End));
			await Assert.That(field.CharIndexToInsertBefore).IsEqualTo(ChatText.Length);
		}

		private static TextEditWidget NarrowField(bool wordWrap)
		{
			var field = new TextEditWidget("", pixelWidth: 100, pixelHeight: 100, multiLine: true)
			{
				WordWrap = wordWrap
			};

			var container = new GuiWidget(300, 200);
			container.AddChild(field);
			return field;
		}

		private static string[] VisualLines(TextEditWidget field) => field.Printer.Text.Split('\n');
	}
}
