// Copyright (c) Nicolas Musset. All rights reserved.
// Copyright (c) 2025, John Lewin
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using Markdig.Agg;
using Markdig.Extensions.Tables;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace Markdig.Renderers.Agg
{
	public class AggTableRenderer : AggObjectRenderer<Table>
	{
		private const int TableBorderAlpha = 150;
		private const int ZebraStripeAlpha = 12;

		protected override void Write(AggRenderer renderer, Table mdTable)
		{
			if (renderer == null) throw new ArgumentNullException(nameof(renderer));
			if (mdTable == null) throw new ArgumentNullException(nameof(mdTable));

			var aggTable = new AggTable(mdTable)
			{
				Margin = new BorderDouble(top: 12),
			};

			renderer.Push(aggTable);

			for (var rowIndex = 0; rowIndex < mdTable.Count; rowIndex++)
			{
				var mdRow = (TableRow)mdTable[rowIndex];
				var borderColor = new Color(renderer.Theme.TextColor, TableBorderAlpha);

				if (rowIndex == 0)
				{
					var rule = CreateHorizontalRule(borderColor);
					aggTable.HorizontalRules.Add(rule);
					renderer.WriteBlock(rule);
				}
				else
				{
					var rule = CreateHorizontalRule(borderColor);
					aggTable.HorizontalRules.Add(rule);
					renderer.WriteBlock(rule);
				}

				var aggRow = new AggTableRow()
				{
					IsHeadingRow = mdRow.IsHeader,
				};
				aggTable.Rows.Add(aggRow);

				renderer.Push(aggRow);

				if (!mdRow.IsHeader && rowIndex % 2 == 0)
				{
					aggRow.BackgroundColor = new Color(renderer.Theme.TextColor, ZebraStripeAlpha);
				}

				for (var i = 0; i < mdRow.Count; i++)
				{
					var mdCell = (TableCell)mdRow[i];

					var aggCell = new AggTableCell
					{
						BorderColor = borderColor,
						Border = new BorderDouble(
							left: 1,
							right: i == mdRow.Count - 1 ? 1 : 0,
							bottom: 0)
					};
					aggRow.Cells.Add(aggCell);

					if (mdTable.ColumnDefinitions.Count > 0)
					{
						// Grab the column definition, or fall back to a default
						var columnIndex = mdCell.ColumnIndex < 0 || mdCell.ColumnIndex >= mdTable.ColumnDefinitions.Count
							? i
							: mdCell.ColumnIndex;
						columnIndex = columnIndex >= mdTable.ColumnDefinitions.Count ? mdTable.ColumnDefinitions.Count - 1 : columnIndex;

						aggTable.Columns[columnIndex].Cells.Add(aggCell);

						if (mdTable.ColumnDefinitions[columnIndex].Alignment.HasValue)
						{
							switch (mdTable.ColumnDefinitions[columnIndex].Alignment)
							{
								case TableColumnAlign.Center:
									aggCell.FlowHAnchor |= HAnchor.Center;
									break;
								case TableColumnAlign.Right:
									aggCell.FlowHAnchor |= HAnchor.Right;
									break;
								case TableColumnAlign.Left:
									aggCell.FlowHAnchor |= HAnchor.Left;
									break;
							}
						}
					}

					renderer.Push(aggCell);
					renderer.Write(mdCell);
					renderer.Pop();
				}

				// Pop row
				renderer.Pop();
			}

			var finalRule = CreateHorizontalRule(new Color(renderer.Theme.TextColor, TableBorderAlpha));
			aggTable.HorizontalRules.Add(finalRule);
			renderer.WriteBlock(finalRule);

			// Pop table
			renderer.Pop();
		}

		private static HorizontalLine CreateHorizontalRule(Color borderColor)
		{
			return new HorizontalLine(borderColor)
			{
				HAnchor = HAnchor.Left
			};
		}
	}
}