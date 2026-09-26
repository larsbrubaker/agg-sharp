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

namespace MatterHackers.PolygonMesh.Csg
{
	/// <summary>
	/// Turns the kernel's Minkowski progress - which counts work items - into a fraction of the
	/// expected running time, so a bar driven by it moves at a steady rate.
	/// </summary>
	/// <remarks>
	/// The kernel's nonconvex &#8853; convex sweep reports one unit per per-triangle hull, one per
	/// 1000-hull batch union and one for the closing merge, all weighted the same. They are not the
	/// same cost. Measured in Release on a sphere-minus-cube dilated by a 12-segment ball (730 to
	/// 7722 triangles): the hulls are about 5% of the run, the batch unions about 85% and the
	/// closing merge the rest. Counted as units the bar raced to the end of each batch's hulls and
	/// then sat through its union, and sat again at the very end through the merge. Weighted by
	/// those measured costs the bar tracks the clock to within the batch granularity: it still
	/// holds during a union (the kernel reports nothing inside one) but no longer lies about how
	/// much is left.
	/// <para>
	/// The unit layout mirrors <c>ManifoldSharp.Minkowski.WorkUnits</c>; if the kernel's batch
	/// size or reporting changes, this has to follow it.
	/// </para>
	/// </remarks>
	public static class MinkowskiProgressModel
	{
		/// <summary>The kernel's batch size (Minkowski.BatchSize).</summary>
		public const int BatchSize = 1000;

		// Relative cost per triangle of each piece of the sweep, from the Release measurements above.
		private const double HullWeight = 1;
		private const double BatchUnionWeight = 17;
		private const double MergeWeight = 2;

		/// <summary>
		/// The number of progress units the kernel's sweep reports for a solid of
		/// <paramref name="triangles"/> triangles: a hull each, a union per batch and the closing merge.
		/// </summary>
		public static long KernelUnits(int triangles)
		{
			long batches = (triangles + BatchSize - 1L) / BatchSize;
			return triangles + batches + 1;
		}

		/// <summary>
		/// Maps the kernel's unit fraction for a sweep over <paramref name="triangles"/> triangles to
		/// the fraction of the expected running time spent so far. Monotonic, 0 at 0 and 1 at 1.
		/// </summary>
		public static double TimeFraction(int triangles, double kernelFraction)
		{
			if (triangles <= 0 || kernelFraction >= 1)
			{
				return Math.Clamp(kernelFraction, 0, 1);
			}

			if (kernelFraction <= 0)
			{
				return 0;
			}

			// The kernel emits fraction = done / total; rounding recovers the whole unit count it had.
			long units = (long)Math.Round(kernelFraction * KernelUnits(triangles));
			double work = 0;
			for (int start = 0; start < triangles && units > 0; start += BatchSize)
			{
				int size = Math.Min(BatchSize, triangles - start);
				if (units > size)
				{
					// Every hull of this batch and its union are done.
					work += size * (HullWeight + BatchUnionWeight);
					units -= size + 1;
				}
				else
				{
					work += units * HullWeight;
					units = 0;
				}
			}

			if (units > 0)
			{
				work += triangles * MergeWeight;
			}

			return Math.Clamp(work / (triangles * (HullWeight + BatchUnionWeight + MergeWeight)), 0, 1);
		}
	}
}
