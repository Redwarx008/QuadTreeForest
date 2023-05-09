using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class CullingHelper
{
    private static float MinDistanceFromPointToAabbSquare(in Vector3 boundsMin, in Vector3 boundsMax, in Vector3 point)
    {
        float dist = 0f;

        if (point.X < boundsMin.X)
        {
            float d = point.X - boundsMin.X;
            dist += d * d;
        }
        else if (point.X > boundsMax.X)
        {
            float d = point.X - boundsMax.X;
            dist += d * d;
        }
        if (point.Z < boundsMin.Z)
        {
            float d = point.Z - boundsMin.Z;
            dist += d * d;
        }
        else if (point.Z > boundsMax.Z)
        {
            float d = point.Z - boundsMax.Z;
            dist += d * d;
        }
        if (point.Y < boundsMin.Y)
        {
            float d = point.Y - boundsMin.Y;
            dist += d * d;
        }
        else if (point.Y > boundsMax.Y)
        {
            float d = point.Y - boundsMax.Y;
            dist += d * d;
        }
        return dist;
    }
    public static bool IsAABBIntersectSphere(in Vector3 boundsMin, in Vector3 boundsMax,
        in Vector3 observerPos, float radius)
    {
        return MinDistanceFromPointToAabbSquare(boundsMin, boundsMax, observerPos) <= radius * radius;
    }
}
