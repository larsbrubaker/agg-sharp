/*
Copyright (c) 2026, Lars Brubaker
All rights reserved.
*/

using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;

namespace Markdig.Agg
{
	public readonly record struct MarkdownClipboardData(string PlainText, string Html);

	public class MarkdownTextWidget : TextWidget
	{
		public MarkdownTextWidget(string text, double x = 0, double y = 0, double pointSize = 12, Justification justification = Justification.Left, Color textColor = default, bool ellipsisIfClipped = true, bool underline = false, Color backgroundColor = default, TypeFace typeFace = null, bool bold = false)
			: base(text, x, y, pointSize, justification, textColor, ellipsisIfClipped, underline, backgroundColor, typeFace, bold)
		{
		}

		public int DocumentStartIndex { get; set; }

		public string TrailingText { get; set; } = string.Empty;

		public int DocumentTextEndExclusive => DocumentStartIndex + (Text?.Length ?? 0);

		public int DocumentEndExclusive => DocumentTextEndExclusive + TrailingText.Length;

		public override void OnDraw(Graphics2D graphics2D)
		{
			DrawSelectionHighlight(graphics2D);
			base.OnDraw(graphics2D);
		}

		private void DrawSelectionHighlight(Graphics2D graphics2D)
		{
			var markdownWidget = this.Parents<MarkdownWidget>().FirstOrDefault();
			if (markdownWidget == null
				|| !markdownWidget.TryGetWidgetSelection(this, out int selectionStart, out int selectionEnd, out Color selectionColor)
				|| string.IsNullOrEmpty(Text))
			{
				return;
			}

			var left = Printer.GetOffsetLeftOfCharacterIndex(selectionStart).X;
			var right = Printer.GetOffsetLeftOfCharacterIndex(selectionEnd).X;
			if (right < left)
			{
				(left, right) = (right, left);
			}

			if (right <= left)
			{
				right = left + 1;
			}

			graphics2D.FillRectangle(left, LocalBounds.Bottom, right, LocalBounds.Top, selectionColor);
		}
	}
}
