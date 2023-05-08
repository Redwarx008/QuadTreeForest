using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class MultiTreeInstance : IDisposable
{
    private DirectMultiMeshInstance _multiMeshInstance;

    private MultiMesh _multiMesh;
    public MultiTreeInstance(World3D world, Mesh treeMesh)
    {
        _multiMesh = new MultiMesh()
        {
            Mesh = treeMesh
        };
        _multiMeshInstance = new(world, _multiMesh);
    }
    public void SetPositions(List<Vector3> positions)
    {
        int pointCount = positions.Count;
        _multiMesh.InstanceCount = pointCount;
        for(int i = 0; i < pointCount; ++i)
        {
            Transform3D transform = new(Basis.Identity, positions[i]);
            _multiMesh.SetInstanceTransform(i, transform);  
        }
    }
    public void SetMaterial(Material material)
    {
        _multiMeshInstance.MaterialOverride = material; 
    }

    public void Dispose()
    {
        _multiMeshInstance.Dispose();
    }
}
