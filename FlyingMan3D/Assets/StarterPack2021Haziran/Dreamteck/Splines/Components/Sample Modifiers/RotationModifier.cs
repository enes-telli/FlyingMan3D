using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines
{
    [System.Serializable]
    public class RotationModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class RotationKey : Key
        {
            public bool useLookTarget = false;
            public Transform target = null;
            public Vector3 rotation = Vector3.zero;

            public RotationKey(Vector3 rotation, double f, double t, RotationModifier modifier) : base(f, t, modifier)
            {
                this.rotation = rotation;
            }
        }

        public List<RotationKey> keys = new List<RotationKey>();

        public RotationModifier()
        {
            keys = new List<RotationKey>();
        }

        public override List<Key> GetKeys()
        {
            List<Key> output = new List<Key>();
            for (int i = 0; i < keys.Count; i++) output.Add(keys[i]);
            return output;
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new List<RotationKey>();
            for (int i = 0; i < input.Count; i++) keys.Add((RotationKey)input[i]);
            base.SetKeys(input);
        }

        public void AddKey(Vector3 rotation, double f, double t)
        {
            keys.Add(new RotationKey(rotation, f, t, this));
        }

        public override void Apply(SplineSample result)
        {
            if (keys.Count == 0) return;
            base.Apply(result);

            Quaternion offset = Quaternion.identity, look = result.rotation;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].useLookTarget && keys[i].target != null)
                {
                    Quaternion lookDir = Quaternion.LookRotation(keys[i].target.position - result.position);
                    look = Quaternion.Slerp(look, lookDir, keys[i].Evaluate(result.percent));
                }
                else
                {
                    Quaternion euler = Quaternion.Euler(keys[i].rotation.x, keys[i].rotation.y, keys[i].rotation.z);
                    offset = Quaternion.Slerp(offset, offset * euler, keys[i].Evaluate(result.percent));
                }
            }
            Quaternion rotation = look * offset;
            Vector3 invertedNormal = Quaternion.Inverse(result.rotation) * result.up;
            result.forward = rotation * Vector3.forward;
            result.up = rotation * invertedNormal;
        }
    }
}
