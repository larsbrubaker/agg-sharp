/*
Copyright (c) 2013, Lars Brubaker
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

using MatterHackers.Agg.Font;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using System.Collections.Generic;
using System;
using System.IO;
using NRasterizer;

namespace MatterHackers.Agg
{
	public class OpenTypeTypeFace : TypeFace
	{
		OpenTypeReader typeReader;

		public OpenTypeTypeFace()
		{
			typeReader = new OpenTypeReader();
		}
	}

	public class NRasterizerWidget : GuiWidget
	{
		string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		OpenTypeTypeFace openTypeTypeFace;

		public NRasterizerWidget()
		{
			AnchorAll();

			string font = "CompositeMS.ttf";
			font = "segoesc.ttf";
			string fontPath = Path.Combine("C:", "Development", "NRasterizer", "Fonts", font);

			openTypeTypeFace = new OpenTypeTypeFace();
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			var openTypeStyliedTypeFace = new StyledTypeFace(openTypeTypeFace, 12);
			//var openTypePrinter = new TypeFacePrinter(alphabet, StyledTypeFace typeFaceStyle, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)

			double textY = 200;

			base.OnDraw(graphics2D);

			graphics2D.DrawString(alphabet, 20, textY);
			graphics2D.DrawString(alphabet.ToLower(), 310, textY);
		}
	}
}