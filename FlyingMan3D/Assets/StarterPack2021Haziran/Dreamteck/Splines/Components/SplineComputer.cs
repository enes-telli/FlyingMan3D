namespace Dreamteck.Splines
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    public delegate void EmptySplineHandler();
    //MonoBehaviour wrapper for the spline class. It transforms the spline using the object's transform and provides thread-safe methods for sampling
    [AddComponentMenu("Dreamteck/Splines/Spline Computer")]
    [ExecuteInEditMode]
    public partial class SplineComputer : MonoBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector]
        public Color editorPathColor = Color.white;
        [HideInInspector]
        public bool alwaysDraw = false;
        [HideInInspector]
        public bool drawThinckness = false;
        [HideInInspector]
        public bool billboardThickness = true;
        private bool isPlaying = false;
        [HideInInspector]
        public bool isNewlyCreated = true;
#endif
        public enum Space { World, Local };
        public enum EvaluateMode { Cached, Calculate }
        public enum SampleMode { Default, Uniform, Optimized }
        public enum UpdateMode { Update, FixedUpdate, LateUpdate, AllUpdate, None }
        public Space space
        {
            get { return _space; }
            set
            {
                if (value != _space)
                {
                    SplinePoint[] worldPoints = GetPoints();
                    _space = value;
                    if (_space == Space.Local)
                    {
                        _transformedSamples = new SplineSample[_rawSamples.Length];
                        for (int i = 0; i < _transformedSamples.Length; i++) _transformedSamples[i] = new SplineSample();
                    }
                    SetPoints(worldPoints);
                    Rebuild(true);
                }
            }
        }
        public Spline.Type type
        {
            get
            {
                return spline.type;
            }

            set
            {
                if (value != spline.type)
                {
                    spline.type = value;
                    Rebuild(true);
                }
            }
        }

        public bool linearAverageDirection
        {
            get
            {
                return spline.linearAverageDirection;
            }

            set
            {
                if (value != spline.linearAverageDirection)
                {
                    spline.linearAverageDirection = value;
                    Rebuild(true);
                }
            }
        }

        public bool is2D
        {
            get { return _is2D; }
            set
            {
                if (value != _is2D)
                {
                    _is2D = value;
                    SetPoints(GetPoints());
                }
            }
        }

        public int sampleRate
        {
            get { return spline.sampleRate; }
            set
            {
                if (value != spline.sampleRate)
                {
                    if (value < 2) value = 2;
                    spline.sampleRate = value;
                    Rebuild(true);
                }
            }
        }

        public float optimizeAngleThreshold
        {
            get { return _optimizeAngleThreshold; }
            set
            {
                if (value != _optimizeAngleThreshold)
                {
                    if (value < 0.001f) value = 0.001f;
                    _optimizeAngleThreshold = value;
                    if (_sampleMode == SampleMode.Optimized)
                    {
                        Rebuild(true);
                    }
                }
            }
        }

        public SampleMode sampleMode
        {
            get { return _sampleMode; }
            set
            {
                if(value != _sampleMode)
                {
                    _sampleMode = value;
                    Rebuild(true);
                }
            }
        }
        [HideInInspector]
        public bool multithreaded = false;
        /// <summary>
        /// Will Rebuild the Spline Computer and its users as soon as it becomes enabled. Called in Start, not actually in Awake
        /// </summary>
        [HideInInspector]
        [Tooltip("Will Rebuild the Spline Computer and its users as soon as it becomes enabled")]
        public bool rebuildOnAwake = false;
        [HideInInspector]
        public UpdateMode updateMode = UpdateMode.Update;
        [HideInInspector]
        public TriggerGroup[] triggerGroups = new TriggerGroup[0];

        public AnimationCurve customValueInterpolation
        {
            get { return spline.customValueInterpolation; }
            set
            {
                spline.customValueInterpolation = value;
                Rebuild();
            }
        }

        public AnimationCurve customNormalInterpolation
        {
            get { return spline.customNormalInterpolation; }
            set
            {
                spline.customNormalInterpolation = value;
                Rebuild();
            }
        }

        public int iterations
        {
            get
            {
                return spline.iterations;
            }
        }

        public double moveStep
        {
            get
            {
                return spline.moveStep;
            }
        }

        public bool isClosed
        {
            get
            {
                return spline.isClosed;
            }
        }

        public int pointCount
        {
            get
            {
                return spline.points.Length;
            }
        }

        /// <summary>
        /// The transformed spline samples in world coordinates
        /// </summary>
        public SplineSample[] samples
        {
            get { return sampleCollection.samples; }
        }

        public int sampleCount
        {
            get { return _sampleCount; }
        }

        /// <summary>
        /// The raw spline samples without transformation applied
        /// </summary>
        public SplineSample[] rawSamples
        {
            get { return _rawSamples; }
        }

        /// <summary>
        /// Thread-safe transform's position
        /// </summary>
        public Vector3 position
        {
            get {
#if UNITY_EDITOR
                if (!isPlaying) return transform.position;
#endif    
                return lastPosition;
            }
        }
        /// <summary>
        /// Thread-safe transform's rotation
        /// </summary>
        public Quaternion rotation
        {
            get {
#if UNITY_EDITOR
                if (!isPlaying) return transform.rotation;
#endif
                return lastRotation;
            }
        }
        /// <summary>
        /// Thread-safe transform's scale
        /// </summary>
        public Vector3 scale
        {
            get {
#if UNITY_EDITOR
                if (!isPlaying) return transform.lossyScale;
#endif
                return lastScale;
            }
        }

        /// <summary>
        /// returns the number of subscribers this computer has
        /// </summary>
        public int subscriberCount
        {
            get
            {
                return _subscribers.Length;
            }
        }

        [HideInInspector]
        [SerializeField]
        private Spline spline = new Spline(Spline.Type.CatmullRom);
        [HideInInspector]
        [SerializeField]
        private SplineSample[] _rawSamples = new SplineSample[0];
        [HideInInspector]
        [SerializeField]
        private SplineSample[] _transformedSamples = new SplineSample[0];
        [HideInInspector]
        [SerializeField]
        private SampleCollection sampleCollection = new SampleCollection();
        [HideInInspector]
        [SerializeField]
        private double[] originalSamplePercents = new double[0];
        private bool[] sampleFlter = new bool[0];
        [HideInInspector]
        [SerializeField]
        private int _sampleCount = 0;
        [HideInInspector]
        [SerializeField]
        private bool _is2D = false;
        [HideInInspector]
        [SerializeField]
        private bool hasSamples = false;
        [HideInInspector]
        [SerializeField]
        private bool[] pointsDirty = new bool[0];
        [HideInInspector]
        [SerializeField]
        [Range(0.001f, 45f)]
        private float _optimizeAngleThreshold = 0.5f;
        [HideInInspector]
        [SerializeField]
        private Space _space = Space.Local;
        [HideInInspector]
        [SerializeField]
        private SampleMode _sampleMode = SampleMode.Default;
        [HideInInspector]
        [SerializeField]
        private SplineUser[] _subscribers = new SplineUser[0];
        [HideInInspector]
        [SerializeField]
        private NodeLink[] nodes = new NodeLink[0];
        private bool rebuildPending = false;

        private bool _trsCheck = false;
        private Transform _trs = null;
        public Transform trs
        {
            get
            {
                if (!_trsCheck)
                {
                    _trs = transform;
                }
                return _trs;
            }
        }

        private Matrix4x4 transformMatrix = new Matrix4x4();
        private Matrix4x4 inverseTransformMatrix = new Matrix4x4();
        private bool queueResample = false, queueRebuild = false;
        private Vector3 lastPosition = Vector3.zero, lastScale = Vector3.zero;
        private bool uniformScale = true;
        private Quaternion lastRotation = Quaternion.identity;

        public event EmptySplineHandler onRebuild;

        private bool useMultithreading
        {
            get
            {
                return multithreaded
#if UNITY_EDITOR
                && isPlaying
#endif
                ;
            }
        }

#if UNITY_EDITOR
        public void EditorAwake()
        {
            UpdateConnectedNodes();
            RebuildImmediate(true, true);
        }

        public void EditorUpdateConnectedNodes()
        {
            UpdateConnectedNodes();
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            isPlaying = Application.isPlaying;
            if (!isPlaying) return; //Do not call rebuild on awake in the  editor
#endif
        }

        private void Start()
        {
            if (rebuildOnAwake)
            {
                RebuildImmediate(true, true);
            }
            else
            {
                ResampleTransform();
            }
        }

        void FixedUpdate()
        {
            if(updateMode == UpdateMode.FixedUpdate || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        void Update()
        {
            if (updateMode == UpdateMode.Update || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        private void RunUpdate(bool immediate = false)
        {
            bool transformChanged = TransformHasChanged();
            if (transformChanged)
            {
                ResampleTransform();
                if (space == Space.Local && nodes.Length > 0)
                {
                    UpdateConnectedNodes();
                }
            }
            if (useMultithreading)
            {
                //Rebuild users at the beginning of the next cycle if multithreaded
                if (queueRebuild)
                {
                    RebuildUsers(immediate);
                }
            }
            if (queueResample)
            {
                if (useMultithreading)
                {
                    if (!transformChanged)
                    {
                        SplineThreading.Run(CalculateAndTransformSamples);
                    }
                    else
                    {
                        SplineThreading.Run(CalculateSamples);
                    }
                }
                else
                {
                    CalculateSamples();
                    if (!transformChanged)
                    {
                        TransformSamples();
                    }
                }
            }

            if (transformChanged)
            {
                SetPointsDirty();
                if (useMultithreading)
                {
                    SplineThreading.Run(TransformSamplesThreaded);
                }
                else
                {
                    TransformSamples(true);
                }
            }
            if (!useMultithreading)
            {
                //If not multithreaded, rebuild users here
                if (queueRebuild)
                {
                    RebuildUsers(immediate);
                }
            }
        }

        void TransformSamplesThreaded()
        {
            TransformSamples(true);
        }

        void CalculateAndTransformSamples()
        {
            CalculateSamples();
            TransformSamples();
        }

        bool TransformHasChanged()
        {
            return lastPosition != trs.position || lastRotation != trs.rotation || lastScale != trs.lossyScale;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            editorPathColor = SplinePrefs.defaultColor;
            drawThinckness = SplinePrefs.defaultShowThickness;
            is2D = SplinePrefs.default2D;
            alwaysDraw = SplinePrefs.defaultAlwaysDraw;
            space = SplinePrefs.defaultComputerSpace;
            type = SplinePrefs.defaultType;
        }
#endif

        void OnEnable()
        {
            if (rebuildPending)
            {
                rebuildPending = false;
                Rebuild();
            }
        }

        public void GetSamples(SampleCollection collection)
        {
            collection.samples = sampleCollection.samples;
            collection.optimizedIndices = sampleCollection.optimizedIndices;
            collection.sampleMode = _sampleMode;
        }

        /// <summary>
        /// Immediately sample the computer's transform (thread-unsafe). Call this before SetPoint(s) if the transform has been modified in the same frame
        /// </summary>
        public void ResampleTransform()
        {
            transformMatrix.SetTRS(trs.position, trs.rotation, trs.lossyScale);
            inverseTransformMatrix = transformMatrix.inverse;
            lastPosition = trs.position;
            lastRotation = trs.rotation;
            lastScale = trs.lossyScale;
            uniformScale = lastScale.x == lastScale.y && lastScale.y == lastScale.z;
        }

        /// <summary>
        /// Subscribe a SplineUser to this computer. This will rebuild the user automatically when there are changes.
        /// </summary>
        /// <param name="input">The SplineUser to subscribe</param>
        public void Subscribe(SplineUser input)
        {
            if (!IsSubscribed(input))
            {
                ArrayUtility.Add(ref _subscribers, input);
            }
        }

        /// <summary>
        /// Unsubscribe a SplineUser from this computer's updates
        /// </summary>
        /// <param name="input">The SplineUser to unsubscribe</param>
        public void Unsubscribe(SplineUser input)
        {
            for (int i = 0; i < _subscribers.Length; i++)
            {
                if (_subscribers[i] == input)
                {
                    ArrayUtility.RemoveAt(ref _subscribers, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if a user is subscribed to that computer
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsSubscribed(SplineUser user)
        {
            for (int i = 0; i < _subscribers.Length; i++)
            {
                if (_subscribers[i] == user)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns an array of subscribed users
        /// </summary>
        /// <returns></returns>
        public SplineUser[] GetSubscribers()
        {
            SplineUser[] subs = new SplineUser[_subscribers.Length];
            _subscribers.CopyTo(subs, 0);
            return subs;
        }

        /// <summary>
        /// Get the points from this computer's spline. All points are transformed in world coordinates.
        /// </summary>
        /// <returns></returns>
        public SplinePoint[] GetPoints(Space getSpace = Space.World)
        {
            SplinePoint[] points = new SplinePoint[spline.points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = spline.points[i];
                if (_space == Space.Local && getSpace == Space.World)
                {
                    points[i].position =TransformPoint(points[i].position);
                    points[i].tangent = TransformPoint(points[i].tangent);
                    points[i].tangent2 = TransformPoint(points[i].tangent2);
                    points[i].normal = TransformDirection(points[i].normal);
                }
            }
            return points;
        }

        /// <summary>
        /// Get a point from this computer's spline. The point is transformed in world coordinates.
        /// </summary>
        /// <param name="index">Point index</param>
        /// <returns></returns>
        public SplinePoint GetPoint(int index, Space getSpace = Space.World)
        {
            if (index < 0 || index >= spline.points.Length) return new SplinePoint();
            if (_space == Space.Local && getSpace == Space.World)
            {
                SplinePoint point = spline.points[index];
                point.position = TransformPoint(point.position);
                point.tangent = TransformPoint(point.tangent);
                point.tangent2 = TransformPoint(point.tangent2);
                point.normal = TransformDirection(point.normal);
                return point;
            } else return spline.points[index];
        }

        public Vector3 GetPointPosition(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World) return TransformPoint(spline.points[index].position);
            else return spline.points[index].position;
        }

        public Vector3 GetPointNormal(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World) return TransformDirection(spline.points[index].normal).normalized;
            else return spline.points[index].normal;
        }

        public Vector3 GetPointTangent(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World) return TransformPoint(spline.points[index].tangent);
            else return spline.points[index].tangent;
        }

        public Vector3 GetPointTangent2(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World) return TransformPoint(spline.points[index].tangent2);
            else return spline.points[index].tangent2;
        }

        public float GetPointSize(int index, Space getSpace = Space.World)
        {
            return spline.points[index].size;
        }

        public Color GetPointColor (int index, Space getSpace = Space.World)
        {
            return spline.points[index].color;
        }

        void Make2D(ref SplinePoint point)
        {
            point.normal = Vector3.back;
            point.position.z = 0f;
            point.tangent.z = 0f;
            point.tangent2.z = 0f;
        }

        /// <summary>
        /// Set the points of this computer's spline.
        /// </summary>
        /// <param name="points">The points array</param>
        /// <param name="setSpace">Use world or local space</param>
        public void SetPoints(SplinePoint[] points, Space setSpace = Space.World)
        {
            bool rebuild = false;
            if (points.Length != spline.points.Length)
            {
                rebuild = true;
                if (points.Length < 4) Break();
                spline.points = new SplinePoint[points.Length];
                SetPointsDirty();
            }
            
            SplinePoint newPoint;
            for (int i = 0; i < points.Length; i++)
            {
                newPoint = points[i];
                if (_space == Space.Local && setSpace == Space.World)
                {
                    newPoint.position = InverseTransformPoint(points[i].position);
                    newPoint.tangent = InverseTransformPoint(points[i].tangent);
                    newPoint.tangent2 = InverseTransformPoint(points[i].tangent2);
                    newPoint.normal = InverseTransformDirection(points[i].normal);
                }
                if (_is2D) Make2D(ref newPoint);
                if (SplinePoint.AreDifferent(ref newPoint, ref spline.points[i]))
                {
                    SetDirty(i);
                    rebuild = true;
                }
                spline.points[i] = newPoint;
            }
            if (isClosed) spline.points[spline.points.Length - 1] = spline.points[0];
            if (rebuild)
            {
                Rebuild();
                UpdateConnectedNodes(points);
            }
        }

        /// <summary>
        /// Set the position of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pos"></param>
        /// <param name="setSpace"></param>
        public void SetPointPosition(int index, Vector3 pos, Space setSpace = Space.World)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            Vector3 newPos = pos;
            if (_space == Space.Local && setSpace == Space.World) newPos = InverseTransformPoint(pos);
            if(newPos != spline.points[index].position)
            {
                SetDirty(index);
                spline.points[index].position = newPos;
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the tangents of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="tan1"></param>
        /// <param name="tan2"></param>
        /// <param name="setSpace"></param>
        public void SetPointTangents(int index, Vector3 tan1, Vector3 tan2, Space setSpace = Space.World)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            Vector3 newTan1 = tan1;
            Vector3 newTan2 = tan2;
            if (_space == Space.Local && setSpace == Space.World)
            {
                newTan1 = InverseTransformPoint(tan1);
                newTan2 = InverseTransformPoint(tan2);
            }
            bool rebuild = false;
            if(newTan2 != spline.points[index].tangent2)
            {
                rebuild = true;
                spline.points[index].SetTangent2Position(newTan2);
            }
            if (newTan1 != spline.points[index].tangent)
            {
                rebuild = true;
                spline.points[index].SetTangentPosition(newTan1);
            }
            if (_is2D) Make2D(ref spline.points[index]);

            if (rebuild)
            {
                SetDirty(index);
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the normal of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="nrm"></param>
        /// <param name="setSpace"></param>
        public void SetPointNormal(int index, Vector3 nrm, Space setSpace = Space.World)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            Vector3 newNrm = nrm;
            if (_space == Space.Local && setSpace == Space.World) newNrm = InverseTransformDirection(nrm);
            if (newNrm != spline.points[index].normal)
            {
                SetDirty(index);
                spline.points[index].normal = newNrm;
                if (_is2D) Make2D(ref spline.points[index]);
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the size of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="size"></param>
        public void SetPointSize(int index, float size)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            if (size != spline.points[index].size)
            {
                SetDirty(index);
                spline.points[index].size = size;
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the color of a control point. THis is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        public void SetPointColor(int index, Color color)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            if (color != spline.points[index].color)
            {
                SetDirty(index);
                spline.points[index].color = color;
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set a control point in world coordinates
        /// </summary>
        /// <param name="index"></param>
        /// <param name="point"></param>
        public void SetPoint(int index, SplinePoint point, Space setSpace = Space.World)
        {
            if (index < 0) return;
            if (index >= spline.points.Length) AppendPoints((index + 1) - spline.points.Length);
            bool rebuild = false;
            SplinePoint newPoint = point;
            if (_space == Space.Local && setSpace == Space.World)
            {
                newPoint.position = InverseTransformPoint(point.position);
                newPoint.tangent = InverseTransformPoint(point.tangent);
                newPoint.tangent2 = InverseTransformPoint(point.tangent2);
                newPoint.normal = InverseTransformDirection(point.normal);
            }
            if (_is2D) Make2D(ref newPoint);
            if (SplinePoint.AreDifferent(ref newPoint, ref spline.points[index])) rebuild = true;
            
            if (rebuild)
            {
                SetDirty(index);
                spline.points[index] = newPoint;
                Rebuild();
                SetNodeForPoint(index, point);
            }
        }

        private void AppendPoints(int count)
        {
            SplinePoint[] newPoints = new SplinePoint[spline.points.Length + count];
            spline.points.CopyTo(newPoints, 0);
            spline.points = newPoints;
            Rebuild(true);
        }

        /// <summary>
        /// Converts a point index to spline percent
        /// </summary>
        /// <param name="pointIndex">The point index</param>
        /// <returns></returns>
        public double GetPointPercent(int pointIndex)
        {
            double percent = DMath.Clamp01((double)pointIndex / (pointCount - 1));
            if (_sampleMode != SampleMode.Uniform) return percent;

            if (originalSamplePercents.Length <= 1) return 0.0;
            for (int i = originalSamplePercents.Length - 2; i >= 0; i--)
            {
                if (originalSamplePercents[i] < percent)
                {
                    double inverseLerp = DMath.InverseLerp(originalSamplePercents[i], originalSamplePercents[i + 1], percent);
                    return DMath.Lerp(sampleCollection.samples[i].percent, sampleCollection.samples[i+1].percent, inverseLerp);
                }
            }
            return 0.0;
        }

        public int PercentToPointIndex(double percent, Spline.Direction direction = Spline.Direction.Forward)
        {
            if(_sampleMode == SampleMode.Uniform)
            {
                int index;
                double lerp;
                GetSamplingValues(percent, out index, out lerp);
                if(lerp > 0.0)
                {
                    lerp = DMath.Lerp(originalSamplePercents[index], originalSamplePercents[index + 1], lerp);
                    if (direction == Spline.Direction.Forward) return DMath.FloorInt(lerp * (pointCount - 1));
                    else return DMath.CeilInt(lerp * (pointCount - 1));
                }
                if (direction == Spline.Direction.Forward)
                    return DMath.FloorInt(originalSamplePercents[index] * (pointCount - 1));
                else return DMath.CeilInt(originalSamplePercents[index] * (pointCount - 1));
            }
            if(direction == Spline.Direction.Forward) return DMath.FloorInt(percent * (pointCount - 1));
            else return DMath.CeilInt(percent * (pointCount - 1));
        }

        public Vector3 EvaluatePosition(double percent)
        {
            return EvaluatePosition(percent, EvaluateMode.Cached);
        }

        /// <summary>
        /// Same as Spline.EvaluatePosition but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public Vector3 EvaluatePosition(double percent, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Calculate) return TransformPoint(spline.EvaluatePosition(percent));
            return sampleCollection.EvaluatePosition(percent);
        }

        public Vector3 EvaluatePosition(int pointIndex, EvaluateMode mode = EvaluateMode.Cached)
        {
            return EvaluatePosition(GetPointPercent(pointIndex), mode);
        }

        public SplineSample Evaluate(double percent)
        {
            return Evaluate(percent, EvaluateMode.Cached);
        }

        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public SplineSample Evaluate(double percent, EvaluateMode mode = EvaluateMode.Cached)
        {
            SplineSample result = new SplineSample();
            Evaluate(percent, result, mode);
            return result;
        }

        /// <summary>
        /// Evaluate the spline at the position of a given point and return a SplineSample
        /// </summary>
        /// <param name="pointIndex">Point index</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        public SplineSample Evaluate(int pointIndex)
        {
            SplineSample result = new SplineSample();
            Evaluate(pointIndex, result);
            return result;
        }

        /// <summary>
        /// Evaluate the spline at the position of a given point and write in the SplineSample output
        /// </summary>
        /// <param name="pointIndex">Point index</param>
        public void Evaluate(int pointIndex, SplineSample result)
        {
            Evaluate(GetPointPercent(pointIndex), result);
        }

        public void Evaluate(double percent, SplineSample result)
        {
            Evaluate(percent, result, EvaluateMode.Cached);
        }
        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="result"></param>
        /// <param name="percent"></param>
        public void Evaluate(double percent, SplineSample result, EvaluateMode mode = EvaluateMode.Cached)
        {
            if(mode == EvaluateMode.Calculate)
            {
                spline.Evaluate(result, percent);
                TransformResult(result);
                return;
            }
            sampleCollection.Evaluate(percent, result);
        }

        /// <summary>
        /// Same as Spline.Evaluate but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineSample[] results, double from = 0.0, double to = 1.0)
        {
            sampleCollection.Evaluate(ref results, from, to);
        }

        /// <summary>
        /// Same as Spline.EvaluatePositions but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluatePositions(ref Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            sampleCollection.EvaluatePositions(ref positions, from, to);
        }

        /// <summary>
        /// Returns the percent from the spline at a given distance from the start point
        /// </summary>
        /// <param name="start">The start point</param>
        /// /// <param name="distance">The distance to travel</param>
        /// <param name="direction">The direction towards which to move</param>
        /// <returns></returns>
        public double Travel(double start, float distance, out float moved, Spline.Direction direction = Spline.Direction.Forward)
        {
            return sampleCollection.Travel(start, distance, direction, out moved);
        }

        public double Travel(double start, float distance, Spline.Direction direction = Spline.Direction.Forward)
        {
            float moved;
            return Travel(start, distance, out moved, direction);
        }

        /// <summary>
        /// Same as Spline.Project but the point is transformed by the computer's transform.
        /// </summary>
        /// <param name="position">Point in space</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <param name="subdivisions">Subdivisions for the Calculate mode. Don't assign if not using Calculated mode.</param>
        /// <returns></returns>
        public void Project(SplineSample result, Vector3 position, double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached, int subdivisions = 4)
        {
            if(mode == EvaluateMode.Calculate)
            {
                position = InverseTransformPoint(position);
                double percent = spline.Project(position, subdivisions, from, to);
                spline.Evaluate(result, percent);
                TransformResult(result);
                return;
            }
            sampleCollection.Project(position, pointCount, result, from, to);
        }

        public SplineSample Project(Vector3 point, double from = 0.0, double to = 1.0)
        {
            SplineSample result = new SplineSample();
            Project(result, point, from, to);
            return result;
        }

        /// <summary>
        /// Same as Spline.CalculateLength but this takes the computer's transform into account when calculating the length.
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution [0-1] default: 1f</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0)
        {
            if (!hasSamples) return 0f;
            return sampleCollection.CalculateLength(from, to);
        }

        private void TransformResult(SplineSample result)
        {
            result.position = TransformPoint(result.position);
            result.forward = TransformDirection(result.forward);
            result.up = TransformDirection(result.up);
            if (!uniformScale)
            {
                result.forward.Normalize();
                result.up.Normalize();
            }
        }

        public void Rebuild(bool forceUpdateAll = false)
        {
            if(forceUpdateAll) SetPointsDirty();
#if UNITY_EDITOR
            //If it's the editor and it's not playing, then rebuild immediate
            if (Application.isPlaying) queueResample = true;
            else RebuildImmediate(true);
#else
            queueResample = true;
#endif
            if (updateMode == UpdateMode.None) queueResample = false;
        }

        public void RebuildImmediate()
        {
            RebuildImmediate(true, true);
        }

        public void RebuildImmediate(bool calculateSamples = true, bool forceUpdateAll = false)
        {
            if (calculateSamples)
            {
                queueResample = true;
                if (forceUpdateAll)
                {
                    SetPointsDirty();
                }
            }
            else
            {
                queueResample = false;
            }
            RunUpdate(true);
        }

        private void RebuildUsers(bool immediate = false)
        {
            for (int i = _subscribers.Length - 1; i >= 0; i--)
            {
                if (_subscribers[i] != null)
                {
                    if (_subscribers[i].spline != this)
                    {
                        ArrayUtility.RemoveAt(ref _subscribers, i);
                    }
                    else
                    {
                        if (immediate)
                        {
                            _subscribers[i].RebuildImmediate();
                        } 
                        else
                        {
                            _subscribers[i].Rebuild();
                        }
                    }
                }
                else
                {
                    ArrayUtility.RemoveAt(ref _subscribers, i);
                }
            }
            if (onRebuild != null) onRebuild();
            queueRebuild = false;
        }

        void UnsetPointsDirty()
        {
            if (pointsDirty.Length != spline.points.Length) pointsDirty = new bool[spline.points.Length];
            for (int i = 0; i < pointsDirty.Length; i++) pointsDirty[i] = false;
        }

        void SetPointsDirty()
        {
            if (pointsDirty.Length != spline.points.Length) pointsDirty = new bool[spline.points.Length];
            for (int i = 0; i < pointsDirty.Length; i++)
            {
                pointsDirty[i] = true;
            }
        }

        void SetDirty(int index)
        {
            if (sampleMode == SampleMode.Uniform)
            {
                SetPointsDirty();
                return;
            }
            if (pointsDirty.Length != spline.points.Length)
            {
                pointsDirty = new bool[spline.points.Length];
            }
            pointsDirty[index] = true;
            if (index == 0 && isClosed)
            {
                pointsDirty[pointsDirty.Length - 1] = true;
            }
        }

        private void CalculateSamples()
        {
            queueResample = false;
            if (pointCount == 0)
            {
                if (_rawSamples.Length != 0)
                {
                    _rawSamples = new SplineSample[0];
                    sampleCollection.samples = new SplineSample[0];
                }
                return;
            }

            if (pointCount == 1)
            {
                if (_rawSamples.Length != 1)
                {
                    _rawSamples = new SplineSample[1];
                    _rawSamples[0] = new SplineSample();
                    sampleCollection.samples = new SplineSample[1];
                    sampleCollection.samples[0] = new SplineSample();
                }
                Evaluate(0.0, _rawSamples[0]);
                return;
            }

            if (_sampleMode == SampleMode.Uniform) spline.EvaluateUniform(ref _rawSamples, ref originalSamplePercents);
            else
            {
                if(originalSamplePercents.Length > 0) originalSamplePercents = new double[0];
                if (_rawSamples.Length != spline.iterations)
                {
                    _rawSamples = new SplineSample[spline.iterations];
                    for (int i = 0; i < _rawSamples.Length; i++) _rawSamples[i] = new SplineSample();
                }
                bool isHermite = true;
                if (type == Spline.Type.Bezier || type == Spline.Type.Linear) isHermite = false;

                for (int i = 0; i < _rawSamples.Length; i++)
                {
                    double samplePercent = (double)i / (_rawSamples.Length - 1);
                    if (isHermite ? IsDirtyHermite(samplePercent) : IsDirtyBezier(samplePercent))
                    {
                        spline.Evaluate(_rawSamples[i], samplePercent);
                    }
                }
            }

            if (isClosed)
            {
                _rawSamples[_rawSamples.Length - 1].CopyFrom(_rawSamples[0]);
                _rawSamples[_rawSamples.Length - 1].percent = 1.0;
            }
        }

        private void TransformSamples(bool forceTransformAll = false)
        {
            if (_transformedSamples.Length != _rawSamples.Length)
            {
                _transformedSamples = new SplineSample[_rawSamples.Length];
                for (int i = 0; i < _transformedSamples.Length; i++) _transformedSamples[i] = new SplineSample(_rawSamples[i]);
            }

            bool isHermite = true;
            if (type == Spline.Type.Bezier || type == Spline.Type.Linear) isHermite = false;
            if (space == Space.Local)
            {
                for (int i = 0; i < _rawSamples.Length; i++)
                {
                    if (!forceTransformAll && isHermite ? !IsDirtyHermite(_rawSamples[i].percent) : !IsDirtyBezier(_rawSamples[i].percent)) continue;
                    _transformedSamples[i].CopyFrom(_rawSamples[i]);
                    TransformResult(_transformedSamples[i]);
                }
            } else _transformedSamples = _rawSamples;
            if (_sampleMode == SampleMode.Optimized) OptimizeSamples();
            else
            {
                sampleCollection.samples = _transformedSamples;
                if (sampleFlter.Length > 0) sampleFlter = new bool[0];
                _sampleCount = sampleCollection.Count;
            }
            if (_sampleMode == SampleMode.Optimized)
            {
                if (sampleCollection.optimizedIndices.Length != _rawSamples.Length) sampleCollection.optimizedIndices = new int[_rawSamples.Length];
                sampleCollection.optimizedIndices[0] = 0;
                sampleCollection.optimizedIndices[sampleCollection.optimizedIndices.Length - 1] = sampleCollection.Count - 1;
                for (int i = 1; i < _rawSamples.Length-1; i++)
                {
                    sampleCollection.optimizedIndices[i] = 0;
                    double samplePercent = (double)i / (_rawSamples.Length - 1);
                    for (int j = 0; j < sampleCollection.Count; j++)
                    {
                        if (sampleCollection.samples[j].percent > samplePercent) break;
                        sampleCollection.optimizedIndices[i] = j;
                    }
                }
                if(sampleCollection.optimizedIndices.Length > 1) sampleCollection.optimizedIndices[sampleCollection.optimizedIndices.Length - 1] = sampleCollection.Count - 1;
            } else if (sampleCollection.Count > 0) sampleCollection.optimizedIndices = new int[0];
            sampleCollection.sampleMode = _sampleMode;
            queueRebuild = true;
            hasSamples = _sampleCount > 0;
            UnsetPointsDirty();
        }

        void OptimizeSamples()
        {
            if (_transformedSamples.Length <= 1) return;
            if (sampleFlter.Length != _rawSamples.Length) sampleFlter = new bool[_rawSamples.Length];
            _sampleCount = 2;
            Vector3 lastSavedDirection = _transformedSamples[0].forward;
            sampleFlter[0]  = true;
            sampleFlter[sampleFlter.Length - 1] = true;//Always include the first and last samples
            for (int i = 1; i < _transformedSamples.Length - 1; i++)
            {
                float angle = Vector3.Angle(lastSavedDirection, _transformedSamples[i].forward);
                if (angle >= _optimizeAngleThreshold)
                {
                    sampleFlter[i] = true;
                    _sampleCount++;
                    lastSavedDirection = _transformedSamples[i].forward;
                }
                else sampleFlter[i] = false;
            }

            if (sampleCollection.Count != _sampleCount || sampleCollection.samples == _transformedSamples)
            {
                sampleCollection.samples = new SplineSample[_sampleCount];
                for (int i = 0; i < sampleCollection.Count; i++) sampleCollection.samples[i] = new SplineSample();
            }

            int index = 0;
            for (int i = 0; i < _transformedSamples.Length; i++)
            {
                if (sampleFlter[i])
                { 
                    sampleCollection.samples[index].CopyFrom(_transformedSamples[i]);
                    index++;
                }
            }
        }

        bool IsDirtyBezier(double samplePercent)
        {
            float pointValue = ((float)samplePercent) * (pointCount - 1);
            int pointIndex = Mathf.FloorToInt(pointValue);
            if (pointsDirty[pointIndex]) return true;
            int nextIndex = pointIndex + 1;
            if (nextIndex > pointCount - 1)
            {
                if (isClosed) nextIndex = 0;
                else nextIndex = pointCount - 1;
            }
            if (pointsDirty[nextIndex]) return true;
            int previousIndex = pointIndex - 1;
            if (previousIndex < 0)
            {
                if (isClosed) previousIndex = pointCount - 1;
                else previousIndex = 0;
            }
            if (pointsDirty[previousIndex] && Mathf.Approximately(pointValue, pointIndex)) return true;
            return false;
        }

        bool IsDirtyHermite(double samplePercent)
        {
            float pointValue = ((float)samplePercent) * (pointCount - 1);
            int pointIndex = Mathf.FloorToInt(pointValue);
            if (pointsDirty[pointIndex]) return true;
            int nextIndex = pointIndex + 1;
            if (nextIndex > pointCount - 1)
            {
                if (isClosed) nextIndex = 0;
                else nextIndex = pointCount - 1;
            }
            int forwardIndex = nextIndex + 1;
            if (forwardIndex > pointCount - 1)
            {
                if (isClosed) forwardIndex = 1;
                else forwardIndex = pointCount - 1;
            }
            if (pointsDirty[nextIndex] || pointsDirty[forwardIndex]) return true;
            int previousIndex = pointIndex - 1;
            if (previousIndex < 0)
            {
                if (isClosed) previousIndex = pointCount - 2;
                else previousIndex = 0;
            }
            int backwardIndex = previousIndex - 1;
            if (backwardIndex < 0)
            {
                if(isClosed) backwardIndex = pointCount - 2;
                else backwardIndex = 0;
            }
            if (pointsDirty[previousIndex]) return true;
            if (pointsDirty[backwardIndex] && Mathf.Approximately(pointValue, pointIndex)) return true;
            return false;
        }

        /// <summary>
        /// Same as Spline.Break() but it will update all subscribed users
        /// </summary>
        public void Break()
        {
            Break(0);
        }

        /// <summary>
        /// Same as Spline.Break(at) but it will update all subscribed users
        /// </summary>
        /// <param name="at"></param>
        public void Break(int at)
        {
            if (spline.isClosed)
            {
                spline.Break(at);
                if (at != 0) SetPointsDirty();
                else
                {
                    SetDirty(0);
                    SetDirty(pointCount - 1);
                }
                Rebuild();
            }
        }

        /// <summary>
        /// Same as Spline.Close() but it will update all subscribed users
        /// </summary>
        public void Close()
        {
            if (!spline.isClosed)
            {
                spline.Close();
                SetDirty(0);
                SetDirty(pointCount-1);
                Rebuild();
            }
        }

        /// <summary>
        /// Same as Spline.HermiteToBezierTangents() but it will update all subscribed users
        /// </summary>
        public void CatToBezierTangents()
        {
            spline.CatToBezierTangents();
            SetPoints(spline.points, Space.Local);
        }

        /// <summary>
        /// Casts a ray along the transformed spline against all scene colliders.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percent of evaluation where the hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool Raycast(out RaycastHit hit, out double hitPercent, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0 , QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal)
        {
            resolution = DMath.Clamp01(resolution);
            Spline.FormatFromTo(ref from, ref to, false);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            hitPercent = 0f;
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
                if (Physics.Linecast(fromPos, toPos, out hit, layerMask, hitTriggers))
                {
                    double segmentPercent = (hit.point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    hitPercent = DMath.Lerp(prevPercent, percent, segmentPercent);
                    return true;
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            return false;
        }

        /// <summary>
        /// Casts a ray along the transformed spline against all scene colliders and returns all hits. Order is not guaranteed.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percents of evaluation where each hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool RaycastAll(out RaycastHit[] hits, out double[] hitPercents, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal)
        {
            resolution = DMath.Clamp01(resolution);
            Spline.FormatFromTo(ref from, ref to, false);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            List<RaycastHit> hitList = new List<RaycastHit>();
            List<double> percentList = new List<double>();
            bool hasHit = false;
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
                RaycastHit[] h = Physics.RaycastAll(fromPos, toPos - fromPos, Vector3.Distance(fromPos, toPos), layerMask, hitTriggers);
                for (int i = 0; i < h.Length; i++)
                {
                    hasHit = true;
                    double segmentPercent = (h[i].point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    percentList.Add(DMath.Lerp(prevPercent, percent, segmentPercent));
                    hitList.Add(h[i]);
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            hits = hitList.ToArray();
            hitPercents = percentList.ToArray();
            return hasHit;
        }


        public void CheckTriggers(double start, double end, SplineUser user = null)
        {
            for (int i = 0; i < triggerGroups.Length; i++)
            {
                triggerGroups[i].Check(start, end);
            }
        }

        public void CheckTriggers(int group, double start, double end)
        {
            if (group < 0 || group >= triggerGroups.Length)
            {
                Debug.LogError("Trigger group " + group + " does not exist");
                return;
            }
            triggerGroups[group].Check(start, end);
        }

        public void ResetTriggers()
        {
            for (int i = 0; i < triggerGroups.Length; i++) triggerGroups[i].Reset();
        }

        public void ResetTriggers(int group)
        {
            if (group < 0 || group >= triggerGroups.Length)
            {
                Debug.LogError("Trigger group " + group + " does not exist");
                return;
            }
            for (int i = 0; i < triggerGroups[group].triggers.Length; i++)
            {
                triggerGroups[group].triggers[i].Reset();
            }
        }

        /// <summary>
        /// Get the available junctions for the given point
        /// </summary>
        /// <param name="pointIndex"></param>
        /// <returns></returns>
        public List<Node.Connection> GetJunctions(int pointIndex)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if(nodes[i].pointIndex == pointIndex) return nodes[i].GetConnections(this);
            }
            return new List<Node.Connection>();
        }

        /// <summary>
        /// Get all junctions for all points in the given interval
        /// </summary>
        /// <param name="start"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Dictionary<int, List<Node.Connection>> GetJunctions(double start = 0.0, double end = 1.0)
        {
            int index;
            double lerp;
            sampleCollection.GetSamplingValues(start, out index, out lerp);
            Dictionary<int, List<Node.Connection>> junctions = new Dictionary<int, List<Node.Connection>>();
            float startValue = (pointCount - 1) * (float)start;
            float endValue = (pointCount - 1) * (float)end;
            for (int i = 0; i < nodes.Length; i++)
            {
                bool add = false;
                if (end > start && nodes[i].pointIndex > startValue && nodes[i].pointIndex < endValue) add = true;
                else if (nodes[i].pointIndex < startValue && nodes[i].pointIndex > endValue) add = true;
                if (!add && Mathf.Abs(startValue - nodes[i].pointIndex) <= 0.0001f) add = true;
                if (!add && Mathf.Abs(endValue - nodes[i].pointIndex) <= 0.0001f) add = true;
                if (add) junctions.Add(nodes[i].pointIndex, nodes[i].GetConnections(this));
            }
            return junctions;
        }

        /// <summary>
        /// Call this to connect a node to a spline's point
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pointIndex"></param>
        public void ConnectNode(Node node, int pointIndex)
        {
            if (node == null)
            {
                Debug.LogError("Missing Node");
                return;
            }
            if (pointIndex < 0 || pointIndex >= spline.points.Length)
            {
                Debug.Log("Invalid point index " + pointIndex);
                return;
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].node == null) continue;
                if (nodes[i].pointIndex == pointIndex || nodes[i].node == node)
                {
                    Node.Connection[] connections = nodes[i].node.GetConnections();
                    for (int j = 0; j < connections.Length; j++)
                    {
                        if (connections[j].spline == this)
                        {
                            Debug.LogError("Node " + node.name + " is already connected to spline " + name + " at point " + nodes[i].pointIndex);
                            return;
                        }
                    }
                    AddNodeLink(node, pointIndex);
                    return;
                }
            }
            node.AddConnection(this, pointIndex);
            AddNodeLink(node, pointIndex);
        }

        public void DisconnectNode(int pointIndex)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].pointIndex == pointIndex)
                {
                    nodes[i].node.RemoveConnection(this, pointIndex);
                    ArrayUtility.RemoveAt(ref nodes, i);
                    return;
                }
            }
        }

        private void AddNodeLink(Node node, int pointIndex)
        {
            NodeLink newLink = new NodeLink();
            newLink.node = node;
            newLink.pointIndex = pointIndex;
            ArrayUtility.Add(ref nodes, newLink);
            UpdateConnectedNodes();
        }

        public Dictionary<int, Node> GetNodes(double start = 0.0, double end = 1.0)
        {
            int index;
            double lerp;
            sampleCollection.GetSamplingValues(start, out index, out lerp);
            Dictionary<int, Node> nodeList = new Dictionary<int, Node>();
            float startValue = (pointCount - 1) * (float)start;
            float endValue = (pointCount - 1) * (float)end;
            for (int i = 0; i < nodes.Length; i++)
            {
                bool add = false;
                if (end > start && nodes[i].pointIndex > startValue && nodes[i].pointIndex < endValue) add = true;
                else if (nodes[i].pointIndex < startValue && nodes[i].pointIndex > endValue) add = true;
                if (!add && Mathf.Abs(startValue - nodes[i].pointIndex) <= 0.0001f) add = true;
                if (!add && Mathf.Abs(endValue - nodes[i].pointIndex) <= 0.0001f) add = true;
                if (add) nodeList.Add(nodes[i].pointIndex, nodes[i].node);
            }
            return nodeList;
        }

        public Node GetNode(int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= pointCount) return null;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].pointIndex == pointIndex) return nodes[i].node;
            }
            return null;
        }

        public void TransferNode(int pointIndex, int newPointIndex)
        {
            if(newPointIndex < 0 || newPointIndex >= pointCount)
            {
                Debug.LogError("Invalid new point index " + newPointIndex);
                return;
            }
            if (GetNode(newPointIndex) != null)
            {
                Debug.LogError("Cannot move node to point " + newPointIndex + ". Point already connected to a node");
                return;
            }
            Node node = GetNode(pointIndex);
            if(node == null)
            {
                Debug.LogError("No node connected to point " + pointIndex);
                return;
            }
            DisconnectNode(pointIndex);
            ConnectNode(node, newPointIndex);
        }

        public void ShiftNodes(int startIndex, int endIndex, int shift)
        {
            if(startIndex < endIndex)
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    Node node = GetNode(i);
                    if(node != null) TransferNode(i, i + shift);
                }
            } else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    Node node = GetNode(i);
                    if (node != null) TransferNode(i, i + shift);
                }
            }
        }

        /// <summary>
        /// Gets all connected computers along with the connected indices and connection indices
        /// </summary>
        /// <param name="computers">A list of the connected computers</param>
        /// <param name="connectionIndices">The point indices of this computer where the other computers are connected</param>
        /// <param name="connectedIndices">The point indices of the other computers where they are connected</param>
        /// <param name="percent"></param>
        /// <param name="direction"></param>
        /// <param name="includeEqual">Should point indices that are placed exactly at the percent be included?</param>
        public void GetConnectedComputers(List<SplineComputer> computers, List<int> connectionIndices, List<int> connectedIndices, double percent, Spline.Direction direction, bool includeEqual)
        {
            if (computers == null) computers = new List<SplineComputer>();
            if (connectionIndices == null) connectionIndices = new List<int>();
            if (connectedIndices == null) connectionIndices = new List<int>();
            computers.Clear();
            connectionIndices.Clear();
            connectedIndices.Clear();
            int pointValue = Mathf.FloorToInt((pointCount - 1) * (float)percent);
            for (int i = 0; i < nodes.Length; i++)
            {
                bool condition = false;
                if (includeEqual)
                {
                    if (direction == Spline.Direction.Forward) condition = nodes[i].pointIndex >= pointValue;
                    else condition = nodes[i].pointIndex <= pointValue;
                } else
                {

                }
                if (condition)
                {
                    Node.Connection[] connections = nodes[i].node.GetConnections();
                    for (int j = 0; j < connections.Length; j++)
                    {
                        if (connections[j].spline != this) {
                            computers.Add(connections[j].spline);
                            connectionIndices.Add(nodes[i].pointIndex);
                            connectedIndices.Add(connections[j].pointIndex);
                        }
                    }
                } 
            }
        }

        /// <summary>
        /// Returns a list of all connected computers. This includes the base computer too.
        /// </summary>
        /// <returns></returns>
        public List<SplineComputer> GetConnectedComputers()
        {
            List<SplineComputer> computers = new List<SplineComputer>();
            computers.Add(this);
            if (nodes.Length == 0) return computers;
            GetConnectedComputers(ref computers);
            return computers;
        }

        public void GetSamplingValues(double percent, out int index, out double lerp)
        {
            sampleCollection.GetSamplingValues(percent, out index, out lerp);
        }

        private void GetConnectedComputers(ref List<SplineComputer> computers)
        {
            SplineComputer comp = computers[computers.Count - 1];
            if (comp == null) return;
            for (int i = 0; i < comp.nodes.Length; i++)
            {
                if (comp.nodes[i].node == null) continue;
                Node.Connection[] connections = comp.nodes[i].node.GetConnections();
                for (int n = 0; n < connections.Length; n++)
                {
                    bool found = false;
                    if (connections[n].spline == this) continue;
                    for (int x = 0; x < computers.Count; x++)
                    {
                        if (computers[x] == connections[n].spline)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        computers.Add(connections[n].spline);
                        GetConnectedComputers(ref computers);
                    }
                }
            }
        }

        private void RemoveNodeLinkAt(int index)
        {
            //Then remove the node link
            NodeLink[] newLinks = new NodeLink[nodes.Length - 1];
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i == index) continue;
                else if (i < index) newLinks[i] = nodes[i];
                else newLinks[i - 1] = nodes[i];
            }
            nodes = newLinks;
        }

        //This magically updates the Node's position and all other points, connected to it when a point, linked to a Node is edited.
        private void SetNodeForPoint(int index, SplinePoint worldPoint)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].pointIndex == index)
                {
                    nodes[i].node.UpdatePoint(this, nodes[i].pointIndex, worldPoint);
                    break;
                }
            }
        }

        private void UpdateConnectedNodes(SplinePoint[] worldPoints)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].node == null)
                {
                    RemoveNodeLinkAt(i);
                    i--;
                    Rebuild();
                    continue;
                }
                bool found = false;
                foreach(Node.Connection connection in nodes[i].node.GetConnections())
                {
                    if(connection.spline == this)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    RemoveNodeLinkAt(i);
                    i--;
                    Rebuild();
                    continue;
                }
                nodes[i].node.UpdatePoint(this, nodes[i].pointIndex, worldPoints[nodes[i].pointIndex]);
                nodes[i].node.UpdateConnectedComputers(this);
            }
        }

        private void UpdateConnectedNodes()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].node == null)
                {
                    RemoveNodeLinkAt(i);
                    Rebuild();
                    i--;
                    continue;
                }
                bool found = false;
                Node.Connection[] connections = nodes[i].node.GetConnections();
                for (int j = 0; j < connections.Length; j++)
                {
                    if(connections[j].spline == this && connections[j].pointIndex == nodes[i].pointIndex)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    nodes[i].node.UpdatePoint(this, nodes[i].pointIndex, GetPoint(nodes[i].pointIndex));
                    nodes[i].node.UpdateConnectedComputers(this);
                } else
                {
                    RemoveNodeLinkAt(i);
                    Rebuild();
                    i--;
                    continue;
                }
            }
        }

        public Vector3 TransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.TransformPoint(point);
#endif
            return transformMatrix.MultiplyPoint3x4(point);
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.InverseTransformPoint(point);
#endif
            return inverseTransformMatrix.MultiplyPoint3x4(point);
        }

        public Vector3 TransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.TransformDirection(direction);
#endif
            return transformMatrix.MultiplyVector(direction);
        }

        public Vector3 InverseTransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.InverseTransformDirection(direction);
#endif
            return inverseTransformMatrix.MultiplyVector(direction);
        }

        [System.Serializable]
        internal class NodeLink
        {
            [SerializeField]
            internal Node node = null;
            [SerializeField]
            internal int pointIndex = 0;

            internal List<Node.Connection> GetConnections(SplineComputer exclude)
            {
                Node.Connection[] connections = node.GetConnections();
                List<Node.Connection> connectionList = new List<Node.Connection>();
                for (int i = 0; i < connections.Length; i++)
                {
                    if (connections[i].spline == exclude) continue;
                    connectionList.Add(connections[i]);
                }
                return connectionList;
            }
        }
    }
}
