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
using System.Threading.Tasks;
using MatterHackers.Agg.Transform;
using MatterHackers.VectorMath;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// The coordinate helpers (TransformToParentSpace, TransformFromParentSpace, TransformToScreenSpace,
	/// TransformFromScreenSpace) have to agree with where a widget is drawn under a parent that zooms its children,
	/// and "from" has to undo "to". The point "to" and every "from" used to offset by translations alone, and "from"
	/// skipped the widget's own offset, so a point read into a node inside a zoomed node editor was wrong.
	/// </summary>
	/// <remarks>
	/// Every test runs at <see cref="GuiWidget.DeviceScale"/> 1 and 2. It is process wide, so the class is a keyless
	/// <c>[NotInParallel]</c> and <see cref="AtDeviceScale"/> restores it in a finally.
	/// </remarks>
	[NotInParallel]
	public class ScaledParentCoordinateTests
	{
		private static readonly Vector2 ContainerPosition = new Vector2(30, 40);
		private static readonly Vector2 CanvasPan = new Vector2(20, 10);
		private static readonly Vector2 GroupPosition = new Vector2(50, 60);
		private static readonly Vector2 ChildPosition = new Vector2(100, 80);

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

		/// <summary>
		/// window > container (at 30, 40) > canvas (zoom <paramref name="scale"/>, pan 20, 10) > group (at 50, 60)
		/// > child (at 100, 80, 40 x 20).
		/// </summary>
		private static (SystemWindow window, GuiWidget canvas, GuiWidget child) BuildTree(double scale)
		{
			var window = new SystemWindow(800, 600);

			var container = new GuiWidget(700, 500)
			{
				Position = ContainerPosition,
			};
			window.AddChild(container);

			var canvas = new GuiWidget();
			container.AddChild(canvas);
			canvas.LocalBounds = new RectangleDouble(0, 0, 1000, 1000);
			canvas.ParentToChildTransform = Affine.NewScaling(scale) * Affine.NewTranslation(CanvasPan);

			var group = new GuiWidget(400, 300)
			{
				Position = GroupPosition,
			};
			canvas.AddChild(group);

			var child = new GuiWidget(40, 20)
			{
				Position = ChildPosition,
			};
			group.AddChild(child);

			return (window, canvas, child);
		}

		/// <summary>
		/// Where a point in the child's coordinates is drawn in the window, written out by hand rather than through
		/// the helpers under test.
		/// </summary>
		private static Vector2 DrawnInWindow(double scale, Vector2 childPoint)
		{
			return ContainerPosition + (GroupPosition + ChildPosition + childPoint) * scale + CanvasPan;
		}

		[Test]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task PointHelpersAgreeWithWhereTheChildIsDrawn(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, _, child) = BuildTree(scale);
			var childPoint = new Vector2(7, 3);
			var drawn = DrawnInWindow(scale, childPoint);

			var toWindow = child.TransformToParentSpace(window, childPoint);
			await Assert.That(toWindow.X).IsEqualTo(drawn.X).Within(1e-9);
			await Assert.That(toWindow.Y).IsEqualTo(drawn.Y).Within(1e-9);

			var toScreen = child.TransformToScreenSpace(childPoint);
			await Assert.That(toScreen.X).IsEqualTo(drawn.X).Within(1e-9);
			await Assert.That(toScreen.Y).IsEqualTo(drawn.Y).Within(1e-9);

			var fromWindow = child.TransformFromParentSpace(window, drawn);
			await Assert.That(fromWindow.X).IsEqualTo(childPoint.X).Within(1e-9);
			await Assert.That(fromWindow.Y).IsEqualTo(childPoint.Y).Within(1e-9);

			var fromScreen = child.TransformFromScreenSpace(drawn);
			await Assert.That(fromScreen.X).IsEqualTo(childPoint.X).Within(1e-9);
			await Assert.That(fromScreen.Y).IsEqualTo(childPoint.Y).Within(1e-9);

			// The mouse routing is a separate walk (each child inverts its own transform); a press where the helpers
			// say the point is drawn has to land on the child at that point.
			Vector2 received = new Vector2(double.NaN, double.NaN);
			child.MouseDown += (s, e) => received = e.Position;
			window.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, toScreen.X, toScreen.Y, 0));
			window.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, toScreen.X, toScreen.Y, 0));
			await Assert.That(received.X).IsEqualTo(childPoint.X).Within(1e-9);
			await Assert.That(received.Y).IsEqualTo(childPoint.Y).Within(1e-9);
		});

		[Test]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task RectangleHelpersAgreeWithWhereTheChildIsDrawn(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, _, child) = BuildTree(scale);
			var drawnBottomLeft = DrawnInWindow(scale, Vector2.Zero);
			var drawnTopRight = DrawnInWindow(scale, new Vector2(child.Width, child.Height));

			foreach (var drawn in new[] { child.TransformToParentSpace(window, child.LocalBounds), child.TransformToScreenSpace(child.LocalBounds) })
			{
				await Assert.That(drawn.Left).IsEqualTo(drawnBottomLeft.X).Within(1e-9);
				await Assert.That(drawn.Bottom).IsEqualTo(drawnBottomLeft.Y).Within(1e-9);
				await Assert.That(drawn.Right).IsEqualTo(drawnTopRight.X).Within(1e-9);
				await Assert.That(drawn.Top).IsEqualTo(drawnTopRight.Y).Within(1e-9);
			}

			var drawnRectangle = new RectangleDouble(drawnBottomLeft.X, drawnBottomLeft.Y, drawnTopRight.X, drawnTopRight.Y);
			foreach (var local in new[] { child.TransformFromParentSpace(window, drawnRectangle), child.TransformFromScreenSpace(drawnRectangle) })
			{
				await Assert.That(local.Left).IsEqualTo(0).Within(1e-9);
				await Assert.That(local.Bottom).IsEqualTo(0).Within(1e-9);
				await Assert.That(local.Right).IsEqualTo(child.Width).Within(1e-9);
				await Assert.That(local.Top).IsEqualTo(child.Height).Within(1e-9);
			}
		});

		[Test]
		[Arguments(0.5, 1.0)]
		[Arguments(1.5, 1.0)]
		[Arguments(0.5, 2.0)]
		[Arguments(1.5, 2.0)]
		public Task ToThenFromRoundTripsToTheInput(double scale, double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, canvas, child) = BuildTree(scale);
			var point = new Vector2(3.25, -11.5);
			var rectangle = new RectangleDouble(-4, 2.5, 13, 9);

			foreach (var relativeTo in new GuiWidget[] { window, canvas, canvas.Parent, null })
			{
				var point2 = child.TransformFromParentSpace(relativeTo, child.TransformToParentSpace(relativeTo, point));
				await Assert.That(point2.X).IsEqualTo(point.X).Within(1e-9);
				await Assert.That(point2.Y).IsEqualTo(point.Y).Within(1e-9);

				var rectangle2 = child.TransformFromParentSpace(relativeTo, child.TransformToParentSpace(relativeTo, rectangle));
				await Assert.That(rectangle2.Left).IsEqualTo(rectangle.Left).Within(1e-9);
				await Assert.That(rectangle2.Bottom).IsEqualTo(rectangle.Bottom).Within(1e-9);
				await Assert.That(rectangle2.Right).IsEqualTo(rectangle.Right).Within(1e-9);
				await Assert.That(rectangle2.Top).IsEqualTo(rectangle.Top).Within(1e-9);
			}

			var screenPoint = child.TransformFromScreenSpace(child.TransformToScreenSpace(point));
			await Assert.That(screenPoint.X).IsEqualTo(point.X).Within(1e-9);
			await Assert.That(screenPoint.Y).IsEqualTo(point.Y).Within(1e-9);

			var screenRectangle = child.TransformFromScreenSpace(child.TransformToScreenSpace(rectangle));
			await Assert.That(screenRectangle.Left).IsEqualTo(rectangle.Left).Within(1e-9);
			await Assert.That(screenRectangle.Bottom).IsEqualTo(rectangle.Bottom).Within(1e-9);
			await Assert.That(screenRectangle.Right).IsEqualTo(rectangle.Right).Within(1e-9);
			await Assert.That(screenRectangle.Top).IsEqualTo(rectangle.Top).Within(1e-9);
		});

		/// <summary>
		/// With no zoom anywhere the helpers are plain offsets, bit for bit: "to" adds each widget's translation in
		/// turn from the child up (what the code has always done) and "from" subtracts them from the top down.
		/// </summary>
		[Test]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public Task APureTranslationTreeIsExactOffsets(double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, canvas, child) = BuildTree(1);
			canvas.ParentToChildTransform = Affine.NewTranslation(0.1, 0.7);
			var point = new Vector2(0.3, 0.2);

			var expectedTo = point;
			for (var widget = child; widget != window; widget = widget.Parent)
			{
				expectedTo += new Vector2(widget.ParentToChildTransform.tx, widget.ParentToChildTransform.ty);
			}

			var to = child.TransformToParentSpace(window, point);
			await Assert.That(to.X).IsEqualTo(expectedTo.X);
			await Assert.That(to.Y).IsEqualTo(expectedTo.Y);
			var toScreen = child.TransformToScreenSpace(point);
			await Assert.That(toScreen.X).IsEqualTo(expectedTo.X);
			await Assert.That(toScreen.Y).IsEqualTo(expectedTo.Y);

			var rectangle = new RectangleDouble(0.3, 0.2, 1.1, 2.9);
			var expectedRectangle = rectangle;
			for (var widget = child; widget != window; widget = widget.Parent)
			{
				expectedRectangle.Offset(widget.ParentToChildTransform.tx, widget.ParentToChildTransform.ty);
			}

			var toRectangle = child.TransformToParentSpace(window, rectangle);
			await Assert.That(toRectangle.Left).IsEqualTo(expectedRectangle.Left);
			await Assert.That(toRectangle.Bottom).IsEqualTo(expectedRectangle.Bottom);
			await Assert.That(toRectangle.Right).IsEqualTo(expectedRectangle.Right);
			await Assert.That(toRectangle.Top).IsEqualTo(expectedRectangle.Top);

			var screen = new Vector2(123.4, 56.7);
			var expectedFrom = screen - new Vector2(ContainerPosition.X, ContainerPosition.Y)
				- new Vector2(0.1, 0.7) - GroupPosition - ChildPosition;
			var from = child.TransformFromParentSpace(window, screen);
			await Assert.That(from.X).IsEqualTo(expectedFrom.X);
			await Assert.That(from.Y).IsEqualTo(expectedFrom.Y);
			var fromScreen = child.TransformFromScreenSpace(screen);
			await Assert.That(fromScreen.X).IsEqualTo(expectedFrom.X);
			await Assert.That(fromScreen.Y).IsEqualTo(expectedFrom.Y);
		});

		/// <summary>
		/// "From" lands in the widget's own coordinates even when its <see cref="GuiWidget.LocalBounds"/> does not
		/// start at zero (a TextWidget's Bottom is below zero by the font's descent). The old "from" subtracted each
		/// ancestor's <see cref="GuiWidget.BoundsRelativeToParent"/>, which includes that LocalBounds offset, and
		/// skipped the widget itself, so it was off by both.
		/// </summary>
		[Test]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public Task FromLandsInTheWidgetsOwnCoordinatesWhenItsBoundsAreOffset(double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var window = new SystemWindow(800, 600);

			var offsetParent = new GuiWidget();
			window.AddChild(offsetParent);
			offsetParent.LocalBounds = new RectangleDouble(-15, -8, 285, 192);
			offsetParent.OriginRelativeParent = new Vector2(120, 90);

			var text = new TextWidget("Descent");
			offsetParent.AddChild(text);
			text.OriginRelativeParent = new Vector2(33, 21);
			await Assert.That(text.LocalBounds.Bottom).IsLessThan(0)
				.Because("the case needs a widget whose LocalBounds starts below zero");

			// Where the text's own point (x, y) is drawn, written out by hand: each widget offsets by its origin.
			var textPoint = new Vector2(text.LocalBounds.Left + 2, text.LocalBounds.Bottom + 1);
			var drawn = new Vector2(120 + 33 + textPoint.X, 90 + 21 + textPoint.Y);

			foreach (var from in new[] { text.TransformFromParentSpace(window, drawn), text.TransformFromScreenSpace(drawn) })
			{
				await Assert.That(from.X).IsEqualTo(textPoint.X).Within(1e-9);
				await Assert.That(from.Y).IsEqualTo(textPoint.Y).Within(1e-9);
			}

			var parentPoint = new Vector2(-12.5, -6);
			var fromIntoParent = offsetParent.TransformFromParentSpace(window, new Vector2(120 + parentPoint.X, 90 + parentPoint.Y));
			await Assert.That(fromIntoParent.X).IsEqualTo(parentPoint.X).Within(1e-9);
			await Assert.That(fromIntoParent.Y).IsEqualTo(parentPoint.Y).Within(1e-9);

			var roundTrip = text.TransformFromParentSpace(window, text.TransformToParentSpace(window, textPoint));
			await Assert.That(roundTrip.X).IsEqualTo(textPoint.X).Within(1e-9);
			await Assert.That(roundTrip.Y).IsEqualTo(textPoint.Y).Within(1e-9);

			var rectangle = new RectangleDouble(text.LocalBounds.Left, text.LocalBounds.Bottom, 10, 5);
			var rectangleRoundTrip = text.TransformFromScreenSpace(text.TransformToScreenSpace(rectangle));
			await Assert.That(rectangleRoundTrip.Left).IsEqualTo(rectangle.Left).Within(1e-9);
			await Assert.That(rectangleRoundTrip.Bottom).IsEqualTo(rectangle.Bottom).Within(1e-9);
			await Assert.That(rectangleRoundTrip.Right).IsEqualTo(rectangle.Right).Within(1e-9);
			await Assert.That(rectangleRoundTrip.Top).IsEqualTo(rectangle.Top).Within(1e-9);
		});

		/// <summary>
		/// A dialog shown by a SingleWindowProvider is a client <see cref="SystemWindow"/> inside a movable
		/// <see cref="WindowWidget"/> inside a parentless overlay SystemWindow. "To screen" walks up to the overlay;
		/// "from screen" used to stop at the first SystemWindow above the widget - the client - so it skipped the
		/// WindowWidget's and the client's offsets and a screen point read into the dialog was off by both.
		/// </summary>
		[Test]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public Task FromScreenUndoesToScreenInsideANestedDialog(double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var overlay = new SystemWindow(800, 600);

			var client = new SystemWindow(300, 200);
			var movable = new WindowWidget(new ThemeConfig(), client);
			overlay.AddChild(movable);
			movable.Position = new Vector2(170, 130);

			var child = new GuiWidget(60, 30)
			{
				Position = new Vector2(25, 35),
			};
			client.AddChild(child);

			await Assert.That(client.TransformToParentSpace(overlay, Vector2.Zero).Length).IsGreaterThan(0)
				.Because("the case needs the client window away from the screen origin");

			var point = new Vector2(4.5, 7.25);
			var screen = child.TransformToScreenSpace(point);
			await Assert.That(screen.X).IsEqualTo(child.TransformToParentSpace(overlay, point).X).Within(1e-9);
			await Assert.That(screen.Y).IsEqualTo(child.TransformToParentSpace(overlay, point).Y).Within(1e-9);

			var back = child.TransformFromScreenSpace(screen);
			await Assert.That(back.X).IsEqualTo(point.X).Within(1e-9);
			await Assert.That(back.Y).IsEqualTo(point.Y).Within(1e-9);

			var rectangle = new RectangleDouble(1, 2, 13, 17);
			var rectangleBack = child.TransformFromScreenSpace(child.TransformToScreenSpace(rectangle));
			await Assert.That(rectangleBack.Left).IsEqualTo(rectangle.Left).Within(1e-9);
			await Assert.That(rectangleBack.Bottom).IsEqualTo(rectangle.Bottom).Within(1e-9);
			await Assert.That(rectangleBack.Right).IsEqualTo(rectangle.Right).Within(1e-9);
			await Assert.That(rectangleBack.Top).IsEqualTo(rectangle.Top).Within(1e-9);
		});

		/// <summary>
		/// An unbounded coordinate under a shrinking ancestor is infinite, and Affine's full matrix multiply turns
		/// infinity times a zero term into NaN on the other axis. The old per-axis offsets stayed finite; so must
		/// the point helpers.
		/// </summary>
		[Test]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public Task AnInfiniteCoordinateStaysOnItsOwnAxis(double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, _, child) = BuildTree(0.5);

			var to = child.TransformToParentSpace(window, new Vector2(double.PositiveInfinity, 3));
			await Assert.That(to.X).IsEqualTo(double.PositiveInfinity);
			await Assert.That(to.Y).IsEqualTo(DrawnInWindow(0.5, new Vector2(0, 3)).Y).Within(1e-9);

			var toScreen = child.TransformToScreenSpace(new Vector2(7, double.NegativeInfinity));
			await Assert.That(toScreen.X).IsEqualTo(DrawnInWindow(0.5, new Vector2(7, 0)).X).Within(1e-9);
			await Assert.That(toScreen.Y).IsEqualTo(double.NegativeInfinity);

			var from = child.TransformFromParentSpace(window, new Vector2(double.NegativeInfinity, DrawnInWindow(0.5, new Vector2(0, 3)).Y));
			await Assert.That(from.X).IsEqualTo(double.NegativeInfinity);
			await Assert.That(from.Y).IsEqualTo(3).Within(1e-9);

			var fromScreen = child.TransformFromScreenSpace(new Vector2(DrawnInWindow(0.5, new Vector2(7, 0)).X, double.PositiveInfinity));
			await Assert.That(fromScreen.X).IsEqualTo(7).Within(1e-9);
			await Assert.That(fromScreen.Y).IsEqualTo(double.PositiveInfinity);
		});

		/// <summary>
		/// A widget collapsed to zero scale has no inverse, and Affine's inverse returns NaN or infinity. Every point
		/// on a collapsed axis is drawn in the same place, so "from" falls back to removing the translation on that
		/// axis - finite, and what the helpers always returned - while the other axis is still undone exactly.
		/// "To" of an infinite coordinate on a collapsed axis is the collapsed position, not NaN.
		/// </summary>
		[Test]
		[Arguments(1.0)]
		[Arguments(2.0)]
		public Task AZeroScaleStaysFinite(double deviceScale) => AtDeviceScale(deviceScale, async () =>
		{
			var (window, canvas, child) = BuildTree(1);
			canvas.ParentToChildTransform = Affine.NewScaling(0, 2) * Affine.NewTranslation(CanvasPan);

			var from = child.TransformFromParentSpace(window, new Vector2(400, 300));
			await Assert.That(double.IsFinite(from.X)).IsTrue();
			await Assert.That(double.IsFinite(from.Y)).IsTrue();
			var expectedY = (300 - ContainerPosition.Y - CanvasPan.Y) / 2 - GroupPosition.Y - ChildPosition.Y;
			await Assert.That(from.Y).IsEqualTo(expectedY).Within(1e-9);

			var fromScreen = child.TransformFromScreenSpace(new Vector2(400, 300));
			await Assert.That(fromScreen.X).IsEqualTo(from.X);
			await Assert.That(fromScreen.Y).IsEqualTo(from.Y);

			var to = child.TransformToParentSpace(window, new Vector2(double.PositiveInfinity, 5));
			await Assert.That(to.X).IsEqualTo(ContainerPosition.X + CanvasPan.X).Within(1e-9);
			await Assert.That(to.Y).IsEqualTo(DrawnInWindowY(2, 5)).Within(1e-9);

			// A sheared, singular transform (both rows the same) falls back the same way on both axes.
			canvas.ParentToChildTransform = new Affine(1, 1, 1, 1, CanvasPan.X, CanvasPan.Y);
			var fromSingular = child.TransformFromParentSpace(window, new Vector2(400, 300));
			await Assert.That(double.IsFinite(fromSingular.X)).IsTrue();
			await Assert.That(double.IsFinite(fromSingular.Y)).IsTrue();
		});

		private static double DrawnInWindowY(double scaleY, double childY)
		{
			return ContainerPosition.Y + (GroupPosition.Y + ChildPosition.Y + childY) * scaleY + CanvasPan.Y;
		}
	}
}
