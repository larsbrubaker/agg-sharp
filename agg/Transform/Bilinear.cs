//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
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
// Bilinear 2D transformations
//
//----------------------------------------------------------------------------

namespace MatterHackers.Agg.Transform
{
	//==========================================================trans_bilinear
	public sealed class Bilinear : ITransform
	{
		private double[,] m_mtx = new double[4, 2];
		private bool m_valid;

		//--------------------------------------------------------------------
		public Bilinear()
		{
			m_valid = (false);
		}

		//--------------------------------------------------------------------
		// Arbitrary quadrangle transformations
		public Bilinear(double[] src, double[] dst)
		{
			quad_to_quad(src, dst);
		}

		//--------------------------------------------------------------------
		// Direct transformations
		public Bilinear(double x1, double y1, double x2, double y2, double[] quad)
		{
			rect_to_quad(x1, y1, x2, y2, quad);
		}

		//--------------------------------------------------------------------
		// Reverse transformations
		public Bilinear(double[] quad,
					   double x1, double y1, double x2, double y2)
		{
			quad_to_rect(quad, x1, y1, x2, y2);
		}

		//--------------------------------------------------------------------
		// Set the transformations using two arbitrary quadrangles.
		public void quad_to_quad(double[] src, double[] dst)
		{
			double[,] left = new double[4, 4];
			double[,] right = new double[4, 2];

			uint i;
			for (i = 0; i < 4; i++)
			{
				uint ix = i * 2;
				uint iy = ix + 1;
				left[i, 0] = 1.0;
				left[i, 1] = src[ix] * src[iy];
				left[i, 2] = src[ix];
				left[i, 3] = src[iy];

				right[i, 0] = dst[ix];
				right[i, 1] = dst[iy];
			}
			m_valid = simul_eq.solve(left, right, m_mtx);
		}

		//--------------------------------------------------------------------
		// Set the direct transformations, i.e., rectangle -> quadrangle
		public void rect_to_quad(double x1, double y1, double x2, double y2,
						  double[] quad)
		{
			double[] src = new double[8];
			src[0] = src[6] = x1;
			src[2] = src[4] = x2;
			src[1] = src[3] = y1;
			src[5] = src[7] = y2;
			quad_to_quad(src, quad);
		}

		//--------------------------------------------------------------------
		// Set the reverse transformations, i.e., quadrangle -> rectangle
		public void quad_to_rect(double[] quad,
						  double x1, double y1, double x2, double y2)
		{
			double[] dst = new double[8];
			dst[0] = dst[6] = x1;
			dst[2] = dst[4] = x2;
			dst[1] = dst[3] = y1;
			dst[5] = dst[7] = y2;
			quad_to_quad(quad, dst);
		}

		//--------------------------------------------------------------------
		// Check if the equations were solved successfully
		public bool is_valid()
		{
			return m_valid;
		}

		//--------------------------------------------------------------------
		// Transform a point (x, y)
		public void Transform(ref double x, ref double y)
		{
			double tx = x;
			double ty = y;
			double xy = tx * ty;
			x = m_mtx[0, 0] + m_mtx[1, 0] * xy + m_mtx[2, 0] * tx + m_mtx[3, 0] * ty;
			y = m_mtx[0, 1] + m_mtx[1, 1] * xy + m_mtx[2, 1] * tx + m_mtx[3, 1] * ty;
		}

		//--------------------------------------------------------------------
		public sealed class iterator_x
		{
			private double inc_x;
			private double inc_y;

			public double x;
			public double y;

			public iterator_x()
			{
			}

			public iterator_x(double tx, double ty, double step, double[,] m)
			{
				inc_x = (m[1, 0] * step * ty + m[2, 0] * step);
				inc_y = (m[1, 1] * step * ty + m[2, 1] * step);
				x = (m[0, 0] + m[1, 0] * tx * ty + m[2, 0] * tx + m[3, 0] * ty);
				y = (m[0, 1] + m[1, 1] * tx * ty + m[2, 1] * tx + m[3, 1] * ty);
			}

			public static iterator_x operator ++(iterator_x a)
			{
				a.x += a.inc_x;
				a.y += a.inc_y;

				return a;
			}
		};

		public iterator_x begin(double x, double y, double step)
		{
			return new iterator_x(x, y, step, m_mtx);
		}
	};
}