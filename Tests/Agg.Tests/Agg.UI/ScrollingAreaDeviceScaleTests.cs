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
	/// The scroll area's <see cref="GuiWidget.Margin"/> and <see cref="GuiWidget.Padding"/> are design units, while
	/// every bound they were added to is in device pixels. At 1x the two agree; at 2x the clamp stopped the content
	/// margin x (scale - 1) short of (or past) where layout draws the gap, and the ratio and overflow reads were off
	/// the same way.
	/// </summary>
	/// <remarks>
	/// <see cref="GuiWidget.DeviceScale"/> is process wide, so these are keyless <c>[NotInParallel]</c> and restore
	/// it in a finally. Every widget is built after the scale is set, the way an app built on that display does.
	/// </remarks>
	public class ScrollingAreaDeviceScaleTests
	{
		private const double MarginDesign = 5;

		/// <summary>
		/// Scrolled as far as it goes each way, the content's edge stops exactly the scroll area's margin inside the
		/// view's edge, in device pixels.
		/// </summary>
		/// <remarks>
		/// The clamp also reads the area's top padding, but it is not set here: every scroll bar update zeroes
		/// ScrollArea.Padding (the "force layout" hack in <see cref="ScrollBar"/>), so it is always 0 by then.
		/// </remarks>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task ScrollEndsLeaveExactlyTheDeviceMargin(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;
				var scrollable = MakeScrollable(scale, 300, 400);

				double margin = MarginDesign * scale;

				// content left and top as far right and down as it goes: the start of the content
				scrollable.ScrollPosition = new Vector2(100000, -100000);
				RectangleDouble content = scrollable.ScrollArea.BoundsRelativeToParent;
				await Assert.That(content.Top).IsEqualTo(scrollable.LocalBounds.Top - margin).Within(0.001)
					.Because($"at the top the content sits the device margin below the view's top (content {content})");
				await Assert.That(content.Left).IsEqualTo(scrollable.LocalBounds.Left + margin).Within(0.001)
					.Because($"at the left the content sits the device margin inside the view's left (content {content})");

				// and the other way: the end of the content
				scrollable.ScrollPosition = new Vector2(-100000, 100000);
				content = scrollable.ScrollArea.BoundsRelativeToParent;
				await Assert.That(content.Bottom).IsEqualTo(scrollable.LocalBounds.Bottom + margin).Within(0.001)
					.Because($"at the bottom the content sits the device margin above the view's bottom (content {content})");
				await Assert.That(content.Right).IsEqualTo(scrollable.LocalBounds.Right - margin).Within(0.001)
					.Because($"at the right the content sits the device margin inside the view's right (content {content})");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// The same design at 2x is the 1x design drawn twice as big, so every ratio the scroll bar reads or sets
		/// must come out the same and every position twice as far.
		/// </summary>
		[Test]
		[NotInParallel]
		public async Task RatiosAtTwoXMatchOneX()
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				var (oneXViewRatio, oneXHalfWayTop, oneXReadBack) = MeasureRatios(1);
				var (twoXViewRatio, twoXHalfWayTop, twoXReadBack) = MeasureRatios(2);

				await Assert.That(twoXViewRatio.Y).IsEqualTo(oneXViewRatio.Y).Within(0.0001)
					.Because("the thumb is the same share of the track at any scale");
				await Assert.That(twoXViewRatio.X).IsEqualTo(oneXViewRatio.X).Within(0.0001);

				await Assert.That(twoXHalfWayTop).IsEqualTo(oneXHalfWayTop * 2).Within(0.001)
					.Because("half way down lands twice as far from the view's top at 2x");

				await Assert.That(twoXReadBack.Y).IsEqualTo(oneXReadBack.Y).Within(0.0001)
					.Because("the ratio read back from a scrolled position is the same at any scale");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>
		/// Content that fits the view on its own but not with its margins needs a scroll bar, at 2x as at 1x.
		/// </summary>
		[Test]
		[NotInParallel]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public async Task ContentTallerThanTheViewOnlyByItsMarginsShowsTheBar(double scale)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = scale;

				// 6 design units short of the view: fits on its own, and on its own plus 2 x 5 of margin counted in
				// design units at 2x, but overflows with that margin counted in device pixels
				var scrollable = MakeScrollable(scale, 150, 194, ScrollBar.ShowState.WhenRequired);

				await Assert.That(scrollable.VerticalScrollBar.Visible).IsTrue()
					.Because("the margins are part of how far the content moves, so there is something to scroll to");
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		private static (Vector2 viewRatio, double halfWayTopFromViewTop, Vector2 readBack) MeasureRatios(double scale)
		{
			GuiWidget.DeviceScale = scale;
			var scrollable = MakeScrollable(scale, 300, 400);

			Vector2 viewRatio = scrollable.RatioOfViewToContents0To1();

			scrollable.ScrollRatioFromTop0To1 = new Vector2(0, 0.5);
			double halfWayTop = scrollable.LocalBounds.Top - scrollable.ScrollArea.BoundsRelativeToParent.Top;

			// a quarter of the way through the travel, measured from the two ends the clamp allows
			scrollable.ScrollPosition = new Vector2(0, -100000);
			double topEnd = scrollable.ScrollPosition.Y;
			scrollable.ScrollPosition = new Vector2(0, 100000);
			double bottomEnd = scrollable.ScrollPosition.Y;
			scrollable.ScrollPosition = new Vector2(0, topEnd + (bottomEnd - topEnd) * .25);

			return (viewRatio, halfWayTop, scrollable.ScrollRatioFromTop0To1);
		}

		/// <summary>
		/// A 200 x 200 design-unit view holding one content block, both sized in device pixels for
		/// <paramref name="scale"/>, with a design-unit margin on every side.
		/// </summary>
		private static ScrollableWidget MakeScrollable(double scale, double contentWidth, double contentHeight,
			ScrollBar.ShowState showState = ScrollBar.ShowState.Never)
		{
			var container = new GuiWidget(400 * scale, 400 * scale);
			var scrollable = new ScrollableWidget(200 * scale, 200 * scale, autoScroll: true);
			scrollable.MinimumSize = Vector2.Zero;
			scrollable.VerticalScrollBar.Show = showState;
			container.AddChild(scrollable);

			scrollable.ScrollArea.Margin = new BorderDouble(MarginDesign);
			scrollable.AddChild(new GuiWidget(contentWidth * scale, contentHeight * scale));

			return scrollable;
		}
	}
}
