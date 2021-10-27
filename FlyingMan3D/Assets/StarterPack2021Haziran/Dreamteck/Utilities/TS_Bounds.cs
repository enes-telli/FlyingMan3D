using UnityEngine;
using System.Collections;

namespace Dreamteck
{
    [System.Serializable]
    public class TS_Bounds
    {
        public Vector3 center = Vector3.zero;
        public Vector3 extents = Vector3.zero;
        public Vector3 max = Vector3.zero;
        public Vector3 min = Vector3.zero;
        public Vector3 size = Vector3.zero;

        public TS_Bounds()
        {

        }

        public TS_Bounds(Bounds bounds)
        {
            center = bounds.center;
            extents = bounds.extents;
            max = bounds.max;
            min = bounds.min;
            size = bounds.size;
        }

        public TS_Bounds(Vector3 c, Vector3 s)
        {
            center = c;
            size = s;
            extents = s / 2;
            max = center + extents;
            min = center - extents;
        }

        public TS_Bounds(Vector3 min, Vector3 max, Vector3 center)
        {
            size = new Vector3(max.x - min.x, max.y - min.y, max.z - min.z);
            extents = size / 2f;
            this.min = min;
            this.max = max;
            this.center = center;
        }

        public void CreateFromMinMax(Vector3 min, Vector3 max)
        {
            size.x = max.x - min.x;
            size.y = max.y - min.y;
            size.z = max.z - min.z;
            extents = size / 2f;
            this.min = min;
            this.max = max;
            center = (Vector3.Lerp(min, max, 0.5f));
        }

        public bool Contains(Vector3 point)
        {
            if (point.x < min.x || point.x > max.x) return false;
            if (point.y < min.y || point.y > max.y) return false;
            if (point.z < min.z || point.z > max.z) return false;
            return true;
        }
    }
}