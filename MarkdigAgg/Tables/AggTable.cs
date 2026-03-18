// Copyright (c) 2026, Nicolas Musset, John Lewin, Lars Brubaker
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class AggTable : FlowLayoutWidget
	{

		public List<AggTableColumn> Columns { get; }

		public List<AggTableRow> Rows { get; }

		public AggTable(Table table) : base(FlowDirection.TopToBottom)
		{
			this.HAnchor = HAnchor.Stretch;
			this.Columns = table.ColumnDefinitions.Select(c => new AggTableColumn(c)).ToList();
		}

		public override void OnLayout(LayoutEventArgs layoutEventArgs)
		{
			base.OnLayout(layoutEventArgs);

			if (this.Columns?.Count > 0)
			{
				foreach (var column in this.Columns)
				{
					column.SetCellWidths();
				}
			}

			// Re-run layout after columns have real measured widths.
			base.OnLayout(layoutEventArgs);
		}
	}
}
