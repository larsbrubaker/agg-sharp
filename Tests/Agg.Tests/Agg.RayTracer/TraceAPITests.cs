﻿/*
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

using Agg.Tests.Agg;
using MatterHackers.PolygonMesh.Processors;
using MatterHackers.RayTracer.Traceable;
using MatterHackers.VectorMath;
using System.Collections.Generic;
using System.Linq;

namespace MatterHackers.RayTracer
{
	[MhTestFixture("Agg.RayTracer")]
	public class TraceAPITests
	{
		[MhTest]
		public void EnumerateBvh()
		{
			// create a bvh hierarchy
			var level4_a = new TriangleShape(new Vector3(0, 0, 1), new Vector3(0, 0, 3), new Vector3(0, 1, 2), null);
			var level4_b = new TriangleShape(new Vector3(0, 0, 11), new Vector3(0, 0, 13), new Vector3(0, 1, 12), null);
			var level4_c = new TriangleShape(new Vector3(3, 0, 1), new Vector3(3, 0, 3), new Vector3(3, 1, 2), null);
			var level3_a = new UnboundCollection(new List<ITraceable>() { level4_a, level4_b, level4_c });
			var level3_b = new TriangleShape(new Vector3(43, 0, 1), new Vector3(43, 0, 3), new Vector3(43, 1, 2), null);
			var level2_a = new Transform(level3_a, Matrix4X4.CreateTranslation(0, 0, 40));
			var level2_b = new Transform(level3_b, Matrix4X4.CreateTranslation(0, 40, 0));
			var level1 = new UnboundCollection(new List<ITraceable>() { level2_a, level2_b });
			var root = new Transform(level1);

			// enumerate it and check it
			MhAssert.Equal(9, new BvhIterator(root).Count());

			int count = 0;
			foreach(var item in new BvhIterator(root))
			{
				switch(count++)
				{
					case 0:
						MhAssert.True(item.Bvh is Transform);
						MhAssert.Equal(0, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 0), item.TransformToWorld);
						break;
					case 1:
						MhAssert.True(item.Bvh is UnboundCollection);
						MhAssert.Equal(1, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0,0,0), item.TransformToWorld);
						break;
					case 2:
						MhAssert.True(item.Bvh is Transform);
						MhAssert.Equal(2, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 0), item.TransformToWorld);
						break;
					case 3:
						MhAssert.True(item.Bvh is UnboundCollection);
						MhAssert.Equal(3, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 40), item.TransformToWorld);
						break;
					case 4:
						MhAssert.True(item.Bvh is TriangleShape);
						MhAssert.Equal(4, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 40), item.TransformToWorld);
						break;
					case 5:
						MhAssert.True(item.Bvh is TriangleShape);
						MhAssert.Equal(4, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 40), item.TransformToWorld);
						break;
					case 6:
						MhAssert.True(item.Bvh is TriangleShape);
						MhAssert.Equal(4, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 40), item.TransformToWorld);
						break;
					case 7:
						MhAssert.True(item.Bvh is Transform);
						MhAssert.Equal(2, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 0, 0), item.TransformToWorld);
						break;
					case 8:
						MhAssert.True(item.Bvh is TriangleShape);
						MhAssert.Equal(3, item.Depth);
						MhAssert.Equal(Matrix4X4.CreateTranslation(0, 40, 0), item.TransformToWorld);
						break;
				}
			}
		}

		[MhTest]
		public void PlaneGetDistanceToIntersection()
		{
			Plane testPlane = new Plane(Vector3.UnitZ, 10);
			bool hitFrontOfPlane;
			double distanceToHit;

			Ray lookingAtFrontOfPlane = new Ray(new Vector3(0, 0, 11), new Vector3(0, 0, -1));
			MhAssert.True(testPlane.RayHitPlane(lookingAtFrontOfPlane, out distanceToHit, out hitFrontOfPlane));
			MhAssert.True(distanceToHit == 1);
			MhAssert.True(hitFrontOfPlane);

			Ray notLookingAtFrontOfPlane = new Ray(new Vector3(0, 0, 11), new Vector3(0, 0, 1));
			MhAssert.True(!testPlane.RayHitPlane(notLookingAtFrontOfPlane, out distanceToHit, out hitFrontOfPlane));
			MhAssert.True(distanceToHit == double.PositiveInfinity);
			MhAssert.True(!hitFrontOfPlane);

			Ray lookingAtBackOfPlane = new Ray(new Vector3(0, 0, 9), new Vector3(0, 0, 1));
			MhAssert.True(testPlane.RayHitPlane(lookingAtBackOfPlane, out distanceToHit, out hitFrontOfPlane));
			MhAssert.True(distanceToHit == 1);
			MhAssert.True(!hitFrontOfPlane);

			Ray notLookingAtBackOfPlane = new Ray(new Vector3(0, 0, 9), new Vector3(0, 0, -1));
			MhAssert.True(!testPlane.RayHitPlane(notLookingAtBackOfPlane, out distanceToHit, out hitFrontOfPlane));
			MhAssert.True(distanceToHit == double.PositiveInfinity);
			MhAssert.True(hitFrontOfPlane);
		}
	}
}