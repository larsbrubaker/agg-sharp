// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license. 
// See the LICENSE.md file in the project root for more information.

using Markdig.Syntax;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class ThematicBreakX : FlowLayoutWidget
	{
		public ThematicBreakX(Color color)
			: base(FlowDirection.TopToBottom)
		{
			HAnchor = HAnchor.Stretch;
			VAnchor = VAnchor.Fit;
			Margin = new BorderDouble(top: 6, bottom: 12);
			AddChild(new HorizontalLine(color));
		}
	}

	public class AggThematicBreakRenderer : AggObjectRenderer<ThematicBreakBlock>
    {
        protected override void Write(AggRenderer renderer, ThematicBreakBlock obj)
        {
            //var line = new System.Windows.Shapes.Line { X2 = 1 };
            //line.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.ThematicBreakStyleKey);

            //var paragraph = new Paragraph
            //{
            //    Inlines = { new InlineUIContainer(line) }
            //};
			renderer.WriteBlock(new ThematicBreakX(renderer.Theme.TextColor.WithAlpha(90))); // paragraph);
        }
    }
}
