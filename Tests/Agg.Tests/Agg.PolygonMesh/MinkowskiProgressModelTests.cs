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

using System.Threading.Tasks;
using MatterHackers.PolygonMesh.Csg;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.PolygonMesh.UnitTests
{
	public class MinkowskiProgressModelTests
	{
		[Test]
		public async Task TheTimeFractionRisesThroughEveryKernelUnitFromZeroToOne()
		{
			const int triangles = 2500;
			long total = MinkowskiProgressModel.KernelUnits(triangles);
			await Assert.That(total).IsEqualTo(2500 + 3 + 1);

			double previous = -1;
			for (long unit = 0; unit <= total; unit++)
			{
				double fraction = MinkowskiProgressModel.TimeFraction(triangles, unit / (double)total);
				await Assert.That(fraction).IsGreaterThanOrEqualTo(previous);
				previous = fraction;
			}

			await Assert.That(MinkowskiProgressModel.TimeFraction(triangles, 0)).IsEqualTo(0.0);
			await Assert.That(MinkowskiProgressModel.TimeFraction(triangles, 1)).IsEqualTo(1.0);
		}

		[Test]
		public async Task HullsAreCheapAndBatchUnionsCarryTheTime()
		{
			const int triangles = 3000;
			long total = MinkowskiProgressModel.KernelUnits(triangles);

			// Every hull of the first batch done, its union not: the bar has barely moved,
			// because the union that follows is where the batch's time goes.
			double hullsDone = MinkowskiProgressModel.TimeFraction(triangles, 1000 / (double)total);
			await Assert.That(hullsDone).IsLessThan(0.05);

			// One of three batches unioned: close to a third of the time, less its share of the merge.
			double oneBatch = MinkowskiProgressModel.TimeFraction(triangles, 1001 / (double)total);
			await Assert.That(oneBatch).IsEqualTo(0.3).Within(0.001);

			// Every batch unioned, the closing merge still to run: its share is left.
			double allBatches = MinkowskiProgressModel.TimeFraction(triangles, (total - 1) / (double)total);
			await Assert.That(allBatches).IsEqualTo(0.9).Within(0.001);
		}
	}
}
