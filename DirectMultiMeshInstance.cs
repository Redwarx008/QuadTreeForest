using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class DirectMultiMeshInstance : IDisposable
{
    private Rid _instanceRid;

    public MultiMesh MultiMesh
    {
        get => _multiMesh;
        set
        {
            _multiMesh = value;
            RenderingServer.InstanceSetBase(_instanceRid, _multiMesh.GetRid());
        }
    }
    public Transform3D GlobalTransform
    {
        get => _globalTransform;
        set
        {
            _globalTransform = value;
            RenderingServer.InstanceSetTransform(_instanceRid, _globalTransform);
        }
    }
    private Transform3D _globalTransform;

    public Material? MaterialOverride
    {
        get => _materialOverride;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            _materialOverride = value;
            RenderingServer.InstanceGeometrySetMaterialOverride(_instanceRid, value.GetRid());
        }
    }
    private Material? _materialOverride;

    public Aabb Aabb
    {
        get => _aabb;
        set
        {
            _aabb = value;
            RenderingServer.InstanceSetCustomAabb(_instanceRid, value);
        }
    }
    private Aabb _aabb;

    public bool Visible
    {
        get => _visible;
        set
        {
            RenderingServer.InstanceSetVisible(_instanceRid, value);
            _visible = value;
        }
    }
    private bool _visible;

    private MultiMesh _multiMesh;
    public DirectMultiMeshInstance(World3D world, MultiMesh multiMesh)
    {
        _instanceRid = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetScenario(_instanceRid, world.Scenario);
        RenderingServer.InstanceSetVisible(_instanceRid, true);

        _multiMesh = multiMesh;
        RenderingServer.InstanceSetBase(_instanceRid, _multiMesh.GetRid());
    }

    void IDisposable.Dispose()
    {
        RenderingServer.FreeRid(_instanceRid);
    }
}
