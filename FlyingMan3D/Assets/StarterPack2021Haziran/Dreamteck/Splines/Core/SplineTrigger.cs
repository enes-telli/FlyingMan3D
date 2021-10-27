namespace Dreamteck.Splines
{
    using UnityEngine;
    using UnityEngine.Events;

    [System.Serializable]
    public class TriggerGroup{
#if UNITY_EDITOR
        public bool open = false;
#endif

        public bool enabled = true;
        public string name = "";
        public Color color = Color.white;
        public SplineTrigger[] triggers = new SplineTrigger[0];

        public void Check(double start, double end, SplineUser user = null)
        {
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null)
                {
                    continue;
                }

                if (triggers[i].Check(start, end))
                {
                    triggers[i].Invoke(user);
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < triggers.Length; i++) triggers[i].Reset();
        }
    }

    [System.Serializable]
    public class SplineTrigger
    {
        public string name = "Trigger";
        public enum Type { Double, Forward, Backward}
        [SerializeField]
        public Type type = Type.Double;
        public bool workOnce = false;
        private bool worked = false;
        [Range(0f, 1f)]
        public double position = 0.5;
        [SerializeField]
        public bool enabled = true;
        [SerializeField]
        public Color color = Color.white;
        [SerializeField]
        [HideInInspector]
        public UnityEvent onCross = new UnityEvent();
        public event System.Action<SplineUser> onUserCross;

        public SplineTrigger(Type t)
        {
            type = t;
            enabled = true;
            onCross = new UnityEvent();
        }

        /// <summary>
        /// Add a new UnityAction to the trigger
        /// </summary>
        /// <param name="action"></param>
        public void AddListener(UnityAction action)
        {
            onCross.AddListener(action);
        }

        public void Reset()
        {
            worked = false;
        }

        public bool Check(double previousPercent, double currentPercent)
        {
            if (!enabled) return false;
            if (workOnce && worked) return false;
            bool passed = false;
            switch (type)
            {
                case Type.Double: passed = (previousPercent <= position && currentPercent >= position) || (currentPercent <= position && previousPercent >= position); break;
                case Type.Forward: passed = previousPercent <= position && currentPercent >= position; break;
                case Type.Backward: passed = currentPercent <= position && previousPercent >= position; break;
            }
            if (passed) worked = true;
            return passed;
        }

        public void Invoke(SplineUser user = null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            onCross.Invoke();
            if (user)
            {
                if (onUserCross != null)
                {
                    onUserCross(user);
                }
            }
        }
    }
}
