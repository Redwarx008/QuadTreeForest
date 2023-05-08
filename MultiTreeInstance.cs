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
    public void SetPositions(IEnumerable<Vector3> positions)
    {
        int pointCount = positions.Count();

    }

    void IDisposable.Dispose()
    {
        throw new NotImplementedException();
    }
}
