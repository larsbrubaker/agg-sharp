/*
Copyright (c) 2023, Lars Brubaker
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

using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;
using Xunit;

namespace MatterHackers.Agg.Tests
{
    public class Vector2Tests
	{
		[Fact]
		public void ArithmaticOperations()
		{
			var point1 = new Vector2(1, 1);

			var point2 = new Vector2(2, 2);

			Vector2 point3 = point1 + point2;
			Assert.True(point3 == new Vector2(3, 3));

			point3 = point1 - point2;
			Assert.True(point3 == new Vector2(-1, -1));

			point3 += point1;
			Assert.True(point3 == new Vector2(0, 0));

			point3 += point2;
			Assert.True(point3 == new Vector2(2, 2));

			point3 *= 6;
			Assert.True(point3 == new Vector2(12, 12));

			var inlineOpLeftSide = new Vector2(5, -3);
			var inlineOpRightSide = new Vector2(-5, 4);
			Assert.True(inlineOpLeftSide + inlineOpRightSide == new Vector2(.0f, 1));

			Assert.True(inlineOpLeftSide - inlineOpRightSide == new Vector2(10.0f, -7));
		}

		[Fact]
		public void GetLengthAndNormalize()
		{
			var point3 = new Vector2(3, -4);
			Assert.True(point3.Length > 4.999f && point3.Length < 5.001f);

			point3.Normalize();
			Assert.True(point3.Length > 0.99f && point3.Length < 1.01f);
		}

		[Fact]
		public void GetPositionAtTests()
		{
			var line1 = new List<Vector2>()
			{
				new Vector2(10, 3),
				new Vector2(20, 3),
				new Vector2(20, 13),
				new Vector2(10, 13)
			};

			Assert.Equal(30, line1.PolygonLength(false));

			// open segments should also give correct values
			Assert.Equal(new Vector2(13, 3), line1.GetPositionAt(3, false));
			Assert.Equal(new Vector2(10, 13), line1.GetPositionAt(33, false)); //, "Open so return the end");
            Assert.Equal(new Vector2(10, 13), line1.GetPositionAt(33 + 22 * 10, false)); //, "Open so return the end");
            Assert.Equal(new Vector2(10, 3), line1.GetPositionAt(-2, false)); //, "Negative so return the start");
            Assert.Equal(new Vector2(10, 3), line1.GetPositionAt(-2 + -23 * 10, false)); //, "Negative so return the start");

            Assert.Equal(40, line1.PolygonLength(true));

			// closed loops should wrap correctly
			var error = .000001;
			Assert.Equal(new Vector2(13, 3), line1.GetPositionAt(3));
			Assert.True(new Vector2(13, 3).Equals(line1.GetPositionAt(43), error), "Closed loop so we should go back to the beginning");
			Assert.True(new Vector2(13, 3).Equals(line1.GetPositionAt(43 + 22 * 40), error), "Closed loop so we should go back to the beginning");
			Assert.True(new Vector2(10, 5).Equals(line1.GetPositionAt(-2), error), "Negative values are still valid");
			Assert.True(new Vector2(10, 5).Equals(line1.GetPositionAt(-2 + 23 * 40), error), "Negative values are still valid");
		}

		[Fact]
		public void ScalerOperations()
		{
			var scalarMultiplicationArgument = new Vector2(5.0f, 4.0f);
			Assert.True(scalarMultiplicationArgument * -.5 == new Vector2(-2.5f, -2));
			Assert.True(scalarMultiplicationArgument / 2 == new Vector2(2.5, 2));
			Assert.True(2 / scalarMultiplicationArgument == new Vector2(.4, .5));
			Assert.True(5 * scalarMultiplicationArgument == new Vector2(25, 20));
		}

		[Fact]
		public void CrossProduct()
		{
			var rand = new Random();
			var testVector2D1 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			var testVector2D2 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			double cross2D = Vector2.Cross(testVector2D1, testVector2D2);

			var testVector31 = new Vector3(testVector2D1.X, testVector2D1.Y, 0);
			var testVector32 = new Vector3(testVector2D2.X, testVector2D2.Y, 0);
			Vector3 cross3D = Vector3Ex.Cross(testVector31, testVector32);

			Assert.True(cross3D.Z == cross2D);
		}

		[Fact]
		public void DotProduct()
		{
			var rand = new Random();
			var testVector2D1 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			var testVector2D2 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			double cross2D = Vector2.Dot(testVector2D1, testVector2D2);

			var testVector31 = new Vector3(testVector2D1.X, testVector2D1.Y, 0);
			var testVector32 = new Vector3(testVector2D2.X, testVector2D2.Y, 0);
			double cross3D = Vector3Ex.Dot(testVector31, testVector32);

			Assert.True(cross3D == cross2D);
		}

		[Fact]
		public void LengthAndDistance()
		{
			var rand = new Random();
			var test1 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			var test2 = new Vector2(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
			Vector2 test3 = test1 + test2;
			double distance1 = test2.Length;
			double distance2 = (test1 - test3).Length;

			Assert.True(distance1 < distance2 + .001f && distance1 > distance2 - .001f);
		}
	}
}