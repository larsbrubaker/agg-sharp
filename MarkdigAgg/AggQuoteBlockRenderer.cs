// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using Markdig.Syntax;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class QuoteBlockX : FlowLayoutWidget
	{
		public QuoteBlockX(Color accentColor)
			: base(FlowDirection.TopToBottom)
		{
			HAnchor = HAnchor.Stretch;
			VAnchor = VAnchor.Fit;
			Margin = new BorderDouble(bottom: 12);
			Border = new BorderDouble(left: 2);
			BorderColor = accentColor;
			Padding = new BorderDouble(left: 10, top: 3, bottom: 3);
		}
	}

	public class AggQuoteBlockRenderer : AggObjectRenderer<QuoteBlock>
    {
        /// <inheritdoc/>
        protected override void Write(AggRenderer renderer, QuoteBlock obj)
        {
           // var section = new Section();

			renderer.Push(new QuoteBlockX(renderer.Theme.TextColor.WithAlpha(90))); // section);
            renderer.WriteChildren(obj);
            //section.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.QuoteBlockStyleKey);
            renderer.Pop();
        }
    }
}
