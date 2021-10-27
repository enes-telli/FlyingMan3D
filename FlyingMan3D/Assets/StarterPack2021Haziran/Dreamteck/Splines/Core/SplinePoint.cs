using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace Dreamteck.Splines{
	[System.Serializable]
    //A control point used by the SplineClass
	public struct SplinePoint{
        public enum Type {SmoothMirrored, Broken, SmoothFree};
        public Type type
        {
            get { return _type; }
            set
            {
                _type = value;
                if(value == Type.SmoothMirrored) SmoothMirrorTangent2();
            }
        }
        [FormerlySerializedAs("type")]
        [SerializeField]
        [HideInInspector]
        private Type _type;
        public Vector3 position;
        public Color color;
        public Vector3 normal;
        public float size;
        public Vector3 tangent;
        public Vector3 tangent2;

        public static SplinePoint Lerp(SplinePoint a, SplinePoint b, float t)
        {
            SplinePoint result = a;
            if (a.type == Type.Broken || b.type == Type.Broken) result.type = Type.Broken;
            else if (a.type == Type.SmoothFree || b.type == Type.SmoothFree) result.type = Type.SmoothFree;
            else result.type = Type.SmoothMirrored;
            result.position = Vector3.Lerp(a.position, b.position, t);
            GetInterpolatedTangents(a, b, t, out result.tangent, out result.tangent2);
            result.color = Color.Lerp(a.color, b.color, t);
            result.size = Mathf.Lerp(a.size, b.size, t);
            result.normal = Vector3.Slerp(a.normal, b.normal, t);
            return result;
        }

        static void GetInterpolatedTangents(SplinePoint a, SplinePoint b, float t, out Vector3 t1, out Vector3 t2)
        {
            Vector3 P0_1 = (1f - t) * a.position + t * a.tangent2;
            Vector3 P1_2 = (1f - t) * a.tangent2 + t * b.tangent;
            Vector3 P2_3 = (1f - t) * b.tangent + t * b.position;
            Vector3 P01_12 = (1 - t) * P0_1 + t * P1_2;
            Vector3 P12_23 = (1 - t) * P1_2 + t * P2_3;
            t1 = P01_12;
            t2 = P12_23;
        }

        public static bool AreDifferent(ref SplinePoint a, ref SplinePoint b)
        {
            if (a.position != b.position) return true;
            if (a.tangent != b.tangent) return true;
            if (a.tangent2 != b.tangent2) return true;
            if (a.normal != b.normal) return true;
            if (a.color != b.color) return true;
            if (a.size != b.size) return true;
            if (a.type != b.type) return true;
            return false;
        }

        public void SetPosition(Vector3 pos)
        {
            tangent -= position - pos;
            tangent2 -= position - pos;
            position = pos;
        }

        public void SetTangentPosition(Vector3 pos)
        {
            tangent = pos;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public void SetTangent2Position(Vector3 pos)
        {
            tangent2 = pos;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent(); break;
                case Type.SmoothFree: SmoothFreeTangent(); break;
            }
        }

        public SplinePoint(Vector3 p)
        {
            position = p;
            tangent = p;
            tangent2 = p;
            color = Color.white;
            normal = Vector3.up;
            size = 1f;
            _type = Type.SmoothMirrored;
            SmoothMirrorTangent2();
        }
		
		public SplinePoint(Vector3 p, Vector3 t){
			position = p;
			tangent = t;
            tangent2 = p + (p - t);
            color = Color.white;
            normal = Vector3.up;
            size = 1f;
            _type = Type.SmoothMirrored;
            SmoothMirrorTangent2();
        }	
		
		public SplinePoint(Vector3 pos, Vector3 tan, Vector3 nor, float s, Color col){
			position = pos;
			tangent = tan;
            tangent2 = pos + (pos - tan);
			normal = nor;
			size = s;
			color = col;
            _type = Type.SmoothMirrored;
            SmoothMirrorTangent2();
        }

        public SplinePoint(Vector3 pos, Vector3 tan, Vector3 tan2, Vector3 nor, float s, Color col)
        {
            position = pos;
            tangent = tan;
            tangent2 = tan2;
            normal = nor;
            size = s;
            color = col;
            _type = Type.Broken;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public SplinePoint(SplinePoint source)
        {
            position = source.position;
            tangent = source.tangent;
            tangent2 = source.tangent2;
            color = source.color;
            normal = source.normal;
            size = source.size;
            _type = source.type;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        private void SmoothMirrorTangent2()
        {
            tangent2 = position + (position - tangent);
        }

        private void SmoothMirrorTangent()
        {
            tangent = position + (position - tangent2);
        }

        private void SmoothFreeTangent2()
        {
            tangent2 = position + (position - tangent).normalized * (tangent2 - position).magnitude;
        }

        private void SmoothFreeTangent()
        {
            tangent = position + (position - tangent2).normalized * (tangent - position).magnitude;
        }

    }
}
