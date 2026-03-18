// Copyright (c) 2026, Lars Brubaker, Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using Markdig.Renderers.Agg.Inlines;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class HeadingRowX : FlowLeftRightWithWrapping
	{
		private readonly double pointSize;

		public int Level { get; }

		public HeadingRowX(int level)
		{
			Level = level;
			pointSize = GetPointSize(level);
			this.VAnchor = VAnchor.Fit;
			this.HAnchor = HAnchor.Stretch;
			this.Margin = new BorderDouble(3, 4, 0, 12);
			this.RowPadding = new BorderDouble(0, 3);
		}

		private static double GetPointSize(int level)
		{
			return level switch
			{
				1 => 20,
				2 => 17,
				3 => 15,
				4 => 13,
				5 => 12,
				_ => 11
			};
		}

		public override GuiWidget AddChild(GuiWidget childToAdd, int indexInChildrenList = -1)
		{
			if (childToAdd is TextWidget textWidget)
			{
				textWidget.PointSize = pointSize;
				textWidget.Bold = true;
			}
			else if (childToAdd is TextLinkX textLink)
			{
				foreach (var child in childToAdd.Children)
				{
					if (child is TextWidget childTextWidget)
					{
						childTextWidget.PointSize = pointSize;
						childTextWidget.Bold = true;
					}
				}
			}

			return base.AddChild(childToAdd, indexInChildrenList);
		}
	}
}
