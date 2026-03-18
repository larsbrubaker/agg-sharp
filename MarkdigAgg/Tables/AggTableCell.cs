// Copyright (c) 2025, Nicolas Musset, John Lewin, Lars Brubaker
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class AggTableCell : GuiWidget
	{
		public AggTableCell()
		{
			Width = 300;
			Height = 25;
			this.Layout += AggTableCell_Layout;
		}

		private void AggTableCell_Layout(object sender, EventArgs e)
		{
			if (this.Children.Count > 0 && this.Children.First() is FlowLeftRightWithWrapping wrappedChild)
			{
				wrappedChild.ContentHAnchor = this.FlowHAnchor;
				ContentWidth = wrappedChild.ContentWidth;

				if (this.Parent is AggTableRow parentRow)
				{
					parentRow.CellHeightChanged(wrappedChild.Height);
				}
			}
		}

		public double ContentWidth { get; private set; }

		// TODO: Use to align child content when bounds are less than current
		public HAnchor FlowHAnchor { get; set; }
	}
}
