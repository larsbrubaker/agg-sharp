using MatterHackers.Agg.UI;
using MatterHackers.RenderGl.OpenGl;

/*
Copyright (c) 2014, Lars Brubaker
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

namespace MatterHackers.GCodeVisualizer
{
	public class GCodeVertexBuffer : IDisposable
	{
		public int myIndexId;
		public int myIndexLength;
		public BeginMode myMode = BeginMode.Triangles;
		public int myVertexId;
		public int myVertexLength;
		public GCodeVertexBuffer()
		{
			myVertexId = GL.Instance.GenBuffer();
			myIndexId= GL.Instance.GenBuffer();
		}

		public void Dispose()
		{
			if (myVertexId != -1)
			{
				int holdVertexId = myVertexId;
				int holdIndexId = myIndexId;
				UiThread.RunOnIdle(() =>
				{
					GL.Instance.DeleteBuffer(holdVertexId);
					GL.Instance.DeleteBuffer(holdIndexId);
				});

				myVertexId = -1;
			}
		}

		~GCodeVertexBuffer()
		{
			Dispose();
		}

		public void renderRange(int offset, int count)
		{
			GL.Instance.EnableClientState(ArrayCap.ColorArray);
			GL.Instance.EnableClientState(ArrayCap.NormalArray);
			GL.Instance.EnableClientState(ArrayCap.VertexArray);
			GL.Instance.DisableClientState(ArrayCap.TextureCoordArray);
			GL.Instance.Disable(EnableCap.Texture2D);

			GL.Instance.EnableClientState(ArrayCap.IndexArray);

			GL.Instance.BindBuffer(BufferTarget.ArrayBuffer, myVertexId);
			GL.Instance.BindBuffer(BufferTarget.ElementArrayBuffer, myIndexId);

			GL.Instance.ColorPointer(4, ColorPointerType.UnsignedByte, ColorVertexData.Stride, new IntPtr(0));
			GL.Instance.NormalPointer(NormalPointerType.Float, ColorVertexData.Stride, new IntPtr(4));
			GL.Instance.VertexPointer(3, VertexPointerType.Float, ColorVertexData.Stride, new IntPtr(4 + 3 * 4));

			GL.Instance.DrawRangeElements(myMode, 0, myIndexLength, count, DrawElementsType.UnsignedInt, new IntPtr(offset * 4));

			GL.Instance.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.Instance.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			GL.Instance.DisableClientState(ArrayCap.IndexArray);

			GL.Instance.DisableClientState(ArrayCap.VertexArray);
			GL.Instance.DisableClientState(ArrayCap.NormalArray);
			GL.Instance.DisableClientState(ArrayCap.ColorArray);
		}

		public void SetIndexData(int[] data)
		{
			SetIndexData(data, data.Length);
		}

		public void SetIndexData(int[] data, int count)
		{
			myIndexLength = count;
			GL.Instance.BindBuffer(BufferTarget.ElementArrayBuffer, myIndexId);
			unsafe
			{
				fixed (int* dataPointer = data)
				{
					GL.Instance.BufferData(BufferTarget.ElementArrayBuffer, data.Length * sizeof(int), (IntPtr)dataPointer, BufferUsageHint.StaticDraw);
				}
			}
		}

		public void SetVertexData(ColorVertexData[] data)
		{
			SetVertexData(data, data.Length);
		}

		public void SetVertexData(ColorVertexData[] data, int count)
		{
			myVertexLength = count;
			GL.Instance.BindBuffer(BufferTarget.ArrayBuffer, myVertexId);
			unsafe
			{
				fixed (ColorVertexData* dataPointer = data)
				{
					GL.Instance.BufferData(BufferTarget.ArrayBuffer, data.Length * ColorVertexData.Stride, (IntPtr)dataPointer, BufferUsageHint.StaticDraw);
				}
			}
		}
	}
}