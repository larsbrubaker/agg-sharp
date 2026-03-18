// Copyright (c) 2026, Nicolas Musset, John Lewin, Lars Brubaker
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;

namespace Markdig.Renderers.Agg
{
	public class AggTableColumn
	{
		private TableColumnDefinition ColumnDefinition;

		public AggTableColumn(TableColumnDefinition definition)
		{
			this.ColumnDefinition = definition;
		}

		public List<AggTableCell> Cells { get; } = new List<AggTableCell>();

		public void SetCellWidths()
		{
			double cellPadding = 10;

			if (this.Cells.Count == 0)
			{
				return;
			}

			double maxCellWidth = this.Cells.Select(c => c.ContentWidth).Max() + cellPadding * 2;

			foreach (var cell in this.Cells)
			{
				if (cell.Width != maxCellWidth)
				{
					cell.Width = maxCellWidth;
				}
			}
		}
	}
}
