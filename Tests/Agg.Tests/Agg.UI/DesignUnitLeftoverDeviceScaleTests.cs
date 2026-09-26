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

using System.Linq;
using System.Threading.Tasks;
using MatterHackers.VectorMath;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// The last design-unit padding, margin and border values that widgets mixed with device-pixel widths and
	/// bounds - right at 1x, off by the value x (scale - 1) at 2x.
	/// </summary>
	/// <remarks>
	/// <see cref="GuiWidget.DeviceScale"/> is process wide, so these are keyless <c>[NotInParallel]</c> and restore it
	/// in a finally. Every widget is built after the scale is set, the way an app built on that display does.
	/// </remarks>
	public class DesignUnitLeftoverDeviceScaleTests
	{
		/// <summary>
		/// An auto-expanding text is its text plus its padding, in device pixels.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task AutoExpandingTextIsTheTextPlusItsPadding(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var text = new TextWidget("Padded", pointSize: 12)
				{
					AutoExpandBoundsToText = true,
					Padding = new BorderDouble(5),
				};

				RectangleDouble textBounds = text.Printer.LocalBounds;
				await Assert.That(text.Width).IsEqualTo(textBounds.Width + 10 * scale).Within(0.001)
					.Because($"the text plus 5 design units of padding each side at {scale}x");
				await Assert.That(text.Height).IsEqualTo(textBounds.Height + 10 * scale).Within(0.001);
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// A wrapping flow breaks its row at the same design-unit width at every display scale: four 50 unit
		/// items and the rows' 12 units of padding and margin do not fit in 206 units, so the fourth wraps.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task WrappingHappensAtTheSameDesignWidth(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var flow = NewFlowOfFourItems(scale);

				await Assert.That(FilledRows(LayOut(flow, 206, scale))).IsEqualTo(2)
					.Because($"200 units of items need 212 with the row padding and margin, at {scale}x");
				await Assert.That(FilledRows(LayOut(NewFlowOfFourItems(scale), 212, scale))).IsEqualTo(1);
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// A centred wrapping flow puts its row's content in the middle of its width.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task ACentredRowIsInTheMiddle(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var flow = new FlowLeftRightWithWrapping()
				{
					Center = true,
				};
				var first = new GuiWidget(50 * scale, 20 * scale);
				flow.AddChild(first);
				flow.AddChild(new GuiWidget(50 * scale, 20 * scale));

				LayOut(flow, 300, scale);

				// where the item draws in the flow; a row's origin is not its left edge, so the item's Position
				// cannot simply be added to its row's
				double firstLeft = first.TransformToParentSpace(flow, new Vector2(first.LocalBounds.Left, first.LocalBounds.Bottom)).X;
				await Assert.That(firstLeft).IsEqualTo(100 * scale).Within(0.001)
					.Because($"100 units of items centred in 300 start 100 units in, at {scale}x");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// A themed number edit with a one-letter label starts its edit box two units past the label.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task NumberEditStartsTwoUnitsPastItsLabel(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var numberEdit = new ThemedNumberEdit(5, new ThemeConfig(), singleCharLabel: 'X', pixelWidth: 60);
				numberEdit.PerformLayout();

				GuiWidget label = numberEdit.Children.OfType<TextWidget>().First();
				double gap = numberEdit.ActuallNumberEdit.BoundsRelativeToParent.Left - label.BoundsRelativeToParent.Right;
				await Assert.That(gap).IsEqualTo(2 * scale).Within(0.5)
					.Because($"the edit's margin is the label plus its margin plus 2 units, at {scale}x");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// A horizontal splitter's top panel sits inside its border.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task SplitterPanelSitsInsideItsBorder(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var container = new GuiWidget(400 * scale, 200 * scale);
				var splitter = new Splitter()
				{
					Orientation = Orientation.Horizontal,
				};
				splitter.Panel1.Border = new BorderDouble(4);
				container.AddChild(splitter);
				splitter.SplitterDistance = 50 * scale;

				await Assert.That(splitter.Panel1.LocalBounds.Left).IsEqualTo(4 * scale).Within(0.001)
					.Because($"the 4 design-unit border is {4 * scale} px at {scale}x");
				await Assert.That(splitter.Panel1.LocalBounds.Bottom).IsEqualTo(4 * scale).Within(0.001);
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		private static FlowLeftRightWithWrapping NewFlowOfFourItems(double scale)
		{
			var flow = new FlowLeftRightWithWrapping();
			for (int i = 0; i < 4; i++)
			{
				flow.AddChild(new GuiWidget(50 * scale, 20 * scale));
			}

			return flow;
		}

		/// <summary>
		/// Parents the flow in a container of the width under test; the flow wraps against the width it is
		/// stretched to.
		/// </summary>
		private static FlowLeftRightWithWrapping LayOut(FlowLeftRightWithWrapping flow, double designWidth, double scale)
		{
			var parent = new GuiWidget(designWidth * scale, 500 * scale);
			parent.AddChild(flow);
			return flow;
		}

		private static int FilledRows(FlowLeftRightWithWrapping flow)
		{
			return flow.Children.Count(row => row.Children.Count > 0);
		}
	}
}
