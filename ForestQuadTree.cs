using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class ForestQuadTree
{
    internal enum ForestType : byte
    {
        Pine
    }
    private class Node
    {
        public List<Node> Children { private set; get; } = null;

        public short X { private set; get; }

        public short Y { private set; get; }

        public short Size { private set; get; }

        private Dictionary<ForestType, Rect2> _forestRegion = null;

        private Dictionary<ForestType, List<Vector2>> _treePositions = null;

        private Dic
    }
}
