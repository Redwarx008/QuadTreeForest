using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class Main : Node3D
{
	ForestQuadTree _quadTree = null!;

	Dictionary<ForestType, Image> _forestMask = new();

	Dictionary<ForestType, Mesh> _treeMask = new();

	private List<ForestQuadTree.Node> _pendingNodes = new();

	private List<ForestQuadTree.Node> _lastPendingNodes = new();
	public override void _Ready()
	{
		Image pineMask = Image.LoadFromFile("Asset/mask.png");
		_forestMask[ForestType.Pine] = pineMask;
		_treeMask[ForestType.Pine] = new BoxMesh()
		{
			Size = new Vector3(1, 1, 1)
		};
		ForestQuadTree.CreateDesc createDesc = new(_forestMask, _treeMask, GetWorld3D());
		_quadTree = new ForestQuadTree(createDesc);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach(var node in _lastPendingNodes)
		{
			node.Visiable = false;
		}
		_lastPendingNodes.Clear();

		_quadTree.SelectLeaves(GetViewport().GetCamera3D(), _pendingNodes);

		foreach(var node in _pendingNodes)
		{
			node.Visiable = true;
		}
		_lastPendingNodes.AddRange(_pendingNodes);
		_pendingNodes.Clear();	
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_quadTree.Dispose();	
	}
}
