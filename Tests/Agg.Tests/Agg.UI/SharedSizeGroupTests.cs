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

using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	public class SharedSizeGroupTests
	{
		[Test]
		public async Task BasicEqualization_WidthsMatchLargest()
		{
			var scope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 200,
			};

			var row1 = new FlowLayoutWidget();
			var label1 = new GuiWidget { Width = 30, Height = 20, SharedSizeGroupName = "labels" };
			row1.AddChild(label1);
			row1.AddChild(new GuiWidget { Width = 100, Height = 20 });
			scope.AddChild(row1);

			var row2 = new FlowLayoutWidget();
			var label2 = new GuiWidget { Width = 50, Height = 20, SharedSizeGroupName = "labels" };
			row2.AddChild(label2);
			row2.AddChild(new GuiWidget { Width = 100, Height = 20 });
			scope.AddChild(row2);

			var row3 = new FlowLayoutWidget();
			var label3 = new GuiWidget { Width = 40, Height = 20, SharedSizeGroupName = "labels" };
			row3.AddChild(label3);
			row3.AddChild(new GuiWidget { Width = 100, Height = 20 });
			scope.AddChild(row3);

			scope.PerformLayout();

			await Assert.That(label1.Width).IsEqualTo(50);
			await Assert.That(label2.Width).IsEqualTo(50);
			await Assert.That(label3.Width).IsEqualTo(50);
		}

		[Test]
		public async Task MultipleGroups_EachGroupEqualizesIndependently()
		{
			var scope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 200,
			};

			var row1 = new FlowLayoutWidget();
			var label1 = new GuiWidget { Width = 30, Height = 20, SharedSizeGroupName = "labels" };
			var value1 = new GuiWidget { Width = 80, Height = 20, SharedSizeGroupName = "values" };
			row1.AddChild(label1);
			row1.AddChild(value1);
			scope.AddChild(row1);

			var row2 = new FlowLayoutWidget();
			var label2 = new GuiWidget { Width = 50, Height = 20, SharedSizeGroupName = "labels" };
			var value2 = new GuiWidget { Width = 60, Height = 20, SharedSizeGroupName = "values" };
			row2.AddChild(label2);
			row2.AddChild(value2);
			scope.AddChild(row2);

			scope.PerformLayout();

			// "labels" group: max(30, 50) = 50
			await Assert.That(label1.Width).IsEqualTo(50);
			await Assert.That(label2.Width).IsEqualTo(50);

			// "values" group: max(80, 60) = 80
			await Assert.That(value1.Width).IsEqualTo(80);
			await Assert.That(value2.Width).IsEqualTo(80);
		}

		[Test]
		public async Task NestedScopes_GroupsAreIndependent()
		{
			var outerScope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 300,
			};

			var outerLabel = new GuiWidget { Width = 100, Height = 20, SharedSizeGroupName = "col" };
			outerScope.AddChild(outerLabel);

			var innerScope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
			};
			var innerLabel = new GuiWidget { Width = 200, Height = 20, SharedSizeGroupName = "col" };
			innerScope.AddChild(innerLabel);
			outerScope.AddChild(innerScope);

			outerScope.PerformLayout();

			// The outer "col" group should only contain outerLabel (innerLabel is in the nested scope)
			await Assert.That(outerLabel.Width).IsEqualTo(100);
			// The inner "col" group should only contain innerLabel
			await Assert.That(innerLabel.Width).IsEqualTo(200);
		}

		[Test]
		public async Task DynamicAdd_NewWidgetGetsEqualized()
		{
			var scope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 200,
			};

			var row1 = new FlowLayoutWidget();
			var label1 = new GuiWidget { Width = 50, Height = 20, SharedSizeGroupName = "labels" };
			row1.AddChild(label1);
			scope.AddChild(row1);

			scope.PerformLayout();
			await Assert.That(label1.Width).IsEqualTo(50);

			// Add a new row with a wider label
			var row2 = new FlowLayoutWidget();
			var label2 = new GuiWidget { Width = 80, Height = 20, SharedSizeGroupName = "labels" };
			row2.AddChild(label2);
			scope.AddChild(row2);

			// After adding the new child, layout runs and equalizes
			await Assert.That(label1.Width).IsEqualTo(80);
			await Assert.That(label2.Width).IsEqualTo(80);
		}

		[Test]
		public async Task InvisibleWidgets_ExcludedFromEqualization()
		{
			var scope = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 200,
			};

			var row1 = new FlowLayoutWidget();
			var label1 = new GuiWidget { Width = 30, Height = 20, SharedSizeGroupName = "labels" };
			row1.AddChild(label1);
			scope.AddChild(row1);

			var row2 = new FlowLayoutWidget();
			var label2 = new GuiWidget { Width = 80, Height = 20, SharedSizeGroupName = "labels", Visible = false };
			row2.AddChild(label2);
			scope.AddChild(row2);

			scope.PerformLayout();

			// label2 is invisible, so label1 should stay at its own width
			await Assert.That(label1.Width).IsEqualTo(30);
		}

		[Test]
		public async Task NoScope_NoEqualization()
		{
			// Container without IsSharedSizeScope - groups should not equalize
			var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Width = 400,
				Height = 200,
			};

			var label1 = new GuiWidget { Width = 30, Height = 20, SharedSizeGroupName = "labels" };
			container.AddChild(label1);

			var label2 = new GuiWidget { Width = 50, Height = 20, SharedSizeGroupName = "labels" };
			container.AddChild(label2);

			container.PerformLayout();

			await Assert.That(label1.Width).IsEqualTo(30);
			await Assert.That(label2.Width).IsEqualTo(50);
		}

		[Test]
		public async Task SimpleAlignLayout_ScopeWorksWithoutFlow()
		{
			var scope = new GuiWidget
			{
				IsSharedSizeScope = true,
				Width = 400,
				Height = 200,
			};

			var child1 = new GuiWidget { Width = 30, Height = 20, SharedSizeGroupName = "col" };
			scope.AddChild(child1);

			var child2 = new GuiWidget { Width = 50, Height = 20, SharedSizeGroupName = "col" };
			scope.AddChild(child2);

			scope.PerformLayout();

			await Assert.That(child1.Width).IsEqualTo(50);
			await Assert.That(child2.Width).IsEqualTo(50);
		}
	}
}
