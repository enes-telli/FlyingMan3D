using UnityEngine;
using Dreamteck;

namespace Dreamteck.Splines{
    [System.Serializable]
	public class SplineSample {
        public Vector3 position = Vector3.zero;
        public Vector3 up = Vector3.up;
        public Vector3 forward = Vector3.forward;
        public Color color = Color.white;
        public float size = 1f;
        public double percent = 0.0;

        public Quaternion rotation
        {
            get {
                if (up == forward)
                {
                    if (up == Vector3.up) return Quaternion.LookRotation(Vector3.up, Vector3.back);
                    else return Quaternion.LookRotation(forward, Vector3.up);
                }
                return Quaternion.LookRotation(forward, up); }
        }

        public Vector3 right
        {
            get {
                if(up == forward)
                {
                    if (up == Vector3.up) return Vector3.right;
                    else return Vector3.Cross(Vector3.up, forward).normalized;
                }
                return Vector3.Cross(up, forward).normalized; }
        }


        public static SplineSample Lerp(SplineSample a, SplineSample b, float t)
        {
            SplineSample result = new SplineSample();
            Lerp(a, b, t, result);
            return result;
        }

        public static SplineSample Lerp(SplineSample a, SplineSample b, double t)
        {
            SplineSample result = new SplineSample();
            Lerp(a, b, t, result);
            return result;
        }

        public static void Lerp(SplineSample a, SplineSample b, double t, SplineSample target)
        {
            float ft = (float)t;
            target.position = DMath.LerpVector3(a.position, b.position, t);
            target.forward = Vector3.Slerp(a.forward, b.forward, ft);
            target.up = Vector3.Slerp(a.up, b.up, ft);
            target.color = Color.Lerp(a.color, b.color, ft);
            target.size = Mathf.Lerp(a.size, b.size, ft);
            target.percent = DMath.Lerp(a.percent, b.percent, t);
        }

        public static void Lerp(SplineSample a, SplineSample b, float t, SplineSample target)
        {
            target.position = DMath.LerpVector3(a.position, b.position, t);
            target.forward = Vector3.Slerp(a.forward, b.forward, t);
            target.up = Vector3.Slerp(a.up, b.up, t);
            target.color = Color.Lerp(a.color, b.color, t);
            target.size = Mathf.Lerp(a.size, b.size, t);
            target.percent = DMath.Lerp(a.percent, b.percent, t);
        }

        public void Lerp(SplineSample b, double t)
        {
            Lerp(this, b, t, this);
        }

        public void Lerp(SplineSample b, float t)
        {
            Lerp(this, b, t, this);
        }

        public void CopyFrom(SplineSample input)
        {
            position = input.position;
            forward = input.forward;
            up = input.up;
            color = input.color;
            size = input.size;
            percent = input.percent;
        }

        public SplineSample()
        {
        }
		
        public SplineSample(Vector3 position, Vector3 normal, Vector3 direction, Color color, float size, double percent)
        {
            this.position = position;
            this.up = normal;
            this.forward = direction;
            this.color = color;
            this.size = size;
            this.percent = percent;
        }

        public SplineSample(SplineSample input)
        {
            position = input.position;
            up = input.up;
            forward = input.forward;
            color = input.color;
            size = input.size;
            percent = input.percent;
        }
	}
}
