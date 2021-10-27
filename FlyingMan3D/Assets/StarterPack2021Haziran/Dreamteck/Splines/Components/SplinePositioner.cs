using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Positioner")]
    public class SplinePositioner : SplineTracer
    {
        public enum Mode { Percent, Distance }
        
        public GameObject targetObject
        {
            get
            {
                if (_targetObject == null) return gameObject;
                return _targetObject;
            }

            set
            {
                if (value != _targetObject)
                {
                    _targetObject = value;
                    RefreshTargets();
                    Rebuild();
                }
            }
        }

        public double position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value != _position)
                {
                    animPosition = (float)value;
                    _position = value;
                    if (mode == Mode.Distance) SetDistance((float)_position, true);
                    else SetPercent(_position, true);
                }
            }
        }

        public Mode mode
        {
            get { return _mode;  }
            set
            {
                if (value != _mode)
                {
                    _mode = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private GameObject _targetObject;
        [SerializeField]
        [HideInInspector]
        private double _position = 0.0;
        [SerializeField]
        [HideInInspector]
        private float animPosition = 0f;
        [SerializeField]
        [HideInInspector]
        private Mode _mode = Mode.Percent;

        protected override void OnDidApplyAnimationProperties()
        {
            if (animPosition != _position) position = animPosition;
            base.OnDidApplyAnimationProperties();
        }

        protected override Transform GetTransform()
        {
            return targetObject.transform;
        }

        protected override Rigidbody GetRigidbody()
        {
            return targetObject.GetComponent<Rigidbody>();
        }

        protected override Rigidbody2D GetRigidbody2D()
        {
            return targetObject.GetComponent<Rigidbody2D>();
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            if (mode == Mode.Distance) SetDistance((float)_position, true);
            else SetPercent(_position, true);
        }

        public override void SetPercent(double percent, bool checkTriggers = false, bool handleJuncitons = false)
        {
            base.SetPercent(percent, checkTriggers, handleJuncitons);
            _position = percent;
        }

        public override void SetDistance(float distance, bool checkTriggers = false, bool handleJuncitons = false)
        {
            base.SetDistance(distance, checkTriggers, handleJuncitons);
            _position = distance;
        }
    }
}
