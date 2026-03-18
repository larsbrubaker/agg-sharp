/*
Copyright (c) 2025, Lars Brubaker
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
using Markdig.Agg;
using Markdig.Renderers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using TUnit.Assertions;
using TUnit.Core;

namespace Markdig.Agg.Tests
{
	public class MarkdownFeatureRenderingTests
	{
		[Test]
		public async Task HeadingLevelsUseDistinctSizes()
		{
			var root = RenderMarkdown(
				"""
				# Heading 1
				## Heading 2
				### Heading 3
				""");

			var headings = root.Children.OfType<HeadingRowX>().ToList();
			await Assert.That(headings.Count).IsEqualTo(3);

			var sizes = headings
				.Select(heading => heading.Descendants<TextWidget>().First().PointSize)
				.ToList();

			await Assert.That(sizes[0]).IsGreaterThan(sizes[1]);
			await Assert.That(sizes[1]).IsGreaterThan(sizes[2]);
		}

		[Test]
		public async Task OrderedListsRenderNumberMarkers()
		{
			var root = RenderMarkdown(
				"""
				1. First step
				2. Second step
				3. Third step
				""");

			var textWidgets = root.Descendants<TextWidget>().ToList();
			await Assert.That(textWidgets.Any(text => text.Text == "1.")).IsTrue();
			await Assert.That(textWidgets.Any(text => text.Text == "2.")).IsTrue();
			await Assert.That(textWidgets.Any(text => text.Text == "3.")).IsTrue();
		}

		[Test]
		public async Task NestedListsIndentNestedItems()
		{
			var root = RenderMarkdown(
				"""
				- First bullet
				- Second bullet
				  - Nested bullet
				""");

			var lists = root.Descendants<ListX>().ToList();
			await Assert.That(lists.Count).IsGreaterThan(1);
			await Assert.That(lists.Skip(1).First().Margin.Left).IsGreaterThan(0);
		}

		[Test]
		public async Task BlockQuotesRenderContainedText()
		{
			var root = RenderMarkdown(
				"""
				> Keep help articles short, practical, and easy to scan.
				""");

			var quote = root.Descendants<QuoteBlockX>().FirstOrDefault();
			await Assert.That(quote).IsNotNull();
			await Assert.That(quote.Descendants<TextWidget>().Any(text => text.Text == "Keep")).IsTrue();
		}

		[Test]
		public async Task FencedCodeBlocksRenderCodeBlockWidget()
		{
			var root = RenderMarkdown(
				"""
				```cs
				var settings = LoadSettings();
				settings.Save();
				```
				""");

			var codeBlock = root.Descendants<CodeBlockX>().FirstOrDefault();
			await Assert.That(codeBlock).IsNotNull();
			await Assert.That(codeBlock.Descendants<TextWidget>().Any(text => text.Text == "settings.Save();")).IsTrue();
		}

		[Test]
		public async Task CodeBlocksPreserveLeadingSpacesAndUseMonospaceFont()
		{
			var root = RenderMarkdown(
				"""
				```md
				  - Nested bullet
				```
				""");

			var codeText = root.Descendants<TextWidget>().FirstOrDefault(text => text.Text.Contains("Nested bullet"));
			await Assert.That(codeText).IsNotNull();
			await Assert.That(codeText.Text).IsEqualTo("  - Nested bullet");
			await Assert.That(codeText.Printer.TypeFaceStyle.TypeFace).IsNotEqualTo(AggContext.DefaultFont);
			await Assert.That(codeText.Height).IsGreaterThan(codeText.Printer.LocalBounds.Height);
		}

		[Test]
		public async Task HorizontalRulesRenderVisibleLine()
		{
			var root = RenderMarkdown(
				"""
				Top

				---

				Bottom
				""");

			await Assert.That(root.Descendants<HorizontalLine>().Any()).IsTrue();
		}

		private static GuiWidget RenderMarkdown(string markdown)
		{
			var root = new GuiWidget();
			var document = new AggMarkdownDocument
			{
				Markdown = markdown
			};

			document.Parse(new ThemeConfig(), root);
			return root;
		}
	}
}
