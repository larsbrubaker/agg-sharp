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
// classes conv_curve
//
//----------------------------------------------------------------------------
using System.Collections.Generic;

namespace MatterHackers.Agg.VertexSource
{
	//---------------------------------------------------------------conv_curve
	// Curve converter class. Any path storage can have Bezier curves defined
	// by their control points. There are two types of curves supported: curve3
	// and curve4. Curve3 is a conic Bezier curve with 2 endpoints and 1 control
	// point. Curve4 has 2 control points (4 points in total) and can be used
	// to interpolate more complicated curves. Curve4, unlike curve3 can be used
	// to approximate arcs, both circular and elliptical. Curves are approximated
	// with straight lines and one of the approaches is just to store the whole
	// sequence of vertices that approximate our curve. It takes additional
	// memory, and at the same time the consecutive vertices can be calculated
	// on demand.
	//
	// Initially, path storages are not suppose to keep all the vertices of the
	// curves (although, nothing prevents us from doing so). Instead, path_storage
	// keeps only vertices, needed to calculate a curve on demand. Those vertices
	// are marked with special commands. So, if the path_storage contains curves
	// (which are not real curves yet), and we render this storage directly,
	// all we will see is only 2 or 3 straight line segments (for curve3 and
	// curve4 respectively). If we need to see real curves drawn we need to
	// include this class into the conversion pipeline.
	//
	// Class conv_curve recognizes commands path_cmd_curve3 and path_cmd_curve4
	// and converts these vertices into a move_to/line_to sequence.
	//-----------------------------------------------------------------------
	public class FlattenCurves : VertexSourceLegacySupport
	{
		//private double lastX;
		//private double lastY;
		private Curve3 m_curve3;
		private Curve4 m_curve4;

		public IVertexSource VertexSource
		{
			get;
			set;
		}

		public FlattenCurves(IVertexSource vertexSource)
		{
			m_curve3 = new Curve3();
			m_curve4 = new Curve4();
			VertexSource = vertexSource;
			//lastX = (0.0);
			//lastY = (0.0);
		}

		public double ResolutionScale
		{
			get
			{
				return m_curve4.approximation_scale();
			}

			set
			{
				m_curve3.approximation_scale(value);
				m_curve4.approximation_scale(value);
			}
		}

		public void SetVertexSource(IVertexSource vertexSource)
		{
			VertexSource = vertexSource;
		}

		public Curves.CurveApproximationMethod ApproximationMethod
		{
			set
			{
				m_curve3.approximation_method(value);
				m_curve4.approximation_method(value);
			}

			get
			{
				return m_curve4.approximation_method();
			}
		}

		public double AngleTolerance
		{
			set
			{
				m_curve3.angle_tolerance(value);
				m_curve4.angle_tolerance(value);
			}

			get
			{
				return m_curve4.angle_tolerance();
			}
		}

		public double CuspLimit
		{
			set
			{
				m_curve3.cusp_limit(value);
				m_curve4.cusp_limit(value);
			}

			get
			{
				return m_curve4.cusp_limit();
			}
		}

		public override IEnumerable<VertexData> Vertices()
		{
			if (VertexSource == null)
			{
				yield break;
			}

			VertexData lastPosition = new VertexData();

			IEnumerator<VertexData> vertexDataEnumerator = VertexSource.Vertices().GetEnumerator();
			while (vertexDataEnumerator.MoveNext())
			{
				VertexData vertexData = vertexDataEnumerator.Current;
				switch (vertexData.Command)
				{
					case FlagsAndCommand.Curve3:
						{
							vertexDataEnumerator.MoveNext();
							VertexData vertexDataEnd = vertexDataEnumerator.Current;
							m_curve3.init(lastPosition.Position.X, lastPosition.Position.Y, vertexData.Position.X, vertexData.Position.Y, vertexDataEnd.Position.X, vertexDataEnd.Position.Y);
							IEnumerator<VertexData> curveIterator = m_curve3.Vertices().GetEnumerator();
							curveIterator.MoveNext(); // First call returns path_cmd_move_to
							do
							{
								curveIterator.MoveNext();
								if (ShapePath.IsStop(curveIterator.Current.Command))
								{
									break;
								}
								vertexData = new VertexData(FlagsAndCommand.LineTo, curveIterator.Current.Position);
								yield return vertexData;
								lastPosition = vertexData;
							} while (!ShapePath.IsStop(curveIterator.Current.Command));
						}
						break;

					case FlagsAndCommand.Curve4:
						{
							vertexDataEnumerator.MoveNext();
							var vertexDataControl2 = vertexDataEnumerator.Current;
							vertexDataEnumerator.MoveNext();
							var vertexDataEnd = vertexDataEnumerator.Current;
							m_curve4.init(lastPosition.Position.X, lastPosition.Position.Y,
								vertexData.Position.X, vertexData.Position.Y,
								vertexDataControl2.Position.X, vertexDataControl2.Position.Y,
								vertexDataEnd.Position.X, vertexDataEnd.Position.Y);
							var curveIterator = m_curve4.Vertices().GetEnumerator();
							curveIterator.MoveNext(); // First call returns path_cmd_move_to
							while (!ShapePath.IsStop(vertexData.Command))
							{
								curveIterator.MoveNext();
								if (ShapePath.IsStop(curveIterator.Current.Command))
								{
									break;
								}
								vertexData = new VertexData(FlagsAndCommand.LineTo, curveIterator.Current.Position);
								yield return vertexData;
								lastPosition = vertexData;
							}
						}
						break;

					default:
						yield return vertexData;
						lastPosition = vertexData;
						break;
				}
			}
		}
	}
}