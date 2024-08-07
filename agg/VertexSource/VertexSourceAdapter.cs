using MatterHackers.VectorMath;

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
using System.Collections.Generic;

namespace MatterHackers.Agg.VertexSource
{
	//------------------------------------------------------------null_markers
	public struct null_markers : IMarkers
	{
		public void Clear()
		{
		}

		public void add_vertex(double x, double y, FlagsAndCommand unknown)
		{
		}

		public void prepare_src()
		{
		}

		public void rewind(int unknown)
		{
		}

		public FlagsAndCommand vertex(ref double x, ref double y)
		{
			return FlagsAndCommand.Stop;
		}
	};

	//------------------------------------------------------conv_adaptor_vcgen
	public class VertexSourceAdapter : IVertexSourceProxy
	{
		private IGenerator generator;
		private IMarkers markers;
		private status m_status;
		private FlagsAndCommand m_last_cmd;
		private double m_start_x;
		private double m_start_y;

		public IVertexSource VertexSource { get; set; }

		private enum status
		{
			initial,
			accumulate,
			generate
		};

		public VertexSourceAdapter(IVertexSource vertexSource, IGenerator generator)
		{
			markers = new null_markers();
			this.VertexSource = vertexSource;
			this.generator = generator;
			m_status = status.initial;
		}

        public ulong GetLongHashCode(ulong hash = 14695981039346656037)
        {
            foreach (var vertex in this.Vertices())
            {
                hash = vertex.GetLongHashCode(hash);
            }

            return hash;
        }

        public VertexSourceAdapter(IVertexSource vertexSource, IGenerator generator, IMarkers markers)
			: this(vertexSource, generator)
		{
			this.markers = markers;
		}

		private void Attach(IVertexSource vertexSource)
		{
			this.VertexSource = vertexSource;
		}

		protected IGenerator Generator => generator;

		protected IMarkers Markers => markers;

		public IEnumerable<VertexData> Vertices()
		{
			Rewind(0);
			FlagsAndCommand command = FlagsAndCommand.Stop;
			do
			{
				double x;
				double y;
				command = Vertex(out x, out y);
				yield return new VertexData(command, new Vector2(x, y));
			} while (command != FlagsAndCommand.Stop);
		}

		public void Rewind(int path_id)
		{
			VertexSource.Rewind(path_id);
			m_status = status.initial;
		}

		public FlagsAndCommand Vertex(out double x, out double y)
		{
			x = 0;
			y = 0;
			FlagsAndCommand command = FlagsAndCommand.Stop;
			bool done = false;
			while (!done)
			{
				switch (m_status)
				{
					case status.initial:
						markers.Clear();
						m_last_cmd = VertexSource.Vertex(out m_start_x, out m_start_y);
						m_status = status.accumulate;
						goto case status.accumulate;

					case status.accumulate:
						if (ShapePath.IsStop(m_last_cmd))
						{
							return FlagsAndCommand.Stop;
						}

						generator.RemoveAll();
						generator.AddVertex(m_start_x, m_start_y, FlagsAndCommand.MoveTo);
						markers.add_vertex(m_start_x, m_start_y, FlagsAndCommand.MoveTo);

						for (; ; )
						{
							command = VertexSource.Vertex(out x, out y);
							//DebugFile.Print("x=" + x.ToString() + " y=" + y.ToString() + "\n");
							if (ShapePath.IsVertex(command))
							{
								m_last_cmd = command;
								if (ShapePath.IsMoveTo(command))
								{
									m_start_x = x;
									m_start_y = y;
									break;
								}
								generator.AddVertex(x, y, command);
								markers.add_vertex(x, y, FlagsAndCommand.LineTo);
							}
							else
							{
								if (ShapePath.IsStop(command))
								{
									m_last_cmd = FlagsAndCommand.Stop;
									break;
								}
								if (ShapePath.is_end_poly(command))
								{
									generator.AddVertex(x, y, command);
									break;
								}
							}
						}
						generator.Rewind(0);
						m_status = status.generate;
						goto case status.generate;

					case status.generate:
						command = generator.Vertex(ref x, ref y);
						//DebugFile.Print("x=" + x.ToString() + " y=" + y.ToString() + "\n");
						if (ShapePath.IsStop(command))
						{
							m_status = status.accumulate;
							break;
						}
						done = true;
						break;
				}
			}
			return command;
		}
	}
}