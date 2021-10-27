namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class FollowerSpeedModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class SpeedKey : Key
        {
            public float speed = 0f;

            public SpeedKey(double f, double t, FollowerSpeedModifier modifier) : base(f, t, modifier)
            {
            }
        }
        public List<SpeedKey> keys = new List<SpeedKey>();

        public FollowerSpeedModifier()
        {
            keys = new List<SpeedKey>();
        }

        public override List<Key> GetKeys()
        {
            List<Key> output = new List<Key>();
            for (int i = 0; i < keys.Count; i++)
            {
                output.Add(keys[i]);
            }
            return output;
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new List<SpeedKey>();
            for (int i = 0; i < input.Count; i++)
            {
                input[i].modifier = this;
                keys.Add((SpeedKey)input[i]);
            }
        }

        public void AddKey(double f, double t)
        {
            keys.Add(new SpeedKey(f, t, this));
        }

        public override void Apply(SplineSample result)
        {
        }

        public float GetSpeed(SplineSample sample)
        {
            float speed = 0f;
            for (int i = 0; i < keys.Count; i++)
            {
                float lerp = keys[i].Evaluate(sample.percent);
                speed += keys[i].speed * lerp;
            }
            return speed;
        }
    }
}
