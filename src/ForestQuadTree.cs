using Godot;
using PoissonDiscSampling;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

internal enum ForestType : byte
{
    Pine
}
internal class ForestQuadTree : IDisposable
{
    internal static int ChunkSize { get; private set; } = 64;
    internal static int LevelCount { get; private set; }
    internal class CreateDesc
    {
        public Dictionary<ForestType, Image> ForestMask { get; private set; }

        public Dictionary<ForestType, Mesh> TreeMesh { get; private set; }

        public float ViewDistance { get; private set; } = 3000;

        public int Width { get; private set; } = 2048;

        public int Height { get; private set; } = 2048;

        public World3D World { get; private set; }

        public CreateDesc(Dictionary<ForestType, Image> forestMask,
            Dictionary<ForestType, Mesh> treeMesh, World3D world)
        {
            ForestMask = forestMask;
            TreeMesh = treeMesh;
            World = world;
        }
    }
    public class Node
    {
        public List<Node>? Children { private set; get; } = null;

        public short X { private set; get; }

        public short Y { private set; get; }

        public short Size { private set; get; }

        public float MinHeight { private set; get; }

        public float MaxHeight { private set; get; }

        public bool Visiable
        {
            get => _visiable;
            set
            {
                Debug.Assert(_treeInstances != null);
                _visiable = value;
                foreach(var forest in _treeInstances)
                {
                    forest.Value.Visiable = _visiable;
                }
            }
        }
        private bool _visiable = false;

        private Dictionary<ForestType, Rect2>? _forestRegion = null;

        private Dictionary<ForestType, List<Vector2>>? _treePositions = null;

        private Dictionary<ForestType, MultiTreeInstance>? _treeInstances = null;

        public Node(in CreateDesc createDesc, int x, int y, int size, in Node[,] bottomNodes)
        {
            X = (short)x;
            Y = (short)y;
            Size = (short)size;

            int heightMapSizeX = createDesc.Width;
            int heightMapSizeY = createDesc.Height;

            if (Size == ChunkSize)
            {
                // todo: assign a value to MinHeight and MaxHeight.

                _forestRegion = new();
                _treePositions = new();
                _treeInstances = new();

                foreach (var forestMask in _mask)
                {
                    List<Vector2> points = PoissonDiscSamplingHelper.GeneratePointsParallel(2,
                        new Rect2I(X, Y, Size, Size), mask: forestMask.Value);

                    float xMin = float.MaxValue;
                    float xMax = float.MinValue;
                    float yMin = float.MaxValue;
                    float yMax = float.MinValue;

                    List<Vector3> points3D = new(points.Count);
                    for (int i = 0; i < points.Count; i++)
                    {
                        var p = points[i];
                        xMin = MathF.Min(p.X, xMin);
                        xMax = MathF.Max(p.X, xMax);
                        yMin = MathF.Min(p.Y, yMin);
                        yMax = MathF.Max(p.Y, yMax);
                        points3D.Add(new Vector3(p.X, 0, p.Y));
                    }
                    MultiTreeInstance treeInstance = new(createDesc.World, createDesc.TreeMesh[forestMask.Key]);
                    treeInstance.SetPositions(points3D);
                    _treeInstances[forestMask.Key] = treeInstance;
                }

                bottomNodes[X / ChunkSize, Y / ChunkSize] = this;
            }
            else
            {
                int subSize = size / 2;
                Node subTopLeft = new(createDesc, x, y, subSize, bottomNodes);
                MinHeight = subTopLeft.MinHeight;
                MaxHeight = subTopLeft.MaxHeight;

                Children = new()
                {
                    subTopLeft
                };
                if (x + subSize < heightMapSizeX)
                {
                    Node subTopRight = new(createDesc, x + subSize, y, subSize, bottomNodes);
                    MinHeight = MathF.Min(MinHeight, subTopRight.MinHeight);
                    MaxHeight = MathF.Max(MaxHeight, subTopRight.MaxHeight);
                    Children.Add(subTopRight);  
                }

                if (y + subSize < heightMapSizeY)
                {
                    Node subButtomLeft = new(createDesc, x, y + subSize, subSize, bottomNodes);
                    MinHeight = MathF.Min(MinHeight, subButtomLeft.MinHeight);
                    MaxHeight = MathF.Max(MaxHeight, subButtomLeft.MaxHeight);
                    Children.Add(subButtomLeft);
                }

                if (x + subSize < heightMapSizeX && y + subSize < heightMapSizeY)
                {
                    Node subButtomRight = new(createDesc, x + subSize, y + subSize, subSize, bottomNodes);
                    MinHeight = MathF.Min(MinHeight, subButtomRight.MinHeight);
                    MaxHeight = MathF.Max(MaxHeight, subButtomRight.MaxHeight); 
                    Children.Add(subButtomRight);
                }
            }
        }
        public void Select(in Vector3 cameraPos, in List<Node> pendingNodes)
        {
            Vector3 boundsMin = new(X, MinHeight, Y);
            Vector3 boundsMax = new(X + Size, MaxHeight, Y + Size);

            if (!CullingHelper.IsAABBIntersectSphere(boundsMin, boundsMax, cameraPos, _viewDistance))
            {
                return;
            }

            if (Children == null)
            {
                pendingNodes.Add(this);
            }
            else
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].Select(cameraPos, pendingNodes);
                }
            }
        }
        public void FreeMeshInstance()
        {
            Debug.Assert(_treeInstances != null);
            foreach(var forest in _treeInstances)
            {
                forest.Value.Dispose();
            }
        }
    }

    private Node[,] _bottomNodes;

    private Node[,] _topNodes;

    private static Dictionary<ForestType, List<Vector3>> treePositions = null!;

    private static Dictionary<ForestType, Image> _mask = null!;

    private static float _viewDistance = 1000;
    public ForestQuadTree(in CreateDesc createDesc)
    {
        _mask = createDesc.ForestMask;

        int mapWidth = createDesc.Width;
        int mapHeight = createDesc.Height;

        int minLength = Math.Min(mapWidth, mapHeight);
        LevelCount = (int)Math.Log2(minLength) - (int)Math.Log2(ChunkSize);
        int topNodeSize = ChunkSize * (int)MathF.Pow(2, LevelCount - 1);

        int topNodeCountX = (mapWidth - 1 + topNodeSize - 1) / topNodeSize;
        int topNodeCountY = (mapHeight - 1 + topNodeSize - 1) / topNodeSize;

        _topNodes = new Node[topNodeCountX, topNodeCountY];

        _bottomNodes = new Node[(int)MathF.Ceiling((float)mapWidth / ChunkSize), (int)MathF.Ceiling((float)mapWidth / ChunkSize)];

        for (int y = 0; y < topNodeCountY; ++y)
        {
            for (int x = 0; x < topNodeCountX; ++x)
            {
                _topNodes[x, y] = new Node(createDesc, x * topNodeSize, y * topNodeSize, topNodeSize, _bottomNodes);
            }
        }
    }

    public void SelectLeaves(in Camera3D camera, in List<Node> pendingNodes)
    {
        Vector3 cameraPos = camera.GlobalPosition;
        foreach(var node in _topNodes)
        {
            node.Select(cameraPos, pendingNodes); 
        }
    }

    public void Dispose()
    {
        foreach(var node in _bottomNodes)
        {
            node.FreeMeshInstance();
        }
    }
}
