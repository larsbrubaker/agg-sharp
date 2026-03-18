/*
Copyright (c) 2026, Lars Brubaker
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

using System;
using System.Collections.Generic;

namespace MatterHackers.Agg.UI
{
	/// <summary>
	/// Coordinates width sharing among widgets that declare the same group name
	/// within a SharedSizeScope. During the scope's layout, all descendants with
	/// the same SharedSizeGroupName are found and sized to the max width among them.
	/// </summary>
	public static class SharedSizeGroup
	{
		/// <summary>
		/// Scans all descendants of the scope widget for SharedSizeGroupName,
		/// collects them by group, and equalizes widths within each group.
		/// Returns true if any widget's width changed.
		/// </summary>
		public static bool ApplySharedSizes(GuiWidget scopeWidget)
		{
			if (!scopeWidget.IsSharedSizeScope)
			{
				return false;
			}

			var groups = new Dictionary<string, List<GuiWidget>>();
			CollectGroupMembers(scopeWidget, groups);

			if (groups.Count == 0)
			{
				return false;
			}

			bool anyChanged = false;
			foreach (var kvp in groups)
			{
				double maxWidth = 0;
				foreach (var member in kvp.Value)
				{
					maxWidth = Math.Max(maxWidth, member.Width);
				}

				foreach (var member in kvp.Value)
				{
					if (member.Width != maxWidth)
					{
						// Use MinimumSize so HAnchor.Fit widgets respect the shared width
						var min = member.MinimumSize;
						if (min.X < maxWidth)
						{
							member.MinimumSize = new VectorMath.Vector2(maxWidth, min.Y);
						}

						if (member.Width < maxWidth)
						{
							member.Width = maxWidth;
						}

						anyChanged = true;
					}
				}
			}

			return anyChanged;
		}

		private static void CollectGroupMembers(GuiWidget parent, Dictionary<string, List<GuiWidget>> groups)
		{
			foreach (var child in parent.Children)
			{
				if (!child.Visible)
				{
					continue;
				}

				var groupName = child.SharedSizeGroupName;
				if (!string.IsNullOrEmpty(groupName))
				{
					if (!groups.TryGetValue(groupName, out var list))
					{
						list = new List<GuiWidget>();
						groups[groupName] = list;
					}

					list.Add(child);
				}

				// Recurse into children, but stop at nested scopes
				if (!child.IsSharedSizeScope)
				{
					CollectGroupMembers(child, groups);
				}
			}
		}
	}
}
