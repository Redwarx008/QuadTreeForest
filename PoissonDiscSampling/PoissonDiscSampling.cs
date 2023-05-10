using System.Collections.Concurrent;
using Godot;

namespace PoissonDiscSampling
{
    public static class PoissonDiscSamplingHelper
    {
        public static int BlockSize { get; set; } = 32;

        public static List<Vector2> GeneratePoints(float radius, Rect2I region, int attemptNum = 30, Image mask = null)
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> pendingPoints = new List<Vector2>();

            float cellSize = radius / MathF.Sqrt(2);
            int[,] grid = new int[(int)MathF.Ceiling(region.Size.X / cellSize), (int)MathF.Ceiling(region.Size.Y / cellSize)];

            // find the first valid point
            if (mask != null)
            {
                for (int y = region.Position.Y; y < region.Position.Y + region.Size.Y; ++y)
                {
                    for (int x = region.Position.X; x < region.Position.X + region.Size.X; ++x)
                    {
                        if (mask.GetPixel(x, y).R8 != 0)
                        {
                            pendingPoints.Add(new Vector2(x, y));
                        }
                    }
                }
            }
            else
            {
                pendingPoints.Add(new Vector2(region.Position.X + region.Size.X / 2, region.Position.Y + region.Size.Y / 2));
            }


            Random rnd = new Random();
            while (pendingPoints.Count > 0)
            {
                //int pendingIndex = rnd.Next(pendingPoints.Count);
                int pendingIndex = 0;
                Vector2 pendingCenter = pendingPoints[pendingIndex];

                bool isValid = false;

                for (int i = 0; i < attemptNum; ++i)
                {
                    float angle = rnd.NextSingle() * MathF.PI * 2;
                    Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 pendingPoint = pendingCenter + dir * (rnd.NextSingle() * 2 * radius + radius);
                    if (IsValidPoint(pendingPoint, region, cellSize, radius, points, grid, mask))
                    {
                        points.Add(pendingPoint);
                        pendingPoints.Add(pendingPoint);
                        grid[(int)((pendingPoint.X - region.Position.X) / cellSize),
                            (int)((pendingPoint.Y - region.Position.Y) / cellSize)] = points.Count;
                        isValid = true;
                        break;
                    }
                }
                if (!isValid)
                {
                    pendingPoints.RemoveAt(pendingIndex);
                }
            }
            return points;
        }

        public static Vector2[] GeneratePointsParallel(float radius, Rect2I region, int attemptNum = 30, Image mask = null)
        {
            int regionXEnd = region.Position.X + region.Size.X;
            int regionYEnd = region.Position.Y + region.Size.Y;

            int blockXCount = (int)MathF.Ceiling((float)region.Size.X / BlockSize);
            int blockYCount = (int)MathF.Ceiling((float)region.Size.Y / BlockSize);

            Rect2I[] regions = new Rect2I[blockXCount * blockYCount];

            int count = 0;
            for (int y = region.Position.Y; y < regionYEnd; y += BlockSize)
            {
                for (int x = region.Position.X; x < regionXEnd; x += BlockSize)
                {
                    int subRegionWidth = Math.Min(BlockSize, regionXEnd - x);
                    int subRegionHeight = Math.Min(BlockSize, regionYEnd - y);
                    Rect2I subRegion = new(x, y, subRegionWidth, subRegionHeight);
                    regions[count] = subRegion;
                    ++count;
                }
            }

            List<Vector2>[] pointLists = new List<Vector2>[blockXCount * blockYCount];

            Parallel.For(0, regions.Length, i =>
            {
                List<Vector2> subPoints = GeneratePointsParallelPartial(radius, i, blockXCount, blockYCount, regions[i], attemptNum, mask);
                pointLists[i] = subPoints;
            });

            int pointCount = 0;
            foreach(var list in pointLists)
            {
                pointCount += list.Count;
            }

            Vector2[] points = new Vector2[pointCount];
            count = 0;
            foreach (var list in pointLists)
            {
                for(int i = 0; i < list.Count; ++i)
                {
                    points[count++] = list[i];
                }
            }
            return points;
        }
        private static List<Vector2> GeneratePointsParallelPartial(float radius, int index, int blockXCount, int blockYCount,
            Rect2I region, int attemptNum = 30, Image mask = null)
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> pendingPoints = new List<Vector2>();

            float cellSize = radius / MathF.Sqrt(2);

            // In order not to overlap at the border.
            int gridWidth = index % blockXCount != blockXCount - 1 ? (int)MathF.Ceiling(region.Size.X / cellSize) - 1 : (int)MathF.Ceiling(region.Size.X / cellSize);
            int gridHeight = index / blockXCount != blockYCount - 1 ? (int)MathF.Ceiling(region.Size.Y / cellSize) - 1 : (int)MathF.Ceiling(region.Size.Y / cellSize);
            int[,] grid = new int[gridWidth, gridHeight];

            // find the first valid point
            if (mask != null)
            {
                for (int y = region.Position.Y; y < region.Position.Y + region.Size.Y; ++y)
                {
                    for (int x = region.Position.X; x < region.Position.X + region.Size.X; ++x)
                    {
                        if (mask.GetPixel(x, y).R8 != 0)
                        {
                            pendingPoints.Add(new Vector2(x, y));
                        }
                    }
                }
            }
            else
            {
                pendingPoints.Add(new Vector2(region.Position.X + region.Size.X / 2, region.Position.Y + region.Size.Y / 2));
            }


            Random rnd = new Random();
            while (pendingPoints.Count > 0)
            {
                //int pendingIndex = rnd.Next(pendingPoints.Count);
                int pendingIndex = 0;
                Vector2 pendingCenter = pendingPoints[pendingIndex];

                bool isValid = false;

                for (int i = 0; i < attemptNum; ++i)
                {
                    float angle = rnd.NextSingle() * MathF.PI * 2;
                    Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 pendingPoint = pendingCenter + dir * (rnd.NextSingle() * 2 * radius + radius);
                    if (IsValidPoint(pendingPoint, region, cellSize, radius, points, grid, mask))
                    {
                        points.Add(pendingPoint);
                        pendingPoints.Add(pendingPoint);
                        grid[(int)((pendingPoint.X - region.Position.X) / cellSize),
                            (int)((pendingPoint.Y - region.Position.Y) / cellSize)] = points.Count;
                        isValid = true;
                        break;
                    }
                }
                if (!isValid)
                {
                    pendingPoints.RemoveAt(pendingIndex);
                }
            }
            return points;
        }
        private static bool IsValidPoint(in Vector2 pendingPoint, in Rect2I region, float cellSize,
            float radius, in List<Vector2> points, in int[,] grid, Image mask = null)
        {
            if (pendingPoint.X < region.Position.X || pendingPoint.Y < region.Position.Y)
            {
                return false;
            }
            if (pendingPoint.X > region.Position.X + region.Size.X || pendingPoint.Y > region.Position.Y + region.Size.Y)
            {
                return false;
            }
            if (mask != null && mask.GetPixel((int)pendingPoint.X, (int)pendingPoint.Y).R8 == 0)
            {
                return false;
            }

            int cellX = (int)((pendingPoint.X - region.Position.X) / cellSize);
            int cellY = (int)((pendingPoint.Y - region.Position.Y) / cellSize);

            int gridXCount = grid.GetLength(0);
            int gridYCount = grid.GetLength(1);
            if (cellX > gridXCount - 1 || cellY > gridYCount - 1)
            {
                return false;
            }

            int boundStartX = 0;
            int boundEndX = gridXCount - 1;
            int boundStartY = 0;
            int boundEndY = gridYCount - 1;

            int searchStartX = Math.Max(boundStartX, cellX - 2);
            int searchEndX = Math.Min(cellX + 2, boundEndX);
            int searchStartY = Math.Max(boundStartY, cellY - 2);
            int searchEndY = Math.Min(cellY + 2, boundEndY);

            for (int y = searchStartY; y <= searchEndY; ++y)
            {
                for (int x = searchStartX; x <= searchEndX; ++x)
                {
                    int pointIndex = grid[x, y] - 1;
                    // if there are other points here
                    if (pointIndex != -1)
                    {
                        float sqrDst = (pendingPoint - points[pointIndex]).LengthSquared();
                        if (radius * radius > sqrDst)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}