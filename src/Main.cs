using Godot;
using PoissonDiscSampling;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[Tool]
public partial class Main : Node3D
{
	ForestQuadTree _quadTree = null!;

	Dictionary<ForestType, Image> _forestMaskDic = new();

	Dictionary<ForestType, Mesh> _treeMeshDic = new();

	private List<ForestQuadTree.Node> _pendingNodes = new();

	private List<ForestQuadTree.Node> _lastPendingNodes = new();

	private int _mapWidth;

	private int _mapHeight;	
	public override void _Ready()
	{
		Image pineMask = Image.LoadFromFile("Asset/mask.png");
		_mapWidth = pineMask.GetWidth();
		_mapHeight = pineMask.GetHeight();

		_forestMaskDic[ForestType.Pine] = pineMask;
		_treeMeshDic[ForestType.Pine] = new BoxMesh()
		{
			Size = new Vector3(1, 1, 1)
		};

		Stopwatch stopwatch = Stopwatch.StartNew();

		Dictionary<ForestType, List<Vector2>> treePos2D = new();
        var forestTypes = Enum.GetValues(typeof(ForestType));
        foreach (ForestType type in forestTypes)
        {
			Stopwatch stopwatch1 = Stopwatch.StartNew();

            List<Vector2> generatedPos = PoissonDiscSamplingHelper.GeneratePointsParallel(2,
                new Rect2I(0, 0, _mapWidth, _mapHeight), mask: _forestMaskDic[type]);
            treePos2D[type] = generatedPos;

			stopwatch1.Stop();
			GD.Print($"generate positions cost: {stopwatch1.ElapsedMilliseconds} ms");
        }

		Stopwatch stopwatch2 = Stopwatch.StartNew();

		ForestQuadTree.CreateDesc createDesc = new(_forestMaskDic, _treeMeshDic,
			treePos2D, GetWorld3D(), _mapWidth, _mapHeight)
		{
			ViewDistance = 1024
		};
		_quadTree = new ForestQuadTree(createDesc);

		stopwatch2.Stop();
		GD.Print($"build quad tree cost: {stopwatch2.ElapsedMilliseconds} ms");

		stopwatch.Stop();
		GD.Print($"build forest cost: {stopwatch.ElapsedMilliseconds} ms");

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach (var node in _lastPendingNodes)
		{
			node.Visiable = false;
		}
		_lastPendingNodes.Clear();

		_quadTree.SelectLeaves(GetViewport().GetCamera3D(), _pendingNodes);

		foreach (var node in _pendingNodes)
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
