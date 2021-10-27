namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class ColorModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class ColorKey : Key
        {
            public enum BlendMode { Lerp, Multiply, Add, Subtract }
            public Color color = Color.white;
            public BlendMode blendMode = BlendMode.Lerp;

            public ColorKey(double f, double t, ColorModifier modifier) : base(f, t, modifier)
            {
            }

            public Color Blend(Color input, float percent)
            {
                switch (blendMode)
                {
                    case BlendMode.Lerp: return Color.Lerp(input, color, blend * percent);
                    case BlendMode.Add: return input + color * blend * percent;
                    case BlendMode.Subtract: return input - color * blend * percent;
                    case BlendMode.Multiply: return Color.Lerp(input, input * color, blend * percent);
                    default: return input;
                }
            }
        }
        public List<ColorKey> keys = new List<ColorKey>();

        public ColorModifier()
        {
            keys = new List<ColorKey>();
        }

        public override List<Key> GetKeys()
        {
            List<Key> output = new List<Key>();
            for (int i = 0; i < keys.Count; i++) output.Add(keys[i]);
            return output;
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new List<ColorKey>();
            for (int i = 0; i < input.Count; i++) keys.Add((ColorKey)input[i]);
            base.SetKeys(input);
        }

        public void AddKey(double f, double t)
        {
            keys.Add(new ColorKey(f, t, this));
        }

        public override void Apply(SplineSample result)
        {
            if (keys.Count == 0) return;
            base.Apply(result);
            for (int i = 0; i < keys.Count; i++)
            {
                result.color = keys[i].Blend(result.color, keys[i].Evaluate(result.percent));
            }
        }
    }
}
