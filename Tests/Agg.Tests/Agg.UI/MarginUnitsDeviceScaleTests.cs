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
using MatterHackers.VectorMath;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// <see cref="GuiWidget.Margin"/> and <see cref="GuiWidget.Padding"/> are design units, while widths, bounds and
	/// mouse positions are device pixels. Each case here added or compared the plain design value with a device one:
	/// right at 1x, off by the value x (scale - 1) at 2x.
	/// </summary>
	/// <remarks>
	/// <see cref="GuiWidget.DeviceScale"/> is process wide, so these are keyless <c>[NotInParallel]</c> and restore it
	/// in a finally. Every widget is built after the scale is set, the way an app built on that display does.
	/// </remarks>
	public class MarginUnitsDeviceScaleTests
	{
		/// <summary>
		/// A fractional padding on an integer-bounds widget is rounded to whole device pixels, like its margin
		/// and border.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task IntegerBoundsRoundTheDevicePadding(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var widget = new GuiWidget(100, 100)
				{
					EnforceIntegerBounds = true,
				};
				widget.Padding = new BorderDouble(1.3);
				widget.Margin = new BorderDouble(1.3);

				await Assert.That(widget.DeviceMargin.Left % 1).IsEqualTo(0)
					.Because("the margin is rounded, the pattern padding should follow");
				await Assert.That(widget.DevicePadding.Left).IsEqualTo(widget.DeviceMargin.Left)
					.Because($"1.3 units is {1.3 * scale} px, rounded to whole pixels at {scale}x");
				await Assert.That(widget.DevicePadding.Top).IsEqualTo(widget.DeviceMargin.Top);
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// The bounds of a widget's children, margins included, reach exactly each child's device margin past it.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task ChildrenBoundsIncludeTheDeviceMargin(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var parent = new GuiWidget(400 * scale, 400 * scale);
				var child = new GuiWidget(50 * scale, 30 * scale)
				{
					Margin = new BorderDouble(5),
				};
				parent.AddChild(child);

				RectangleDouble bounds = parent.GetChildrenBoundsIncludingMargins();
				await Assert.That(bounds.Width).IsEqualTo((50 + 10) * scale).Within(0.001)
					.Because($"the child plus its 5 design-unit margin each side at {scale}x");
				await Assert.That(bounds.Height).IsEqualTo((30 + 10) * scale).Within(0.001);
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// Dragging a splitter bar as far as it goes stops it at the splitter's padding, where it is drawn.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task SplitterDragStopsAtTheDevicePadding(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var container = new GuiWidget(400 * scale, 200 * scale);
				var splitter = new Splitter()
				{
					Padding = new BorderDouble(10),
				};
				container.AddChild(splitter);
				splitter.SplitterDistance = 150 * scale;

				double barMiddle = splitter.SplitterDistance + splitter.SplitterSize / 2;
				splitter.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, barMiddle, 100 * scale, 0));
				splitter.OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, -10000, 100 * scale, 0));
				splitter.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 0, -10000, 100 * scale, 0));

				await Assert.That(splitter.SplitterDistance).IsEqualTo(10 * scale).Within(0.001)
					.Because($"the bar stops at the 10 design-unit padding, {10 * scale} px, at {scale}x");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}
	}
}
