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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.VectorMath;
using MatterHackers.VectorMath.Bvh;

namespace MatterHackers.PolygonMesh
{
	public class FaceTextureData
	{
		public ImageBuffer image;
		public Vector2Float uv0;
		public Vector2Float uv1;
		public Vector2Float uv2;

		public FaceTextureData(ImageBuffer textureToUse, Vector2Float vector2Float1, Vector2Float vector2Float2, Vector2Float vector2Float3)
		{
			this.image = textureToUse;
			this.uv0 = vector2Float1;
			this.uv1 = vector2Float2;
			this.uv2 = vector2Float3;
		}
	}

	public class Mesh
	{
		// public List<Vector3> Vertices { get; set; } = new List<Vector3>();
		public List<Vector3Float> Vertices { get; set; } = new List<Vector3Float>();

		public FaceList Faces { get; set; } = new FaceList();

		/// <summary>
		/// Gets or sets lookup by face index into the UVs and image for a face.
		/// </summary>
		public Dictionary<int, FaceTextureData> FaceTextures { get; set; } = new Dictionary<int, FaceTextureData>();

		private static object nextIdLocker = new object();

		public BspNode FaceBspTree { get; set; } = null;

		public AxisAlignedBoundingBox cachedAABB = null;

		private TransformedAabbCache transformedAabbCache = new TransformedAabbCache();

		public Dictionary<string, object> PropertyBag = new Dictionary<string, object>();

		public Mesh()
		{
		}

		public Mesh(List<Vector3> v, FaceList f)
		{
			Vertices.Clear();
			Vertices.AddRange(v);

			Faces.Clear();
			Faces.AddRange(f);
		}

		public Mesh(List<Vector3Float> v, FaceList f)
		{
			Vertices.Clear();
			Vertices.AddRange(v);

			Faces.Clear();
			Faces.AddRange(f);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// with a 3xN vertex array and a 3xM vertex index array.
		/// </summary>
		/// <param name="v">a 3xN array of doubles representing vertices</param>
		/// <param name="f">a 3xM array of ints representing face vertex indexes</param>
		public Mesh(double[] v, int[] f)
		{
			for (int vertexIndex = 0; vertexIndex < v.Length - 2; vertexIndex += 3)
			{
				this.Vertices.Add(new Vector3(v[vertexIndex + 0],
					v[vertexIndex + 1],
					v[vertexIndex + 2]));
			}

			for (int faceIndex = 0; faceIndex < f.Length - 2; faceIndex += 3)
			{
				this.Faces.Add(f[faceIndex + 0],
					f[faceIndex + 1],
					f[faceIndex + 2],
					this.Vertices);
			}
		}

		public Mesh(List<Vector3> v, List<int> f)
		{
			for (int vertexIndex = 0; vertexIndex < v.Count; vertexIndex++)
			{
				this.Vertices.Add(new Vector3Float(v[vertexIndex]));
			}

			for (int faceIndex = 0; faceIndex < f.Count - 2; faceIndex += 3)
			{
				this.Faces.Add(f[faceIndex + 0],
					f[faceIndex + 1],
					f[faceIndex + 2],
					this.Vertices);
			}
		}

		public event EventHandler Changed;

		public int ChangedCount { get; private set; } = 0;

		private ulong _longHashBeforeClean = 0;

		public ulong LongHashBeforeClean
		{
			get
			{
				if (_longHashBeforeClean == 0)
				{
					_longHashBeforeClean = GetLongHashCode();
				}

				return _longHashBeforeClean;
			}
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Mesh))
			{
				return false;
			}

			return this.Equals((Mesh)obj);
		}

		public override string ToString()
		{
			return $"Faces = {Faces.Count}, Vertices = {Vertices.Count}";
		}

		public bool Equals(Mesh other)
		{
			if (this.Vertices.Count == other.Vertices.Count
				&& this.Faces.Count == other.Faces.Count)
			{
				for (int i = 0; i < Vertices.Count; i++)
				{
					if (Vertices[i] != other.Vertices[i])
					{
						return false;
					}
				}

				for (int i = 0; i < Faces.Count; i++)
				{
					if (Faces[i].v0 != other.Faces[i].v0
						|| Faces[i].v1 != other.Faces[i].v1
						|| Faces[i].v2 != other.Faces[i].v2)
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}

		public void ReverseFaces()
		{
			for (int i = 0; i < Faces.Count; i++)
			{
				ReverseFace(i, false);
			}

			MarkAsChanged();
		}

		public void ReverseFace(int faceIndex, bool markAsChange = true)
		{
			var face = Faces[faceIndex];
			var hold = face.v0;
			face.v0 = face.v2;
			face.v2 = hold;
			face.normal *= -1;

			if (markAsChange)
			{
				MarkAsChanged();
			}
		}

		public void FlipFace(int faceIndex)
		{
			ReverseFace(faceIndex);
		}

		/// <summary>
		/// Merge vertices that share the exact same coordinates
		/// </summary>
		public void CleanAndMerge()
		{
			var newVertices = new List<Vector3Float>();
			var newFaces = new FaceList();

			var positionToIndex = new Dictionary<(float, float, float), int>();
			int GetIndex(Vector3Float position)
			{
				if (positionToIndex.TryGetValue((position.X, position.Y, position.Z), out int index))
				{
					return index;
				}

				var count = newVertices.Count;
				positionToIndex.Add((position.X, position.Y, position.Z), count);
				newVertices.Add(position);
				return count;
			}

			foreach (var face in Faces)
			{
				int iv0 = GetIndex(Vertices[face.v0]);
				int iv1 = GetIndex(Vertices[face.v1]);
				int iv2 = GetIndex(Vertices[face.v2]);
				if (iv0 != iv1 && iv1 != iv2 && iv2 != iv0)
				{
					newFaces.Add(iv0, iv1, iv2, newVertices);
				}
			}

			if (Faces.Count != newFaces.Count
				|| Vertices.Count != newVertices.Count)
			{
				this.Faces = newFaces;
				this.Vertices = newVertices;

				MarkAsChanged();
			}
        }

		public void CopyAllFaces(Mesh mesh, Matrix4X4 matrix)
		{
			foreach (var face in mesh.Faces)
			{
				var v0 = mesh.Vertices[face.v0].Transform(matrix);
				var v1 = mesh.Vertices[face.v1].Transform(matrix);
				var v2 = mesh.Vertices[face.v2].Transform(matrix);
				this.CreateFace(new[] { v0, v1, v2 });
			}
		}

		public BvhTree<int> GetVertexBvhTree()
		{
			var tinyDistance = new Vector3Float(.001, .001, .001);
			var vertexBvhTree = TradeOffBvhConstructor<int>.CreateNewHierarchy(this.Vertices
				.Select((v, i) => new BvhTreeItemData<int>(i, new AxisAlignedBoundingBox(v - tinyDistance, v + tinyDistance))).ToList(),
				DoSimpleSortSize: 10);

			return vertexBvhTree;
		}

        public class UnionFind
        {
            private readonly int[] parent;
            private readonly int[] size;

            public UnionFind(int n)
            {
                parent = new int[n];
                size = new int[n];
                for (int i = 0; i < n; i++)
                {
                    parent[i] = i;
                    size[i] = 1;
                }
            }

            public int Find(int i)
            {
                if (parent[i] != i)
                {
                    parent[i] = Find(parent[i]); // Path compression
                }
                return parent[i];
            }

            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB) return;

                // Union by size
                if (size[rootA] < size[rootB])
                {
                    parent[rootA] = rootB;
                    size[rootB] += size[rootA];
                }
                else
                {
                    parent[rootB] = rootA;
                    size[rootA] += size[rootB];
                }
            }
        }

        public void MergeVertices(double treatAsSameDistance, double minFaceArea, Action<double, string> reporter = null)
        {
            if (Vertices.Count == 0)
            {
                return;
            }

            // Initialize Union-Find data structure
            var uf = new UnionFind(Vertices.Count);

            // Reuse search results list to avoid allocations
            var searchResults = new List<int>();

            // Build kdtree for initial vertex finding
            var vertexBvhTree = GetVertexBvhTree();
            var mergeDistance = (float)treatAsSameDistance;
            var offset = new Vector3Float(mergeDistance, mergeDistance, mergeDistance);

            // First pass: Find all vertices that should be merged
            for (int i = 0; i < Vertices.Count; i++)
            {
                // Skip if this vertex is already merged into another group
                if (uf.Find(i) != i)
                {
                    continue;
                }

				if (reporter != null
					&& i%256 == 0)
				{
					reporter(i / (double)Vertices.Count, "Merge Vertices");
				}

                searchResults.Clear();
                var edgeAabb = new AxisAlignedBoundingBox(Vertices[i] - offset, Vertices[i] + offset);
                vertexBvhTree.SearchBounds(edgeAabb, searchResults);

                // Find minimum index in search results and union all vertices
                if (searchResults.Count > 1)
                {
                    foreach (var result in searchResults)
                    {
                        uf.Union(i, result);
                    }
                }
            }

            // Arrays to store position sums for each representative vertex
            var xSum = new double[Vertices.Count];
            var ySum = new double[Vertices.Count];
            var zSum = new double[Vertices.Count];
            var count = new int[Vertices.Count];

            // Accumulate positions for each representative
            for (int i = 0; i < Vertices.Count; i++)
            {
                int rep = uf.Find(i);
                var vertex = Vertices[i];
                xSum[rep] += vertex.X;
                ySum[rep] += vertex.Y;
                zSum[rep] += vertex.Z;
                count[rep]++;
            }

            // Create mapping from representative indices to new vertex indices
            var repToNewIndex = new Dictionary<int, int>();
            var newVertices = new List<Vector3Float>();

            // Create new vertices for each representative
            for (int i = 0; i < Vertices.Count; i++)
            {
                if (count[i] > 0)  // This is a representative vertex
                {
                    repToNewIndex[i] = newVertices.Count;
                    newVertices.Add(new Vector3Float(
                        (float)(xSum[i] / count[i]),
                        (float)(ySum[i] / count[i]),
                        (float)(zSum[i] / count[i])
                    ));
                }
            }

            // Create new faces, skipping degenerate ones
            var newFaces = new FaceList();
            foreach (var face in Faces)
            {
                int newV0 = repToNewIndex[uf.Find(face.v0)];
                int newV1 = repToNewIndex[uf.Find(face.v1)];
                int newV2 = repToNewIndex[uf.Find(face.v2)];

                if (newV0 != newV1 && newV1 != newV2 && newV2 != newV0)
                {
                    float area = newVertices[newV0].GetArea(newVertices[newV1], newVertices[newV2]);
                    if (area >= minFaceArea)
                    {
                        newFaces.Add(newV0, newV1, newV2, newVertices);
                    }
                }
            }

            // Update the mesh if changes were made
            if (Faces.Count != newFaces.Count || Vertices.Count != newVertices.Count)
            {
                Vertices = newVertices;
                Faces = newFaces;
                MarkAsChanged();
            }
        }

        /// <summary>
        /// Split the given face on the given plane. Remove the original face
        /// and add as many new faces as required for the split.
        /// </summary>
        /// <param name="faceIndex">The index of the face to split.</param>
        /// <param name="plane">The plane to split the face on. The face will not be split
        /// if it is not intersected by this plane.</param>
        /// <param name="onPlaneDistance">If a given edge of the face has a vertex that is within
        /// this distance of the plane, the edge will not be split.</param>
        /// <returns>Returns if the edge was actually split.</returns>
        public bool SplitFace(int faceIndex, Plane plane, double onPlaneDistance = .001)
		{
			var newVertices = new List<Vector3Float>();
			var newFaces = new List<Face>();
			if (Faces[faceIndex].Split(this.Vertices, plane, newFaces, newVertices, onPlaneDistance))
			{
				var vertexCount = Vertices.Count;
				// remove the face index
				Faces.RemoveAt(faceIndex);
				// add the new vertices
				Vertices.AddRange(newVertices);
				// add the new faces (have to make the vertex indices to the new vertices
				foreach (var newFace in newFaces)
				{
					Face faceNewIndices = newFace;
					faceNewIndices.v0 += vertexCount;
					faceNewIndices.v1 += vertexCount;
					faceNewIndices.v2 += vertexCount;
					Faces.Add(faceNewIndices);
				}

				CleanAndMerge();

				return true;
			}

			return false;
		}

		public class SplitData
		{
			public Face Face { get; }

			public double[] Dist { get; }

			public SplitData(Face face, double[] dist)
			{
				this.Face = face;
				this.Dist = dist;
			}
		}

		public bool Split(Plane plane, double onPlaneDistance = .001, Func<SplitData, bool> clipFace = null, bool cleanAndMerge = true, bool discardFacesOnNegativeSide = false)
		{
			var newVertices = new List<Vector3Float>();
			var newFaces = new List<Face>();
			var facesToRemove = new HashSet<int>();

			for (int i = 0; i < Faces.Count; i++)
			{
				var face = Faces[i];

				if (face.Split(this.Vertices, plane, newFaces, newVertices, onPlaneDistance, clipFace, discardFacesOnNegativeSide))
				{
					// record the face for removal
					facesToRemove.Add(i);
				}
			}

			// make a new list of all the faces we are keeping
			var keptFaces = new FaceList();
			for (int i = 0; i < Faces.Count; i++)
			{
				if (!facesToRemove.Contains(i))
				{
					keptFaces.Add(Faces[i]);
				}
			}

			var vertexCount = Vertices.Count;

			// add the new vertices
			Vertices.AddRange(newVertices);

			// add the new faces (have to make the vertex indices to the new vertices
			foreach (var newFace in newFaces)
			{
				Face faceNewIndices = newFace;
				faceNewIndices.v0 += vertexCount;
				faceNewIndices.v1 += vertexCount;
				faceNewIndices.v2 += vertexCount;
				keptFaces.Add(faceNewIndices);
			}

			Faces = keptFaces;

			if (cleanAndMerge)
			{
				CleanAndMerge();
			}

			return true;
		}

		public ulong GetLongHashCode(ulong hash = 14695981039346656037)
		{
			unchecked
			{
				hash = Vertices.Count.GetLongHashCode(hash);
				hash = Faces.Count.GetLongHashCode(hash);

				// we want to at most consider 100000 vertices
				int vertexStep = Math.Max(1, Vertices.Count / 1000);
				for (int i = 0; i < Vertices.Count; i += vertexStep)
				{
					var vertex = Vertices[i];
					hash = vertex.GetLongHashCode(hash);
				}

				// we want to at most consider 100000 faces
				int faceStep = Math.Max(1, Faces.Count / 10000);
				for (int i = 0; i < Faces.Count; i += faceStep)
				{
					var face = Faces[i];
					hash = face.v0.GetLongHashCode(hash);
					hash = face.v1.GetLongHashCode(hash);
					hash = face.v2.GetLongHashCode(hash);
				}

				return hash;
			}
		}

		public void MarkAsChanged()
		{
			// mark this unchecked as we don't want to throw an exception if this rolls over.
			unchecked
			{
				transformedAabbCache.Changed();
				cachedAABB = null;
				ChangedCount++;
				Changed?.Invoke(this, null);
			}
		}

		public void Transform(Matrix4X4 matrix)
		{
			if (matrix != Matrix4X4.Identity)
			{
				Vertices.Transform(matrix);
				// var inverted = matrix.Inverted;
				Parallel.For(0, Faces.Count, (i) =>
				// for (int i = 0; i < Faces.Count; i++)
				{
					Faces[i] = new Face(Faces[i].v0, Faces[i].v1, Faces[i].v2, Vertices);
					// don't know why one of these does not work
					//Faces[i] = new Face(Faces[i].v0, Faces[i].v1, Faces[i].v2, Faces[i].normal.TransformNormal(matrix));
					//Faces[i] = new Face(Faces[i].v0, Faces[i].v1, Faces[i].v2, Faces[i].normal.TransformNormalInverse(inverted));
				}
				);

				MarkAsChanged();
			}
		}

		public void CalculateNormals()
		{
			foreach (var face in Faces)
			{
				face.CalculateNormal(Vertices);
			}

			MarkAsChanged();
		}

		public void Translate(double x, double y, double z)
		{
			Translate(new Vector3(x, y, z));
		}

		public void Translate(Vector3 offset)
		{
			if (offset != Vector3.Zero)
			{
				Vertices.Transform(Matrix4X4.CreateTranslation(offset));

				MarkAsChanged();
			}
		}

		// private static Dictionary<object, int> Ids = new Dictionary<object, int>(ReferenceEqualityComparer.Default);
		private static int nextId = 0;

		public static int GetID()
		{
			lock (nextIdLocker)
			{
				return nextId++;
			}
		}

		public AxisAlignedBoundingBox GetAxisAlignedBoundingBox()
		{
			if (Vertices.Count == 0)
			{
				return new AxisAlignedBoundingBox(Vector3.Zero, Vector3.Zero);
			}

			if (cachedAABB == null)
			{
				cachedAABB = Vertices.Bounds();
			}

			return cachedAABB;
		}

		public AxisAlignedBoundingBox GetAxisAlignedBoundingBox(Matrix4X4 transform)
		{
			return transformedAabbCache.GetAxisAlignedBoundingBox(this, transform);
		}

		public override int GetHashCode()
		{
			return (int)GetLongHashCode();
		}

		public void CreateFace(params Vector3[] positionsIn)
		{
			var positions = positionsIn.Distinct();
			int firstVertex = this.Vertices.Count;
			// we don't have to iterate the positions twice if we count them as we add them
			int addedPositions = 0;
			foreach (var p in positions)
			{
				this.Vertices.Add(p);
				addedPositions++;
			}

			for (int i = 0; i < addedPositions - 2; i++)
			{
				this.Faces.Add(firstVertex, firstVertex + i + 1, firstVertex + i + 2, this.Vertices);
			}
		}

		public void CreateFace(IEnumerable<Vector3Float> positionsIn)
		{
			var positions = positionsIn.Distinct();
			int firstVertex = this.Vertices.Count;
			// we don't have to iterate the positions twice if we count them as we add them
			int addedPositions = 0;
			foreach (var p in positions)
			{
				this.Vertices.Add(p);
				addedPositions++;
			}

			for (int i = 0; i < addedPositions - 2; i++)
			{
				this.Faces.Add(firstVertex, firstVertex + i + 1, firstVertex + i + 2, this.Vertices);
			}
		}
	}

	public static class FaceExtensionMethods
	{
		public static AxisAlignedBoundingBox GetAxisAlignedBoundingBox(this Face face, Mesh mesh)
		{
			var bounds = AxisAlignedBoundingBox.Empty();
			bounds.ExpandToInclude(mesh.Vertices[face.v0]);
			bounds.ExpandToInclude(mesh.Vertices[face.v1]);
			bounds.ExpandToInclude(mesh.Vertices[face.v2]);
			return bounds;
		}

		/// <summary>
		/// Split the face at the given plane.
		/// </summary>
		/// <param name="face">The face to split.</param>
		/// <param name="faceVertices">The list containing the vertices for the face.</param>
		/// <param name="plane">The plane to split at.</param>
		/// <param name="newFaces">The new faces created will be added to this list, not the mesh.</param>
		/// <param name="newVertices">The new vertices will be added to this list, not the mesh.</param>
		/// <param name="onPlaneDistance">Treat any distance less than this as not crossing the plane.</param>
		/// <param name="clipFace">An optional function that can be called to check if the given
		/// face should be clipped.</param>
		/// <returns>True if the face crosses the plane else false.</returns>
		public static bool Split(this Face face,
			List<Vector3Float> faceVertices, Plane plane, List<Face> newFaces, List<Vector3Float> newVertices,
			double onPlaneDistance, Func<Mesh.SplitData, bool> clipFace = null, bool discardFacesOnNegativeSide = false)
		{
			var v = new Vector3Float[]
			{
				faceVertices[face.v0],
				faceVertices[face.v1],
				faceVertices[face.v2]
			};

			// get the distance from the crossing plane
			var dist = v.Select(a => plane.GetDistanceFromPlane(a)).ToArray();

			// bool if each point is clipped
			var clipPoint = dist.Select(a => Math.Abs(a) > onPlaneDistance).ToArray();

			// bool if there is a clip on a line segment (between points)
			var clipSegment = clipPoint.Select((a, i) =>
			{
				var nextI = (i + 1) % 3;
				// if both points are clipped and they are on opposite sides of the clip plane
				return clipPoint[i] && clipPoint[nextI] && ((dist[i] < 0 && dist[nextI] > 0) || (dist[i] > 0 && dist[nextI] < 0));
			}).ToArray();

			// the number of segments that need to be clipped
			var segmentsClipped = clipSegment[0] ? 1 : 0;
			segmentsClipped += clipSegment[1] ? 1 : 0;
			segmentsClipped += clipSegment[2] ? 1 : 0;

			void ClipEdge(int vi0)
			{
				var vi1 = (vi0 + 1) % 3;
				var vi2 = (vi0 + 2) % 3;
				var totalDistance = Math.Abs(dist[vi0]) + Math.Abs(dist[vi1]);
				var ratioTodist0 = Math.Abs(dist[vi0]) / totalDistance;
				var newPoint = v[vi0] + (v[vi1] - v[vi0]) * ratioTodist0;
				// add the new vertex
				newVertices.Add(newPoint);
			}

			switch (segmentsClipped)
			{
				// if 2 sides are clipped we will add 2 new vertices and 3 polygons
				case 2:
					if (clipFace?.Invoke(new Mesh.SplitData(face, dist)) != false)
					{
						// find the side we are not going to clip
						int vi0 = clipSegment[0] && clipSegment[1] ? 2
							: clipSegment[0] && clipSegment[2] ? 1 : 0;
						var vi1 = (vi0 + 1) % 3;
						var vi2 = (vi0 + 2) % 3;
						// get the current count
						var vertexStart = newVertices.Count;
						// add the existing vertices
						newVertices.Add(v[vi0]);
						newVertices.Add(v[vi1]);
						newVertices.Add(v[vi2]);
						// clip the edges, will add the new points
						ClipEdge(vi1);
						ClipEdge(vi2);
						// add the new faces
						if (!discardFacesOnNegativeSide || dist[vi0] > 0)
						{
							newFaces.Add(new Face(vertexStart, vertexStart + 1, vertexStart + 3, newVertices));
							newFaces.Add(new Face(vertexStart, vertexStart + 3, vertexStart + 4, newVertices));
						}
						if (!discardFacesOnNegativeSide || !(dist[vi0] > 0))
							newFaces.Add(new Face(vertexStart + 3, vertexStart + 2, vertexStart + 4, newVertices));
						return true;
					}

					break;

				// if 1 side is clipped we will add 1 new vertex and 2 polygons
				case 1:
					{
						// find the side we are going to clip
						int vi0 = clipSegment[0] ? 0 : clipSegment[1] ? 1 : 2;
						var vi1 = (vi0 + 1) % 3;
						var vi2 = (vi0 + 2) % 3;
						// get the current count
						var vertexStart = newVertices.Count;
						// add the existing vertices
						newVertices.Add(v[vi0]);
						newVertices.Add(v[vi1]);
						newVertices.Add(v[vi2]);
						// clip the edge, will add the new point
						ClipEdge(vi0);
						// add the new faces
						if (!discardFacesOnNegativeSide || dist[vi0] > 0)
							newFaces.Add(new Face(vertexStart, vertexStart + 3, vertexStart + 2, newVertices));
						if (!discardFacesOnNegativeSide || !(dist[vi0] > 0))
							newFaces.Add(new Face(vertexStart + 3, vertexStart + 1, vertexStart + 2, newVertices));
					}

					return true;

				case 0:
					// This face doesn't cross the plane.
					if (discardFacesOnNegativeSide && !(dist.Max() > onPlaneDistance))
						return true;
					break;
			}

			return false;
		}

		public static bool GetCutLine(this Face face,
			List<Vector3Float> faceVertices,
			Plane plane,
			out Vector3 start,
			out Vector3 end,
			double onPlaneDistance = 0,
			Func<Mesh.SplitData, bool> clipFace = null)
		{
			var v = new Vector3Float[]
			{
				faceVertices[face.v0],
				faceVertices[face.v1],
				faceVertices[face.v2]
			};

			// get the distance from the crossing plane
			var dist = new double[]
			{
				plane.GetDistanceFromPlane(v[0]),
				plane.GetDistanceFromPlane(v[1]),
				plane.GetDistanceFromPlane(v[2]),
			};

			Vector3 ClipEdge(int vi0)
			{
				var vi1 = (vi0 + 1) % 3;
				var totalDistance = Math.Abs(dist[vi0]) + Math.Abs(dist[vi1]);
				var ratioTodist0 = Math.Abs(dist[vi0]) / totalDistance;
				var newPoint = v[vi0] + (v[vi1] - v[vi0]) * ratioTodist0;
				// add the new vertex
				return new Vector3(newPoint);
			}

			if (dist[0] < 0 && dist[1] >= 0 && dist[2] >= 0)
			{
				// p2   p1
				// --------
				//   p0
				start = ClipEdge(2);
				end = ClipEdge(0);
				return true;
			}
			else if (dist[0] >= 0 && dist[1] < 0 && dist[2] < 0)
			{
				// p0
				// --------
				// p1  p2
				start = ClipEdge(0);
				end = ClipEdge(2);
				return true;
			}
			else if (dist[0] >= 0 && dist[1] < 0 && dist[2] >= 0)
			{
				// p0   p2
				// --------
				//   p1
				start = ClipEdge(0);
				end = ClipEdge(1);
				return true;
			}
			else if (dist[0] < 0 && dist[1] >= 0 && dist[2] < 0)
			{
				// p1
				// --------
				// p2  p0
				start = ClipEdge(1);
				end = ClipEdge(0);
				return true;
			}
			else if (dist[0] >= 0 && dist[1] >= 0 && dist[2] < 0)
			{
				// p1   p0
				// --------
				//   p2
				start = ClipEdge(1);
				end = ClipEdge(2);
				return true;
			}
			else if (dist[0] < 0 && dist[1] < 0 && dist[2] >= 0)
			{
				// p2
				// --------
				// p0  p1
				start = ClipEdge(2);
				end = ClipEdge(1);
				return true;
			}

			start = Vector3.Zero;
			end = Vector3.Zero;
			return false;
		}

		public static double GetArea(this Face face, Mesh mesh)
		{
			// area = (a * c * sen(B))/2
			var p0 = mesh.Vertices[face.v0];
			var p1 = mesh.Vertices[face.v1];
			var p2 = mesh.Vertices[face.v2];
			var xy = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
			var xz = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

			double a = (p0 - p1).Length;
			double c = (p0 - p2).Length;
			double b = Vector3.CalculateAngle(xy, xz);

			return (a * c * Math.Sin(b)) / 2d;
		}
	}

	public struct PositionNormal
	{
		public Vector3Float position;
		public Vector3Float normal;
	}


	public static class MeshExtensionMethods
	{
		public static Mesh Copy(this Mesh meshToCopyIn, CancellationToken cancellationToken, Action<double, string> progress = null, bool allowFastCopy = true)
		{
			if (meshToCopyIn != null)
			{
				return new Mesh(meshToCopyIn.Vertices, meshToCopyIn.Faces);
			}

			return null;
		}

		public static Plane GetPlane(this Mesh mesh, int faceIndex)
		{
			var face = mesh.Faces[faceIndex];
			var verts = mesh.Vertices;
			return new Plane(verts[face.v0], verts[face.v1], verts[face.v2]);
		}

		public static PositionNormal[] ToPositionNormalArray(this Mesh mesh)
		{
			List<Vector3Float> positions = mesh.Vertices;
			var faces = mesh.Faces;

			var array = new PositionNormal[faces.Count];

			for (var i = 0; i < faces.Count; i++)
			{
				array[i] = new PositionNormal()
				{
					position = positions[faces[i].v0],
					normal = faces[i].normal,
				};
			}

			return array;
		}


		public static IEnumerable<int> GetCoplanarFaces(this Mesh mesh, Plane plane)
		{
			double normalTolerance = .001;
			double distanceTolerance = .001;

			// TODO: check if the mesh has a face acceleration structure on it (if so use it)
			var normalToleranceSquared = normalTolerance * normalTolerance;
			for (int faceIndex = 0; faceIndex < mesh.Faces.Count; faceIndex++)
			{
				var face = mesh.Faces[faceIndex];
				var faceNormal = mesh.Faces[faceIndex].normal;
				var distanceFromOrigin = faceNormal.Dot(mesh.Vertices[face.v0]);

				if (Math.Abs(plane.DistanceFromOrigin - distanceFromOrigin) <= distanceTolerance
					&& (plane.Normal - new Vector3(faceNormal)).LengthSquared <= normalToleranceSquared)
				{
					yield return faceIndex;
				}
			}
		}

		public static IReadOnlyList<VertexFaceList> GetVertexFaceLists(this Mesh mesh)
		{
			return VertexFaceList.CreateVertexFaceList(mesh);
		}

		public static IReadOnlyList<MeshEdge> GetMeshEdges(this Mesh mesh)
		{
			return MeshEdge.CreateMeshEdgeList(mesh);
		}

		public static IEnumerable<int> GetCoplanarFaces(this Mesh mesh, int faceIndex)
		{
			var plane = mesh.GetPlane(faceIndex);

			return mesh.GetCoplanarFaces(plane);
		}

		public static double GetSurfaceArea(this Mesh mesh, int faceIndex)
		{
			var face = mesh.Faces[faceIndex];
			var verts = mesh.Vertices;
			var a = (verts[face.v0] - verts[face.v1]).Length;
			var b = (verts[face.v1] - verts[face.v2]).Length;
			var c = (verts[face.v2] - verts[face.v0]).Length;
			var p = 0.5 * (a + b + c);
			return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
		}

		public static Matrix4X4 GetMaxPlaneProjection(this Mesh mesh, IEnumerable<int> faces, ImageBuffer textureToUse, Matrix4X4? initialTransform = null)
		{
			// If not set than make it identity
			var firstTransform = initialTransform == null ? Matrix4X4.Identity : (Matrix4X4)initialTransform;

			var textureCoordinateMapping = Matrix4X4.CreateRotation(new Quaternion(mesh.Faces[faces.First()].normal.AsVector3(), Vector3.UnitZ));

			var bounds = RectangleDouble.ZeroIntersection;

			foreach (var face in faces)
			{
				foreach (int vertexIndex in new int[] { mesh.Faces[face].v0, mesh.Faces[face].v1, mesh.Faces[face].v2 })
				{
					var edgeStartPosition = mesh.Vertices[vertexIndex];
					var textureUv = edgeStartPosition.Transform(textureCoordinateMapping);
					bounds.ExpandToInclude(new Vector2(textureUv));
				}
			}

			var centering = Matrix4X4.CreateTranslation(new Vector3(-bounds.Left, -bounds.Bottom, 0));
			var scaling = Matrix4X4.CreateScale(new Vector3(1 / bounds.Width, 1 / bounds.Height, 1));

			return textureCoordinateMapping * firstTransform * centering * scaling;
		}

		/// <summary>
		/// Split a mesh at a series of coplanar planes.
		/// </summary>
		/// <param name="mesh">The mesh to split faces on.</param>
		/// <param name="planeNormal">The plane normal of the planes to split on.</param>
		/// <param name="distancesFromOrigin">The series of coplanar planes to split the mash at.</param>
		/// <param name="onPlaneDistance">Any mesh edge that has a vertex at this distance or less from a cut plane
		/// should not be cut by that plane.</param>
		public static void SplitOnPlanes(this Mesh mesh, Vector3 planeNormal, List<double> distancesFromOrigin, double onPlaneDistance)
		{
			for (int i = 0; i < distancesFromOrigin.Count; i++)
			{
				mesh.Split(new Plane(planeNormal, distancesFromOrigin[i]), onPlaneDistance, (clipData) =>
				{
					// if two distances are less than 0
					if ((clipData.Dist[0] < 0 && clipData.Dist[1] < 0)
						|| (clipData.Dist[1] < 0 && clipData.Dist[2] < 0)
						|| (clipData.Dist[2] < 0 && clipData.Dist[0] < 0))
					{
						return true;
					}

					return false;
				});
			}

			for (int i = distancesFromOrigin.Count - 1; i >= 0; i--)
			{
				mesh.Split(new Plane(planeNormal, distancesFromOrigin[i]), .1);
			}

			return;
		}


		public static Matrix4X4 GetMaxPlaneProjection(this Mesh mesh, int face, ImageBuffer textureToUse, Matrix4X4? initialTransform = null)
		{
			// If not set than make it identity
			var firstTransform = initialTransform == null ? Matrix4X4.Identity : (Matrix4X4)initialTransform;

			var textureCoordinateMapping = Matrix4X4.CreateRotation(new Quaternion(mesh.Faces[face].normal.AsVector3(), Vector3.UnitZ));

			var bounds = RectangleDouble.ZeroIntersection;

			foreach (int vertexIndex in new int[] { mesh.Faces[face].v0, mesh.Faces[face].v1, mesh.Faces[face].v2 })
			{
				var edgeStartPosition = mesh.Vertices[vertexIndex];
				var textureUv = edgeStartPosition.Transform(textureCoordinateMapping);
				bounds.ExpandToInclude(new Vector2(textureUv));
			}

			var centering = Matrix4X4.CreateTranslation(new Vector3(-bounds.Left, -bounds.Bottom, 0));
			var scaling = Matrix4X4.CreateScale(new Vector3(1 / bounds.Width, 1 / bounds.Height, 1));

			return textureCoordinateMapping * firstTransform * centering * scaling;
		}

		public static void PlaceTextureOnFaces(this Mesh mesh, int face, ImageBuffer textureToUse)
		{
			//// planar project along the normal of this face
			var faces = mesh.GetCoplanarFaces(face);
			if (faces.Any())
			{
				mesh.PlaceTextureOnFaces(faces, textureToUse, mesh.GetMaxPlaneProjection(faces, textureToUse));
			}
		}

		public static void PlaceTextureOnFace(this Mesh mesh, int face, ImageBuffer textureToUse)
		{
			//// planar project along the normal of this face
			mesh.PlaceTextureOnFace(face, textureToUse, mesh.GetMaxPlaneProjection(face, textureToUse));
		}

		public static void PlaceTextureOnFace(this Mesh mesh, int face, ImageBuffer textureToUse, Matrix4X4 textureCoordinateMapping, bool markAsChange = true)
		{
			var faceTextures = new Dictionary<int, FaceTextureData>(mesh.FaceTextures);

			var uvs = new Vector2Float[3];
			int uvIndex = 0;
			foreach (int vertexIndex in new int[] { mesh.Faces[face].v0, mesh.Faces[face].v1, mesh.Faces[face].v2 })
			{
				var edgeStartPosition = mesh.Vertices[vertexIndex];
				var textureUv = edgeStartPosition.Transform(textureCoordinateMapping);
				uvs[uvIndex++] = new Vector2Float(textureUv);
			}

			faceTextures[face] = new FaceTextureData(textureToUse, uvs[0], uvs[1], uvs[2]);

			mesh.FaceTextures = faceTextures;

			if (markAsChange)
			{
				mesh.MarkAsChanged();
			}
		}

		public static void PlaceTextureOnFaces(this Mesh mesh, IEnumerable<int> faces, ImageBuffer textureToUse, Matrix4X4 textureCoordinateMapping)
		{
			var faceTextures = new Dictionary<int, FaceTextureData>();

			var uvs = new Vector2Float[3];
			foreach (var face in faces)
			{
				int uvIndex = 0;
				foreach (int vertexIndex in new int[] { mesh.Faces[face].v0, mesh.Faces[face].v1, mesh.Faces[face].v2 })
				{
					var edgeStartPosition = mesh.Vertices[vertexIndex];
					var textureUv = edgeStartPosition.Transform(textureCoordinateMapping);
					uvs[uvIndex++] = new Vector2Float(textureUv);
				}

				faceTextures.Add(face, new FaceTextureData(textureToUse, uvs[0], uvs[1], uvs[2]));
			}

			// add in any existing face textures
			if (mesh.FaceTextures != null)
			{
				foreach (var kvp in mesh.FaceTextures)
				{
					faceTextures[kvp.Key] = kvp.Value;
				}
			}

			mesh.FaceTextures = faceTextures;

			mesh.MarkAsChanged();
		}

		/// <summary>
		/// Copy all of the faces from the copyFrom mesh into the copyTo mesh
		/// </summary>
		/// <param name="copyTo">The mesh receiving the copied faces</param>
		/// <param name="copyFrom">The mesh providing the faces to copy</param>
		public static void CopyFaces(this Mesh copyTo, Mesh copyFrom)
		{
			int vStart = copyTo.Vertices.Count;
			// add all the vertices
			for (int i = 0; i < copyFrom.Vertices.Count; i++)
			{
				copyTo.Vertices.Add(copyFrom.Vertices[i]);
			}

			// add all the faces
			for (int i = 0; i < copyFrom.Faces.Count; i++)
			{
				var face = copyFrom.Faces[i];
				copyTo.Faces.Add(face.v0 + vStart, face.v1 + vStart, face.v2 + vStart, face.normal);
			}
		}

		/// <summary>
		/// Copy a face from the copyFrom mesh into the copyTo mesh
		/// </summary>
		/// <param name="copyTo">The mesh receiving the copied faces</param>
		/// <param name="copyFrom">The mesh providing the faces to copy</param>
		/// <param name="faceIndex">The index of the face to copy</param>
		public static Face AddFaceCopy(this Mesh copyTo, Mesh copyFrom, int faceIndex)
		{
			int vStart = copyTo.Vertices.Count;
			var face = copyFrom.Faces[faceIndex];

			// add all the vertices
			copyTo.Vertices.Add(copyFrom.Vertices[face.v0]);
			copyTo.Vertices.Add(copyFrom.Vertices[face.v1]);
			copyTo.Vertices.Add(copyFrom.Vertices[face.v2]);

			// add all the face
			copyTo.Faces.Add(vStart, vStart + 1, vStart + 2, face.normal);

			return copyTo.Faces[copyTo.Faces.Count - 1];
		}

		public static void RemoveTexture(this Mesh mesh, ImageBuffer texture, int index)
		{
			for (int i = 0; i < mesh.Faces.Count; i++)
			{
				mesh.RemoveTexture(i, texture, index);
			}
		}

		public static void RemoveTexture(this Mesh mesh, int faceIndex, ImageBuffer texture, int index)
		{
			var faceTextures = mesh.FaceTextures;
			if (faceTextures.ContainsKey(faceIndex)
				&& faceTextures[faceIndex]?.image == texture)
			{
				faceTextures.Remove(faceIndex);
				mesh.MarkAsChanged();
			}
		}

		public static void PlaceTexture(this Mesh mesh, ImageBuffer textureToUse, Matrix4X4 textureCoordinateMapping)
		{
			for (int i = 0; i < mesh.Faces.Count; i++)
			{
				mesh.PlaceTextureOnFace(i, textureToUse, textureCoordinateMapping, false);
			}

			mesh.MarkAsChanged();
		}

		public static Mesh TexturedPlane(ImageBuffer textureToUse, double xScale = 1, double yScale = 1)
		{
			throw new NotImplementedException();
			// Mesh texturedPlane = MeshHelper.CreatePlane(xScale, yScale);
			// {
			// Face face = texturedPlane.Faces[0];
			// PlaceTextureOnFace(face, textureToUse);
			// }

			// return texturedPlane;
		}

		public static IEnumerable<MeshEdge> GetNonManifoldEdges(this Mesh mesh)
		{
			foreach (var meshEdge in mesh.GetMeshEdges())
			{
				if (meshEdge.Faces.Count() != 2)
				{
					yield return meshEdge;
				}
			}
		}

		/// <summary>
		/// For every T Junction add a vertex to the mesh edge that needs one.
		/// </summary>
		/// <param name="mesh">The mesh to repair.</param>
		public static void RepairTJunctions(this Mesh mesh)
		{
			var nonManifoldEdges = mesh.GetNonManifoldEdges();
			var vertexBvhTree = mesh.GetVertexBvhTree();
			var searchResults = new List<int>();

			foreach (MeshEdge edge in nonManifoldEdges)
			{
				var start = mesh.Vertices[edge.Vertex0Index];
				var end = mesh.Vertices[edge.Vertex1Index];
				Vector3Float normal = (end - start).GetNormal();

				// Get all the vertices that lay on this edge
				var edgeAabb = new AxisAlignedBoundingBox(start, end);
				searchResults.Clear();

				vertexBvhTree.SearchBounds(edgeAabb, searchResults);
				// map them to this new vertex
				foreach (var result in searchResults)
				{
					// Test if the vertex falls on the edge
					Vector3Float edgeDirection = (end - start).GetNormal();
					var vertex = mesh.Vertices[result];
					Vector3Float vertexDirection = (vertex - start).GetNormal();
					float dotProduct = edgeDirection.Dot(vertexDirection);

					if (Math.Abs(dotProduct - 1) < 1e-6f)
					{
						// If the vertex falls on the edge, split the edge at the vertex
						//IVertex createdVertex;
						//MeshEdge createdMeshEdge;
						//mesh.SplitMeshEdge(edge, out createdVertex, out createdMeshEdge);
						//createdVertex.Position = vertex;
						//createdVertex.Normal = normal;
						//mesh.MergeVertices(vertex, createdVertex);
					}
				}
			}

			// Merge the vertices for the edges that are now manifold
			mesh.CleanAndMerge();
		}

		public static bool IsManifold(this Mesh mesh)
		{
			var meshEdgeList = mesh.GetMeshEdges();

			foreach (var meshEdge in meshEdgeList)
			{
				if (meshEdge.Faces.Count() != 2)
				{
					return false;
				}
			}

			return true;
		}

		public static void RemoveUnusedVertices(this Mesh mesh)
		{
			var usedVertices = new HashSet<int>();

			// Collect vertices used in faces
			foreach (var face in mesh.Faces)
			{
				usedVertices.Add(face.v0);
				usedVertices.Add(face.v1);
				usedVertices.Add(face.v2);
			}

			// Create new vertex list with only used vertices
			var newVertices = new List<Vector3Float>();
			var oldToNewIndex = new int[mesh.Vertices.Count];

			for (int i = 0; i < mesh.Vertices.Count; i++)
			{
				if (usedVertices.Contains(i))
				{
					oldToNewIndex[i] = newVertices.Count;
					newVertices.Add(mesh.Vertices[i]);
				}
				else
				{
					oldToNewIndex[i] = -1;
				}
			}

			// Remap faces to use new vertex indices
			var newFaces = new FaceList();
			foreach (var face in mesh.Faces)
			{
				newFaces.Add(
					oldToNewIndex[face.v0],
					oldToNewIndex[face.v1],
					oldToNewIndex[face.v2],
					newVertices
				);
			}

			mesh.Vertices = newVertices;
			mesh.Faces = newFaces;
			mesh.MarkAsChanged();
		}

		public static void RemoveDegenerateFaces(this Mesh mesh, double minFaceArea)
		{
			var newFaces = new FaceList();

			foreach (var face in mesh.Faces)
			{
				// Only keep faces where all vertices are different
				if (face.v0 != face.v1 && face.v1 != face.v2 && face.v2 != face.v0)
				{
                    var area = face.GetArea(mesh);

                    if (area < minFaceArea)
                    {
                        continue;
                    }
                    
					// Recalculate normal since we're creating a new face
                    newFaces.Add(face.v0, face.v1, face.v2, mesh.Vertices);
				}
			}

			mesh.Faces = newFaces;
			mesh.MarkAsChanged();
		}
	}
}