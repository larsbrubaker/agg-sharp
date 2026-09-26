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
*/

using System;
using System.Linq;
using System.Threading.Tasks;
using MatterHackers.Agg.Transform;
using MatterHackers.VectorMath;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// A popup opened from a widget inside a zoomed parent (a node in a zoomed node editor) mates to where the
	/// anchor is drawn: its edges, its size and a click point inside it are all read through the zoom.
	/// </summary>
	/// <remarks>
	/// <see cref="GuiWidget.DeviceScale"/> is process wide, so the class is a keyless <c>[NotInParallel]</c> and
	/// every case restores it.
	/// </remarks>
	[NotInParallel]
	public class PopupScaledParentTests
	{
		private static readonly Vector2 CanvasPan = new Vector2(20, 10);
		private static readonly Vector2 AnchorPosition = new Vector2(100, 300);

		private static async Task AtDeviceScale(double deviceScale, Func<Task> body)
		{
			double savedDeviceScale = GuiWidget.DeviceScale;
			try
			{
				GuiWidget.DeviceScale = deviceScale;
				await body();
			}
			finally
			{
				GuiWidget.DeviceScale = savedDeviceScale;
			}
		}

		/// <summary>window > canvas (zoom <paramref name="scale"/>, pan 20, 10) > anchor (at 100, 300, 40 x 20).</summary>
		private static (SystemWindow window, GuiWidget anchor) BuildTree(double scale)
		{
			var window = new SystemWindow(800, 600);

			var canvas = new GuiWidget();
			window.AddChild(canvas);
			canvas.LocalBounds = new RectangleDouble(0, 0, 1000, 1000);
			canvas.ParentToChildTransform = Affine.NewScaling(scale) * Affine.NewTranslation(CanvasPan);

			var anchor = new GuiWidget(40, 20)
			{
				Position = AnchorPosition,
			};
			canvas.AddChild(anchor);

			return (window, anchor);
		}

		/// <summary>Where a point in the anchor's coordinates is drawn, written out by hand.</summary>
		private static Vector2 Drawn(double scale, Vector2 anchorPoint) => (AnchorPosition + anchorPoint) * scale + CanvasPan;

		[Test]
		[Arguments(1.0, 1.0)]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task APopupBesideTheAnchorMatesToItsDrawnEdges(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, anchor) = BuildTree(scale);
			var popup = new GuiWidget(60, 30);

			// Beside and above, the way a sub menu opens: the anchor's right to the popup's left, its top to the
			// popup's bottom - both need the anchor's drawn width and height.
			window.ShowPopup(
				new ThemeConfig(),
				new MatePoint(anchor) { Mate = new MateOptions(MateEdge.Right, MateEdge.Top) },
				new MatePoint(popup) { Mate = new MateOptions(MateEdge.Left, MateEdge.Bottom) });

			var anchorTopRight = Drawn(scale, new Vector2(40, 20));
			await Assert.That(popup.Position.X).IsEqualTo(anchorTopRight.X).Within(1e-9)
				.Because($"the popup's left has to meet the anchor's drawn right at zoom {scale}");
			await Assert.That(popup.Position.Y).IsEqualTo(anchorTopRight.Y).Within(1e-9)
				.Because($"the popup's bottom has to meet the anchor's drawn top at zoom {scale}");

			window.Close();
		});

		[Test]
		[Arguments(1.0, 1.0)]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task APopupAtAClickPointOpensWhereTheClickIsDrawn(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, anchor) = BuildTree(scale);
			var popup = new GuiWidget(60, 30);

			// A context menu opens its top left corner at the click, given as a point rectangle in the anchor.
			var click = new Vector2(7, 3);
			window.ShowPopup(
				new ThemeConfig(),
				new MatePoint(anchor) { Mate = new MateOptions(MateEdge.Left, MateEdge.Top) },
				new MatePoint(popup) { Mate = new MateOptions(MateEdge.Left, MateEdge.Top) },
				altBounds: new RectangleDouble(click.X, click.Y, click.X, click.Y));

			var drawnClick = Drawn(scale, click);
			await Assert.That(popup.Position.X).IsEqualTo(drawnClick.X).Within(1e-9)
				.Because($"the popup's left has to be at the drawn click at zoom {scale}");
			await Assert.That(popup.Position.Y + popup.Height).IsEqualTo(drawnClick.Y).Within(1e-9)
				.Because($"the popup's top has to be at the drawn click at zoom {scale}");

			window.Close();
		});

		/// <summary>
		/// An anchor whose LocalBounds do not start at 0, 0 - a text widget's bounds reach below its origin by the
		/// font's descent - is met at its drawn edges by both popup paths: a mated popup and a drop-down list.
		/// </summary>
		[Test]
		[Arguments(1.0, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(1.0, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task BothPopupPathsMeetTheDrawnEdgesOfAnAnchorNotStartingAtItsOrigin(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var offsetBounds = new RectangleDouble(-6, -4, 34, 16);

			// A mated popup: under the anchor, left edges together.
			var (window, anchor) = BuildTree(scale);
			anchor.LocalBounds = offsetBounds;
			var popup = new GuiWidget(60, 30);
			window.ShowPopup(
				new ThemeConfig(),
				new MatePoint(anchor) { Mate = new MateOptions(MateEdge.Left, MateEdge.Bottom) },
				new MatePoint(popup) { Mate = new MateOptions(MateEdge.Left, MateEdge.Top) });

			var drawnBottomLeft = Drawn(scale, new Vector2(offsetBounds.Left, offsetBounds.Bottom));
			await Assert.That(popup.Position.X).IsEqualTo(drawnBottomLeft.X).Within(1e-9)
				.Because($"a mated popup has to line up with the anchor's drawn left at zoom {scale}");
			await Assert.That(popup.Position.Y + popup.Height).IsEqualTo(drawnBottomLeft.Y).Within(1e-9)
				.Because($"a mated popup has to meet the anchor's drawn bottom at zoom {scale}");
			window.Close();

			// The drop-down list path (PopupLayoutEngine), anchored to the same shape. A DropDownList sizes its
			// own bounds from 0, 0, so the engine is given the offset anchor directly.
			(window, anchor) = BuildTree(scale);
			anchor.LocalBounds = offsetBounds;
			var content = new GuiWidget(60, 30);
			var list = new PopupWidget(content, new PopupLayoutEngine(content, anchor, Direction.Down, 0, false), false);

			await Assert.That(list.Position.X).IsEqualTo(drawnBottomLeft.X).Within(1e-9)
				.Because($"a drop-down list has to line up with the anchor's drawn left at zoom {scale}");
			await Assert.That(list.Position.Y + list.Height).IsEqualTo(drawnBottomLeft.Y).Within(1e-9)
				.Because($"a drop-down list has to open right under its drawn bottom at zoom {scale}");
			window.Close();
		});

		[Test]
		[Arguments(1.0, 1.0)]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task ADropDownListOpensUnderItsDrawnControl(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, anchor) = BuildTree(scale);
			var dropDown = new DropDownList("none", Color.Black);
			dropDown.AddItem("Item 0");
			dropDown.AddItem("Item 1");
			anchor.AddChild(dropDown);

			dropDown.InvokeClick();

			var drawn = dropDown.TransformToParentSpace(window, dropDown.LocalBounds);
			var popup = window.Children.OfType<PopupWidget>().Single();
			await Assert.That(popup.Position.X).IsEqualTo(drawn.Left).Within(1e-9)
				.Because($"the list has to open aligned with the drawn control at zoom {scale}");
			await Assert.That(popup.Position.Y + popup.Height).IsEqualTo(drawn.Bottom).Within(1e-9)
				.Because($"the list has to open right under the drawn control at zoom {scale}");

			window.Close();
		});

		[Test]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task ARightAlignedDropDownListOpeningUpMeetsItsDrawnTopAndRight(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, anchor) = BuildTree(scale);
			var dropDown = new DropDownList("a long label to be wider than its items", Color.Black, Direction.Up)
			{
				AlignToRightEdge = true,
			};
			dropDown.AddItem("0");
			dropDown.AddItem("1");
			anchor.AddChild(dropDown);
			// Where there is room on the window both above it and to its left, so neither choice is overruled.
			anchor.Position = new Vector2(400, 150);

			dropDown.InvokeClick();

			var drawn = dropDown.TransformToParentSpace(window, dropDown.LocalBounds);
			var popup = window.Children.OfType<PopupWidget>().Single();
			await Assert.That(popup.Position.X + popup.Width).IsEqualTo(drawn.Right).Within(1e-9)
				.Because($"a right aligned list has to line up with the drawn control's right at zoom {scale}");
			await Assert.That(popup.Position.Y).IsEqualTo(drawn.Top).Within(1e-9)
				.Because($"a list opening up has to sit on the drawn control's top at zoom {scale}");

			window.Close();
		});

		/// <summary>
		/// A drop down opened on an item deep in a list longer than its popup scrolls that item to the popup's
		/// middle, so the entries either side of the current one show too.
		/// </summary>
		[Test]
		[Arguments(1.0, 5)]
		[Arguments(1.0, 20)]
		[Arguments(1.0, 34)]
		[Arguments(2.0, 20)]
		public Task OpeningADropDownCentresTheSelectedItem(double deviceScale, int selected) => AtDeviceScale(deviceScale, async () =>
		{
			var window = new SystemWindow(400 * deviceScale, 600 * deviceScale);
			var dropDown = new DropDownList("none", Color.Black, maxHeight: 150 * deviceScale);
			for (int i = 0; i < 40; i++)
			{
				dropDown.AddItem($"Item {i}");
			}

			window.AddChild(dropDown);
			dropDown.Position = new Vector2(10, 400 * deviceScale);
			dropDown.SelectedIndex = selected;

			dropDown.InvokeClick();

			var item = dropDown.MenuItems[selected];
			var container = item.Parents<ScrollableWidget>().First();
			await Assert.That(container.Height).IsLessThan(container.ScrollArea.Height)
				.Because("the list has to be longer than its popup for the scroll to matter");

			var itemInContainer = item.TransformToParentSpace(container, item.LocalBounds);
			var visible = container.LocalBounds;
			await Assert.That(itemInContainer.Bottom >= visible.Bottom - 1e-9 && itemInContainer.Top <= visible.Top + 1e-9).IsTrue()
				.Because($"item {selected} has to be visible when the list opens: at {itemInContainer}, popup {visible}");

			// Near either end of the list the scroll is clamped, so only an item in the middle can sit centred.
			if (selected == 20)
			{
				await Assert.That(itemInContainer.Center.Y).IsEqualTo(visible.Center.Y).Within(1e-6)
					.Because($"item {selected} has to open in the middle of the popup");
			}

			window.Close();
		});
	}
}
