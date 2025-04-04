//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
//
// classes rbox_ctrl_impl, rbox_ctrl
//
//----------------------------------------------------------------------------
using System;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.Transform;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg.UI
{
	//------------------------------------------------------------------------
	public class TextWidget : GuiWidget
	{
		public static bool DebugShowSize { get; set; } = false;

		public static bool DoubleBufferDefault = true;

		private Color textColor;

		private Color disabledColor;

		public bool EllipsisIfClipped { get; set; }

		public bool EllipsisActive => this.EllipsisIfClipped && Printer.LocalBounds.Width > LocalBounds.Width;

		public override string ToolTipText
		{
			// Override ToolTipText if empty and EllipsisActive
			get => string.IsNullOrEmpty(base.ToolTipText) && this.EllipsisActive ? this.Text : base.ToolTipText;
			set => base.ToolTipText = value;
		}

		public double PointSize
		{
			get => Printer.TypeFaceStyle.EmSizeInPoints / GuiWidget.DeviceScale;
			set
			{
				Printer.TypeFaceStyle = new StyledTypeFace(Printer.TypeFaceStyle.TypeFace, value * GuiWidget.DeviceScale, Printer.TypeFaceStyle.DoUnderline, Printer.TypeFaceStyle.FlattenCurves);

				if (AutoExpandBoundsToText)
				{
					DoExpandBoundsToText();
				}

				this.Invalidate();
			}
		}

		public TypeFacePrinter Printer { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether to show the text in bold.
		/// This function only works if the TypeFace you are using is LiberationSans. Otherwise it has no effect.
		/// </summary>
		public bool Bold
		{
			get
			{
				if (Printer.TypeFaceStyle.TypeFace == AggContext.DefaultFontBold)
				{
					return true;
				}

				return false;
			}

			set
			{
				if (!value && Printer.TypeFaceStyle.TypeFace == AggContext.DefaultFontBold)
				{
					var typeFaceStyle = new StyledTypeFace(AggContext.DefaultFont, Printer.TypeFaceStyle.EmSizeInPoints, Printer.TypeFaceStyle.DoUnderline);
					Printer = new TypeFacePrinter(Text, typeFaceStyle, justification: Printer.Justification);
					if (AutoExpandBoundsToText)
					{
						DoExpandBoundsToText();
					}
				}
				else if (value && Printer.TypeFaceStyle.TypeFace == AggContext.DefaultFont)
				{
					var typeFaceStyle = new StyledTypeFace(AggContext.DefaultFontBold, Printer.TypeFaceStyle.EmSizeInPoints, Printer.TypeFaceStyle.DoUnderline);
					Printer = new TypeFacePrinter(Text, typeFaceStyle, justification: Printer.Justification);
					if (AutoExpandBoundsToText)
					{
						DoExpandBoundsToText();
					}
				}
			}
		}

		public TextWidget(string text, double x = 0, double y = 0, double pointSize = 12, Justification justification = Justification.Left, Color textColor = default(Color), bool ellipsisIfClipped = true, bool underline = false, Color backgroundColor = default(Color), TypeFace typeFace = null, bool bold = false)
		{
			disabledColor = new Color(textColor, 50);

			Selectable = false;
			DoubleBuffer = DoubleBufferDefault;
			AutoExpandBoundsToText = false;
			EllipsisIfClipped = ellipsisIfClipped;
			OriginRelativeParent = new Vector2(x, y);
			this.textColor = textColor;
			if (this.textColor.Alpha0To255 == 0)
			{
				// we assume it is the default if alpha 0.  Also there is no reason to make a text color of this as it will draw nothing.
				this.textColor = Color.Black;
			}

			if (backgroundColor.Alpha0To255 != 0)
			{
				BackgroundColor = backgroundColor;
			}

			base.Text = text;

			if (typeFace == null)
			{
				typeFace = bold ? AggContext.DefaultFontBold : AggContext.DefaultFont;
			}

			var typeFaceStyle = new StyledTypeFace(typeFace, pointSize * GuiWidget.DeviceScale, underline);
			Printer = new TypeFacePrinter(text, typeFaceStyle, justification: justification);

			if (text != null)
			{
				LocalBounds = Printer.LocalBounds;

				MinimumSize = new Vector2(0, LocalBounds.Height);
			}
		}

		public override RectangleDouble LocalBounds
		{
			get => base.LocalBounds;
			set
			{
				if (value != LocalBounds)
				{
					if (AutoExpandBoundsToText)
					{
						RectangleDouble textBoundsWithPadding = Printer.LocalBounds;
						textBoundsWithPadding.Inflate(Padding);
						MinimumSize = new Vector2(textBoundsWithPadding.Width, textBoundsWithPadding.Height);
						base.LocalBounds = textBoundsWithPadding;
					}
					else
					{
						base.LocalBounds = value;
					}
				}
			}
		}

		public bool StrikeThrough { get; set; }

		private bool _underline = false;

		public bool Underline
		{
			get => _underline;
			set
			{
				if (_underline != value)
				{
					_underline = value;
					this.Invalidate();
				}
			}
		}

		public override BorderDouble Padding
		{
			get => base.Padding;
			set
			{
				if (Padding != value)
				{
					base.Padding = value;
					if (AutoExpandBoundsToText)
					{
						LocalBounds = LocalBounds;
					}
				}
			}
		}

		public bool AutoExpandBoundsToText { get; set; }

		public void DoExpandBoundsToText()
		{
			Invalidate(); // do it before and after in case it changes size.
			LocalBounds = Printer.LocalBounds;
			if (Text == "" || LocalBounds.Width < 1)
			{
				Printer.Text = " ";
				LocalBounds = Printer.LocalBounds;
				Printer.Text = "";
			}

			Invalidate();
		}

		public override string Text
		{
			get => base.Text;
			set
			{
				string convertedText = value;
				if (value != null)
				{
					convertedText = value.Replace("\r\n", "\n");
					convertedText = convertedText.Replace('\r', '\n');
					if (convertedText.Contains("\r"))
					{
						throw new Exception("These should have be converted to \n.");
					}
				}

				if (base.Text != convertedText)
				{
					base.Text = convertedText;
					bool wasUsingHintedCache = Printer.DrawFromHintedCache;
					// Text may have been changed by a call back be sure to use what we really have set
					Printer = new TypeFacePrinter(base.Text, Printer.TypeFaceStyle, justification: Printer.Justification)
					{
						DrawFromHintedCache = wasUsingHintedCache
					};

					if (AutoExpandBoundsToText)
					{
						DoExpandBoundsToText();
					}

					Invalidate();
				}
			}
		}

		private readonly char[] spaceTrim = { ' ' };

        public override void OnDraw(Graphics2D graphics2D)
		{
			if (!onloadInvoked)
			{
				// Set onloadInvoked before invoking OnLoad to ensure we only fire once
				onloadInvoked = true;

				this.OnLoad(null);
			}

			OnBeforeDraw(graphics2D);

			graphics2D.PushTransform();

			int numLines = Text.Split('\n').Length - 1;
			if (Text.Contains("\r"))
			{
				Text = Text.Trim();
                Text = Text.Replace("\r\n", "\n");
                Text = Text.Replace("\n\r", "\n");
            }

			double yOffsetForText = Printer.TypeFaceStyle.EmSizeInPixels * numLines;
			double xOffsetForText = 0;
			switch (Printer.Justification)
			{
				case Justification.Left:
					break;

				case Justification.Center:
					xOffsetForText = (Width - Printer.LocalBounds.Width) / 2;
					break;

				case Justification.Right:
					xOffsetForText = Width - Printer.LocalBounds.Width;
					break;

				default:
					throw new NotImplementedException();
			}

			graphics2D.SetTransform(graphics2D.GetTransform() * Affine.NewTranslation(xOffsetForText, yOffsetForText));

			if (this.EllipsisActive) // only do this if it's static text
			{
				TypeFacePrinter shortTextPrinter = Printer;
				shortTextPrinter.DrawFromHintedCache = Printer.DrawFromHintedCache;
				while (shortTextPrinter.LocalBounds.Width > LocalBounds.Width && shortTextPrinter.Text.Length > 4)
				{
					shortTextPrinter = new TypeFacePrinter(shortTextPrinter.Text.Substring(0, shortTextPrinter.Text.Length - 4).TrimEnd(spaceTrim) + "...", Printer);
				}

				shortTextPrinter.Render(graphics2D, this.TextColor);
			}
			else
			{
				if (this.StrikeThrough)
				{
					var bounds = Printer.LocalBounds;
					var center = bounds.Center.Y;
					graphics2D.Line(bounds.Left, center, bounds.Right, center, this.TextColor);
				}

				if (this.Underline)
				{
					var bounds = this.LocalBounds;
					var bottom = Math.Round(Printer.LocalBounds.Bottom) + .5;
					graphics2D.Line(bounds.Left, bottom, bounds.Right, bottom, new Color(this.TextColor, 200));
				}

				// it all fits or it's editable (if editable it will need to be offset/scrolled sometimes).
				Printer.Render(graphics2D, this.TextColor);
			}

			// Debug on-screen fonts
			if (DebugShowSize && this.Text.Trim().Length > 0)
			{
				graphics2D.FillRectangle(this.Width - 12, this.Height - 13, this.Width, this.Height, new Color(Color.White, 100));
				graphics2D.DrawString(this.PointSize.ToString(), this.Width - 10, this.Height - 11, 7, color: Color.Black);
				graphics2D.DrawString(this.PointSize.ToString(), this.Width - 11, this.Height - 12, 7, color: Color.Red);
			}

			graphics2D.PopTransform();

			OnAfterDraw(graphics2D);

			if (DebugShowBounds)
			{
				ShowDebugBounds(graphics2D);
			}
		}

		public override void OnEnabledChanged(EventArgs e)
		{
			this.Invalidate();
			base.OnEnabledChanged(e);
		}

		public Color TextColor
		{
			get => this.Enabled ? textColor : this.disabledColor;
			set
			{
				if (textColor != value)
				{
					textColor = value;
					disabledColor = new Color(textColor, 50);

					this.Invalidate();
				}
			}
		}
	}
}
