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

        public Dictionary<ForestType, List<Vector2>> TreePositions { get; private set; }

        public float ViewDistance { get; set; } = 3000;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public World3D World { get; private set; }

        public CreateDesc(Dictionary<ForestType, Image> forestMask,
            Dictionary<ForestType, Mesh> treeMesh, Dictionary<ForestType, 
                List<Vector2>> treePositions, World3D world, int mapWidth, int mapHeight)
        {
            ForestMask = forestMask;
            TreeMesh = treeMesh;
            TreePositions = treePositions;
            World = world;
            Width = mapWidth;
            Height = mapHeight;
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

        // Only leaf nodes have it.
        private Dictionary<ForestType, MultiTreeInstance>? _treeInstances;
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
                _treeInstances = new();

                foreach (var forestMask in _mask)
                {
                    MultiTreeInstance treeInstance = new(createDesc.World, createDesc.TreeMesh[forestMask.Key]);
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
        public void SetTreesPositions(ForestType type, List<Vector3> treesPos)
        {
            Debug.Assert(_treeInstances != null);
            _treeInstances[type].SetPositions(treesPos);
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

        // Assign the position of each tree to each chunk
        foreach (var forestPosPair in createDesc.TreePositions)
        {
            Dictionary<Vector2I, List<Vector3>> positionsPerChunk = new();
            List<Vector2> treePos2D = forestPosPair.Value;
            for(int i = 0; i < treePos2D.Count; ++i)
            {
                Vector2 p2D = treePos2D[i];
                Vector3 p3D = new(p2D.X, 0, p2D.Y);

                int chunkPosX = (int)p2D.X / ChunkSize;
                int chunkPosY = (int)p2D.Y / ChunkSize;
                Vector2I chunkPos = new Vector2I(chunkPosX, chunkPosY);
                if (!positionsPerChunk.ContainsKey(chunkPos))
                {
                    positionsPerChunk[chunkPos] = new();
                }
                positionsPerChunk[chunkPos].Add(p3D);
            }

            foreach(var chunkPosPair in positionsPerChunk)
            {
                _bottomNodes[chunkPosPair.Key.X, chunkPosPair.Key.Y].SetTreesPositions(
                    forestPosPair.Key, chunkPosPair.Value);
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
