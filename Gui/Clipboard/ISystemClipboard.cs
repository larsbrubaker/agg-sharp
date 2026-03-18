using MatterHackers.Agg.Image;
using System.Collections.Specialized;

namespace MatterHackers.Agg.UI
{
	public interface ISystemClipboard
	{
		bool ContainsFileDropList { get; }

		bool ContainsHtml { get; }

		bool ContainsImage { get; }

		bool ContainsText { get; }

		StringCollection GetFileDropList();

		ImageBuffer GetImage();

		string GetHtml();

		string GetText();

		void SetText(string text);

		void SetTextAndHtml(string text, string html);

		void SetImage(ImageBuffer imageBuffer);
	}
}