using UnityEngine;
using UnityEngine.Events;

namespace Dreamteck.Splines
{
    public delegate void SplineReachHandler();
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Follower")]
    public class SplineFollower : SplineTracer
    {
        public enum FollowMode { Uniform, Time }
        public enum Wrap { Default, Loop, PingPong }
        [HideInInspector]
        public Wrap wrapMode = Wrap.Default;
        [HideInInspector]
        public FollowMode followMode = FollowMode.Uniform;

        [HideInInspector]
        public bool autoStartPosition = false;

        [HideInInspector]
        public bool follow = true;

        /// <summary>
        /// If the follow mode is set to Uniform and there is an added offset in the motion panel, this will presserve the uniformity of the follow speed
        /// </summary>
        [HideInInspector]
        public bool preserveUniformSpeedWithOffset = false;

        /// <summary>
        /// Used when follow mode is set to Uniform. Defines the speed of the follower
        /// </summary>
        public float followSpeed
        {
            get { return _followSpeed; }
            set
            {
                if (_followSpeed != value)
                {
                    if (value < 0f) value = 0f;
                    _followSpeed = value;
                }
            }
        }

        public double startPosition
        {
            get { return _startPosition; }
            set
            {
                if (value != _startPosition)
                {
                    _startPosition = DMath.Clamp01(value);
                    if (!followStarted) SetPercent(_startPosition);
                }
            }
        }

        /// <summary>
        /// Used when follow mode is set to Time. Defines how much time it takes for the follower to travel through the path
        /// </summary>
        public float followDuration
        {
            get { return _followDuration; }
            set
            {
                if (_followDuration != value)
                {
                    if (value < 0f) value = 0f;
                    _followDuration = value;
                }
            }
        }

        public event System.Action<double> onEndReached;
        public event System.Action<double> onBeginningReached;

        public FollowerSpeedModifier speedModifier
        {
            get
            {
                return _speedModifier;
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _followSpeed = 1f;
        [SerializeField]
        [HideInInspector]
        private float _followDuration = 1f;
        [SerializeField]
        [HideInInspector]
        [Range(0f, 1f)]
        private double _startPosition = 0.0;

        [SerializeField]
        [HideInInspector]
        private FollowerSpeedModifier _speedModifier = new FollowerSpeedModifier();

        [SerializeField]
        [HideInInspector]
        private FloatEvent _unityOnEndReached = null;
        [SerializeField]
        [HideInInspector]
        private FloatEvent _unityOnBeginningReached = null;

        private double lastClippedPercent = -1.0;
        private bool followStarted = false;

#if UNITY_EDITOR
        public bool editorSetPosition = true;
#endif

        protected override void Start()
        {
            base.Start();
            if (follow && autoStartPosition) SetPercent(spline.Project(GetTransform().position).percent);
        }

        protected override void LateRun()
        {
            base.LateRun();
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (follow) Follow();
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            Evaluate(_result.percent, _result);
            if (follow && !autoStartPosition) ApplyMotion();
        }

        void Follow()
        {
            if (!followStarted)
            {
                if (autoStartPosition)
                {
                    Project(GetTransform().position, evalResult);
                    SetPercent(evalResult.percent);
                }
                else SetPercent(_startPosition);
            }

            followStarted = true;
            switch (followMode)
            {
                case FollowMode.Uniform:
                    float speed = _followSpeed + _speedModifier.GetSpeed(result);
                    Move(Time.deltaTime * speed); break;
                case FollowMode.Time:
                    if (_followDuration == 0.0) Move(0.0);
                    else Move((double)Time.deltaTime / _followDuration);
                    break;
            }
        }

        public void Restart(double startPosition = 0.0)
        {
            followStarted = false;
            SetPercent(startPosition);
        }

        public override void SetPercent(double percent, bool checkTriggers = false, bool handleJuncitons = false)
        {
            base.SetPercent(percent, checkTriggers, handleJuncitons);
            lastClippedPercent = percent;
        }

        public override void SetDistance(float distance, bool checkTriggers = false, bool handleJuncitons = false)
        {
            base.SetDistance(distance, checkTriggers, handleJuncitons);
            lastClippedPercent = ClipPercent(_result.percent);
            if (samplesAreLooped && clipFrom == clipTo && distance > 0f && lastClippedPercent == 0.0) lastClippedPercent = 1.0;
        }

        public void Move(double percent)
        {
            if (percent == 0.0) return;
            if (sampleCount <= 1)
            {
                if (sampleCount == 1)
                {
                    _result.CopyFrom(GetSampleRaw(0));
                    ApplyMotion();
                }
                return;
            }
            Evaluate(_result.percent, _result);
            double startPercent = _result.percent;
            if (wrapMode == Wrap.Default && lastClippedPercent >= 1.0 && startPercent == 0.0) startPercent = 1.0;
            double p = startPercent + (_direction == Spline.Direction.Forward ? percent : -percent);
            bool callOnEndReached = false, callOnBeginningReached = false;
            lastClippedPercent = p;
            if (_direction == Spline.Direction.Forward && p >= 1.0)
            {
                if (startPercent < 1.0)
                {
                    callOnEndReached = true;
                }
                switch (wrapMode)
                {
                    case Wrap.Default:
                        p = 1.0;
                        break;
                    case Wrap.Loop:
                        CheckTriggers(startPercent, 1.0);
                        CheckNodes(startPercent, 1.0);
                        while (p > 1.0) p -= 1.0;
                        startPercent = 0.0;
                        break;
                    case Wrap.PingPong:
                        p = DMath.Clamp01(1.0 - (p - 1.0));
                        startPercent = 1.0;
                        _direction = Spline.Direction.Backward;
                        break;
                }
            }
            else if (_direction == Spline.Direction.Backward && p <= 0.0)
            {
                if (startPercent > 0.0)
                {
                    callOnBeginningReached = true;
                }
                switch (wrapMode)
                {
                    case Wrap.Default:
                        p = 0.0;
                        break;
                    case Wrap.Loop:
                        CheckTriggers(startPercent, 0.0);
                        CheckNodes(startPercent, 0.0);
                        while (p < 0.0) p += 1.0;
                        startPercent = 1.0;
                        break;
                    case Wrap.PingPong:
                        p = DMath.Clamp01(-p);
                        startPercent = 0.0;
                        _direction = Spline.Direction.Forward;
                        break;
                }
            }
            CheckTriggers(startPercent, p);
            CheckNodes(startPercent, p);
            Evaluate(p, _result);
            ApplyMotion();
            if (callOnEndReached)
            {
                if (onEndReached != null)
                {
                    onEndReached(startPercent);
                }
                _unityOnEndReached.Invoke((float)startPercent);
            }
            else if (callOnBeginningReached)
            {
                if (onBeginningReached != null)
                {
                    onBeginningReached(startPercent);
                }
                _unityOnBeginningReached.Invoke((float)startPercent);
            }
            InvokeTriggers();
            InvokeNodes();
        }

        public void Move(float distance)
        {
            bool endReached = false, beginningReached = false;
            float moved = 0f;
            double startPercent = _result.percent;
            _result.percent = DoTravel(_result.percent, distance, out moved);
            if (startPercent != _result.percent)
            {
                CheckTriggers(startPercent, _result.percent);
                CheckNodes(startPercent, _result.percent);
            }

            if (direction == Spline.Direction.Forward)
            {
                if (_result.percent >= 1.0)
                {
                    if (startPercent < 1.0)
                    {
                        endReached = true;
                    }
                    switch (wrapMode)
                    {
                        case Wrap.Loop:
                            _result.percent = DoTravel(0.0, distance - moved, out moved);
                            CheckTriggers(0.0, _result.percent);
                            CheckNodes(0.0, _result.percent);
                            break;
                        case Wrap.PingPong:
                            _direction = Spline.Direction.Backward;
                            _result.percent = DoTravel(1.0, distance - moved, out moved);
                            CheckTriggers(1.0, _result.percent);
                            CheckNodes(1.0, _result.percent);
                            break;
                    }
                }
            } else
            {
                if (_result.percent <= 0.0)
                {
                    if (startPercent > 0.0)
                    {
                        beginningReached = true;
                    }
                    switch (wrapMode)
                    {
                        case Wrap.Loop:
                            _result.percent = DoTravel(1.0, distance - moved, out moved);
                            CheckTriggers(1.0, _result.percent);
                            CheckNodes(1.0, _result.percent);
                            break;
                        case Wrap.PingPong:
                            _direction = Spline.Direction.Forward;
                            _result.percent = DoTravel(0.0, distance - moved, out moved);
                            CheckTriggers(0.0, _result.percent);
                            CheckNodes(0.0, _result.percent);
                            break;
                    }
                }
            }

            Evaluate(_result.percent, _result);
            ApplyMotion();
            if (endReached)
            {
                if (onEndReached != null)
                {
                    onEndReached(startPercent);
                }
                _unityOnEndReached.Invoke((float)startPercent);
            }
            else if (beginningReached)
            {
                if (onBeginningReached != null)
                {
                    onBeginningReached(startPercent);
                }
                _unityOnBeginningReached.Invoke((float)startPercent);
            }
            InvokeTriggers();
            InvokeNodes();
        }

        protected virtual double DoTravel(double start, float distance, out float moved)
        {
            moved = 0f;
            double result = 0.0;
            if (preserveUniformSpeedWithOffset && _motion.hasOffset)
            {
                result = TravelWithOffset(start, distance, _direction, _motion.offset, out moved);
            } else
            {
                result = Travel(start, distance, _direction, out moved);
            }
            return result;
        }

        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }
    }
}
