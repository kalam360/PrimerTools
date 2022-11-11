using System;
using System.ComponentModel;
using UnityEngine;

namespace Primer.Graph
{
    public readonly struct ContinuousGrid : IGrid
    {
        public static ContinuousGrid zero = new(0);
        public static IGrid Lerp(IGrid a, IGrid b, float t) =>
            new ContinuousGrid(IGrid.Lerp(a, b, t));


        public Vector3[] Points { get; }
        public int Size { get; }

        int PointsPerSide => Size + 1;


        ContinuousGrid(Vector3[] points, int size) {
            Points = points;
            Size = size;
        }


        #region Public constructors
        public ContinuousGrid(int size) {
            Size = size;
            Points = new Vector3[(size + 1) * (size + 1)];
        }

        public ContinuousGrid(
            Vector3[] vertices,
            [DefaultValue("Vector3.zero")]
            Vector3? fillValue = null,
            int minSize = 1
        ) {
            var originalSize = GetSizeFromArray(vertices) - 1;

            if (originalSize.IsInteger() && originalSize >= minSize) {
                Size = (int)originalSize;
                Points = vertices;
                return;
            }

            var fill = fillValue ?? Vector3.zero;
            var newSize = Mathf.Max(Mathf.CeilToInt(originalSize), minSize);
            var newLength = (newSize + 1) * (newSize + 1);
            var newVertices = new Vector3[newLength];

            vertices.CopyTo(newVertices, 0);

            // Fill places required to make a grid out of it
            for (var i = vertices.Length; i < newLength; i++) {
                newVertices[i] = fill;
            }

            Size = newSize;
            Points = newVertices;
        }
        #endregion


        #region Render to mesh
        public void RenderTo(Mesh mesh, bool bothSides = false) {
            var triangles = DefineTriangles(bothSides);
            var points = bothSides ? DuplicatedList(Points) : Points;

            mesh.Clear();
            mesh.vertices = points;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }
        #endregion


        #region Manipulation
        public IGrid Resize(int newSize) {
            var size = Size;
            if (newSize == size) return this;

            var pointsPerSide = newSize + 1;
            var result = new Vector3[pointsPerSide * pointsPerSide];

            for (var y = 0; y < pointsPerSide; y++) {
                for (var x = 0; x < pointsPerSide; x++) {
                    var i = y * pointsPerSide + x;
                    var col = (float)x / newSize * size;
                    var row = (float)y / newSize * size;

                    result[i] = col.IsInteger() && row.IsInteger()
                        ? Points[(int)row * PointsPerSide + (int)col]
                        : QuadLerp(row, col);
                }
            }

            return new ContinuousGrid(result, newSize);
        }

        public IGrid Crop(float croppedSize) {
            if (croppedSize.IsInteger() && Size == (int)croppedSize) {
                return this;
            }

            if (Size < croppedSize) {
                throw new Exception("Crop size is bigger than grid area. Do you want IGrid.Resize()?");
            }

            if (croppedSize == 0) {
                return zero;
            }

            var finalSize = Mathf.CeilToInt(croppedSize);
            var lastIndex = finalSize;
            var currentPps = PointsPerSide;
            var newPps = finalSize + 1;
            var t = croppedSize % 1;

            var points = Points;
            var copy = new Vector3[newPps * newPps];

            // Copy unchanged points, including points to lerp
            for (var x = 0; x < newPps; x++) {
                for (var y = 0; y < newPps; y++) {
                    copy[y * newPps + x] = points[y * currentPps + x];
                }
            }

            // Lerp bottom
            for (var x = 0; x < newPps; x++) {
                var y = lastIndex;
                var a = points[(y - 1) * currentPps + x];
                var b = points[y * currentPps + x];
                copy[y * newPps + x] = Vector3.Lerp(a, b, t);
            }

            // Lerp right side
            for (var y = 0; y < newPps; y++) {
                var x = lastIndex;
                var a = points[y * currentPps + x - 1];
                var b = points[y * currentPps + x];
                copy[y * newPps + x] = Vector3.Lerp(a, b, t);
            }

            // Lerp corner
            copy[lastIndex * newPps + lastIndex] = QuadLerp(croppedSize, croppedSize);

            return new ContinuousGrid(copy, finalSize);
        }
        #endregion


        #region Triangles
        int[] DefineTriangles(bool bothSides) {
            var size = Size;
            var pointsPerSide = PointsPerSide;
            var pointsCount = pointsPerSide * pointsPerSide;
            var trianglesPerSquare = bothSides ? 4 : 2;
            var edgesPerSquare = trianglesPerSquare * 3;
            var triangles = new int[size * size * edgesPerSquare];
            var v = 0;
            var t = 0;

            // setting each square's triangles
            for (var x = 0; x < size; x++) {
                for (var y = 0; y < size; y++) {
                    // first triangle
                    triangles[t] = v;
                    triangles[t + 1] = v + pointsPerSide;
                    triangles[t + 2] = v + pointsPerSide + 1;

                    // second triangle
                    triangles[t + 3] = v;
                    triangles[t + 4] = v + pointsPerSide + 1;
                    triangles[t + 5] = v + 1;

                    if (bothSides) {
                        // first triangle back
                        triangles[t + 6] = pointsCount + triangles[t];
                        triangles[t + 7] = pointsCount + triangles[t + 2];
                        triangles[t + 8] = pointsCount + triangles[t + 1];

                        // second triangle back
                        triangles[t + 9] = pointsCount + triangles[t + 3];
                        triangles[t + 10] = pointsCount + triangles[t + 5];
                        triangles[t + 11] = pointsCount + triangles[t + 4];
                    }

                    t += edgesPerSquare;
                    v++;
                }
                v++;
            }

            return triangles;
        }
        #endregion


        #region Helpers
        static float GetSizeFromArray(Vector3[] vertices) => Mathf.Sqrt(vertices.Length);

        static Vector3[] DuplicatedList(Vector3[] list) {
            var copy = new Vector3[list.Length * 2];
            list.CopyTo(copy, 0);
            list.CopyTo(copy, list.Length);
            return copy;
        }

        Vector3 QuadLerp(float row, float col) {
            var top = Mathf.FloorToInt(row);
            var bottom = Mathf.CeilToInt(row);
            var left = Mathf.FloorToInt(col);
            var right = Mathf.CeilToInt(col);

            var topLeft = Points[top * PointsPerSide + left];
            var topRight = Points[top * PointsPerSide + right];
            var bottomLeft = Points[bottom * PointsPerSide + left];
            var bottomRight = Points[bottom * PointsPerSide + right];

            var tx = col.GetDecimals();
            var ty = row.GetDecimals();

            var a = Vector3.Lerp(topLeft, topRight, tx);
            var b = Vector3.Lerp(bottomLeft, bottomRight, tx);

            return Vector3.Lerp(a, b, ty);
        }
        #endregion
    }
}
