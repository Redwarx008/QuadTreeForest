using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class ForestQuadTree
{
    internal static int ChunkSize { get; private set; } = 64;
    internal enum ForestType : byte
    {
        Pine
    }
    private class Node
    {
        public List<Node>? Children { private set; get; } = null;

        public short X { private set; get; }

        public short Y { private set; get; }

        public short Size { private set; get; }

        private Dictionary<ForestType, Rect2>? _forestRegion = null;

        private Dictionary<ForestType, List<Vector2>>? _treePositions = null;

        private Dictionary<ForestType, MultiTreeInstance>? _treeInstances = null;

        public Node(int x, int y, int size, in List<Node> allNodes)
        {
            X = (short)x;
            Y = (short)y;
            Size = (short)Size;
        }
    }

    private List<Node> _allNodes = new();

    public ForestQuadTree()
    {

    }
}
