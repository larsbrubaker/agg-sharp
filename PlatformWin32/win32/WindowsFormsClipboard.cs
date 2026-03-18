using MatterHackers.Agg.Image;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace MatterHackers.Agg.UI
{
	public class WindowsFormsClipboard : ISystemClipboard
	{
		private const string HtmlHeaderTemplate =
			"Version:0.9\r\n" +
			"StartHTML:{0:0000000000}\r\n" +
			"EndHTML:{1:0000000000}\r\n" +
			"StartFragment:{2:0000000000}\r\n" +
			"EndFragment:{3:0000000000}\r\n";

		public string GetText()
		{
			try
			{
				return System.Windows.Forms.Clipboard.GetText();
			}
			catch
			{
				return "Clipboard Failed";
			}
		}

		public string GetHtml()
		{
			try
			{
				return System.Windows.Forms.Clipboard.ContainsText(System.Windows.Forms.TextDataFormat.Html)
					? System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.Html)
					: string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		public void SetText(string text)
		{
			try
			{
				if (UiThread.IsUiThread)
				{
					System.Windows.Forms.Clipboard.SetText(text);
				}
				else
				{
					UiThread.RunOnIdle(() => System.Windows.Forms.Clipboard.SetText(text));
				}
			}
			catch
			{
			}
		}

		public void SetTextAndHtml(string text, string html)
		{
			try
			{
				var dataObject = new System.Windows.Forms.DataObject();
				dataObject.SetText(text ?? string.Empty, System.Windows.Forms.TextDataFormat.UnicodeText);

				if (!string.IsNullOrWhiteSpace(html))
				{
					dataObject.SetText(ToClipboardHtmlFragment(html), System.Windows.Forms.TextDataFormat.Html);
				}

				SetDataObject(dataObject);
			}
			catch
			{
			}
		}

		public bool ContainsText
		{
			get
			{
				try
				{
					return System.Windows.Forms.Clipboard.ContainsText();
				}
				catch
				{
					return false;
				}
			}
		}

		public bool ContainsHtml
		{
			get
			{
				try
				{
					return System.Windows.Forms.Clipboard.ContainsText(System.Windows.Forms.TextDataFormat.Html);
				}
				catch
				{
					return false;
				}
			}
		}

		public bool ContainsImage
		{
			get
			{
				try
				{
					return System.Windows.Forms.Clipboard.ContainsImage();
				}
				catch
				{
					return false;
				}
			}
		}

		private static void Copy8BitDataToImage(ImageBuffer destImage, Bitmap bitmap)
		{
			destImage.Allocate(bitmap.Width, bitmap.Height, bitmap.Width * 4, 32);
			if (destImage.GetRecieveBlender() == null)
			{
				destImage.SetRecieveBlender(new BlenderBGRA());
			}

			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
			int sourceIndex = 0;
			int destIndex = 0;
			unsafe
			{
				byte[] destBuffer = destImage.GetBuffer(out int offset);
				byte* pSourceBuffer = (byte*)bitmapData.Scan0;

				System.Drawing.Color[] colors = bitmap.Palette.Entries;

				for (int y = 0; y < destImage.Height; y++)
				{
					sourceIndex = y * bitmapData.Stride;
					destIndex = destImage.GetBufferOffsetY(destImage.Height - 1 - y);
					for (int x = 0; x < destImage.Width; x++)
					{
						System.Drawing.Color color = colors[pSourceBuffer[sourceIndex++]];
						destBuffer[destIndex++] = color.B;
						destBuffer[destIndex++] = color.G;
						destBuffer[destIndex++] = color.R;
						destBuffer[destIndex++] = color.A;
					}
				}
			}

			bitmap.UnlockBits(bitmapData);
		}

		public static bool ConvertBitmapToImage(ImageBuffer destImage, Bitmap bitmap)
		{
			if (bitmap != null)
			{
				switch (bitmap.PixelFormat)
				{
					case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
						{
							destImage.Allocate(bitmap.Width, bitmap.Height, bitmap.Width * 4, 32);
							if (destImage.GetRecieveBlender() == null)
							{
								destImage.SetRecieveBlender(new BlenderBGRA());
							}

							BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
							int sourceIndex = 0;
							int destIndex = 0;
							unsafe
							{
								byte[] destBuffer = destImage.GetBuffer(out int offset);
								byte* pSourceBuffer = (byte*)bitmapData.Scan0;
								for (int y = 0; y < destImage.Height; y++)
								{
									destIndex = destImage.GetBufferOffsetXY(0, destImage.Height - 1 - y);
									for (int x = 0; x < destImage.Width; x++)
									{
#if true
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
#else
                                            Color notPreMultiplied = new Color(pSourceBuffer[sourceIndex + 0], pSourceBuffer[sourceIndex + 1], pSourceBuffer[sourceIndex + 2], pSourceBuffer[sourceIndex + 3]);
                                            sourceIndex += 4;
                                            Color preMultiplied = notPreMultiplied.ToColorF().premultiply().ToColor();
                                            destBuffer[destIndex++] = preMultiplied.blue;
                                            destBuffer[destIndex++] = preMultiplied.green;
                                            destBuffer[destIndex++] = preMultiplied.red;
                                            destBuffer[destIndex++] = preMultiplied.alpha;
#endif
									}
								}
							}

							bitmap.UnlockBits(bitmapData);

							return true;
						}

					case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
						{
							destImage.Allocate(bitmap.Width, bitmap.Height, bitmap.Width * 4, 32);
							if (destImage.GetRecieveBlender() == null)
							{
								destImage.SetRecieveBlender(new BlenderBGRA());
							}

							BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
							int sourceIndex = 0;
							int destIndex = 0;
							unsafe
							{
								byte[] destBuffer = destImage.GetBuffer(out int offset);
								byte* pSourceBuffer = (byte*)bitmapData.Scan0;
								for (int y = 0; y < destImage.Height; y++)
								{
									sourceIndex = y * bitmapData.Stride;
									destIndex = destImage.GetBufferOffsetXY(0, destImage.Height - 1 - y);
									for (int x = 0; x < destImage.Width; x++)
									{
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = pSourceBuffer[sourceIndex++];
										destBuffer[destIndex++] = 255;
									}
								}
							}

							bitmap.UnlockBits(bitmapData);
							return true;
						}

					case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
						{
							Copy8BitDataToImage(destImage, bitmap);
							return true;
						}

					default:
						// let this code fall through and return false
						break;
				}
			}

			return false;
		}

		public ImageBuffer GetImage()
		{
			try
			{
				var bitmap = new Bitmap(System.Windows.Forms.Clipboard.GetImage());
				var image = new ImageBuffer();
				if (ConvertBitmapToImage(image, bitmap))
				{
					return image;
				}
			}
			catch
			{
			}

			return null;
		}

		public static Bitmap ConvertImageToBitmap(ImageBuffer sourceImage)
		{
			var bitmap = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

			int destIndex = 0;
			unsafe
			{
				byte[] sourceBuffer = sourceImage.GetBuffer();
				byte* pDestBuffer = (byte*)bitmapData.Scan0;
				int scanlinePadding = bitmapData.Stride - bitmapData.Width * 4;
				for (int y = 0; y < sourceImage.Height; y++)
				{
					int sourceIndex = sourceImage.GetBufferOffsetXY(0, sourceImage.Height - 1 - y);
					for (int x = 0; x < sourceImage.Width; x++)
					{
						pDestBuffer[destIndex++] = sourceBuffer[sourceIndex++];
						pDestBuffer[destIndex++] = sourceBuffer[sourceIndex++];
						pDestBuffer[destIndex++] = sourceBuffer[sourceIndex++];
						pDestBuffer[destIndex++] = sourceBuffer[sourceIndex++];
					}

					destIndex += scanlinePadding;
				}
			}

			bitmap.UnlockBits(bitmapData);

			return bitmap;
		}

		public void SetImage(ImageBuffer imageBuffer)
		{
			System.Windows.Forms.Clipboard.SetImage(ConvertImageToBitmap(imageBuffer));
		}

		public bool ContainsFileDropList => System.Windows.Forms.Clipboard.ContainsFileDropList();

		public StringCollection GetFileDropList()
		{
			return System.Windows.Forms.Clipboard.GetFileDropList();
		}

		private static void SetDataObject(System.Windows.Forms.IDataObject dataObject)
		{
			if (UiThread.IsUiThread)
			{
				System.Windows.Forms.Clipboard.SetDataObject(dataObject, true);
			}
			else
			{
				UiThread.RunOnIdle(() => System.Windows.Forms.Clipboard.SetDataObject(dataObject, true));
			}
		}

		private static string ToClipboardHtmlFragment(string html)
		{
			const string prefix = "<html><body><!--StartFragment-->";
			const string suffix = "<!--EndFragment--></body></html>";
			var fragment = html ?? string.Empty;
			var initialHeader = string.Format(HtmlHeaderTemplate, 0, 0, 0, 0);
			var fullHtml = prefix + fragment + suffix;

			var startHtml = Encoding.UTF8.GetByteCount(initialHeader);
			var startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
			var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
			var endHtml = startHtml + Encoding.UTF8.GetByteCount(fullHtml);
			var finalHeader = string.Format(HtmlHeaderTemplate, startHtml, endHtml, startFragment, endFragment);

			return finalHeader + fullHtml;
		}
	}
}