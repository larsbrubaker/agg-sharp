using MatterHackers.Agg.Font;
using MatterHackers.Agg.Image;
using System.Collections.Generic;
using Xunit;

namespace Agg.Tests.Agg
{
    [Collection("Agg.Font")]
    public class FontTests
    {
        [Fact]
        public void CanPrintTests()
        {
            // Invoke DrawString with a carriage return. If any part of the font pipeline throws, this test fails
            ImageBuffer testImage = new ImageBuffer(300, 300);
            testImage.NewGraphics2D().DrawString("\r", 30, 30);
        }

        [Fact]
        public void TextWrappingTest()
        {
            EnglishTextWrapping englishWrapping = new EnglishTextWrapping(8);
            List<string> wrappedLines = englishWrapping.WrapSingleLineOnWidth("Layers or MM", 30);
            Assert.True(wrappedLines.Count == 3);
            Assert.True(wrappedLines[0] == "Layer");
            Assert.True(wrappedLines[1] == "s or");
            Assert.True(wrappedLines[2] == "MM");
        }
    }
}