using UnityEngine;
using System.Collections;
using System.IO;

namespace Dreamteck
{
    public static class LinearAlgebraUtility
    {
        public static Vector3 ProjectOnLine(Vector3 fromPoint, Vector3 toPoint, Vector3 project)
        {
            Vector3 projectedPoint = Vector3.Project((project - fromPoint), (toPoint - fromPoint)) + fromPoint;
            Vector3 dir = toPoint - fromPoint;
            Vector3 projectedDir = projectedPoint - fromPoint;
            float dot = Vector3.Dot(projectedDir, dir);
            if(dot > 0f)
            {
                if(projectedDir.sqrMagnitude <= dir.sqrMagnitude) return projectedPoint;
                else return toPoint;
            } else return fromPoint;
        }

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            Vector3 ab = b - a;
            Vector3 av = value - a;
            return Vector3.Dot(av, ab) / Vector3.Dot(ab, ab);
        }

        public static float DistanceOnSphere(Vector3 from, Vector3 to, float radius)
        {
            float distance = 0;
            
            if (from == to)
            {
                distance = 0;
            }
            else if (from == -to)
            {
                distance = Mathf.PI * radius;
            }
            else
            {
                distance = Mathf.Sqrt(2) * radius * Mathf.Sqrt(1.0f - Vector3.Dot(from, to));
            }

            return distance;
        }
    }
}