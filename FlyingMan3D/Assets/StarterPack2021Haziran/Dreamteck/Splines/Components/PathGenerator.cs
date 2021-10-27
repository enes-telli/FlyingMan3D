using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Users/Path Generator")]
    public class PathGenerator : MeshGenerator
    {
        public int slices
        {
            get { return _slices; }
            set
            {
                if (value != _slices)
                {
                    if (value < 1) value = 1;
                    _slices = value;
                    Rebuild();
                }
            }
        }

        public bool useShapeCurve
        {
            get { return _useShapeCurve; }
            set
            {
                if (value != _useShapeCurve)
                {
                    _useShapeCurve = value;
                    if (_useShapeCurve)
                    {
                        _shape = new AnimationCurve();
                        _shape.AddKey(new Keyframe(0, 0));
                        _shape.AddKey(new Keyframe(1, 0));
                    } else _shape = null;
                    Rebuild();
                }
            }
        }

        public float shapeExposure
        {
            get { return _shapeExposure; }
            set
            {
                if (spline != null && value != _shapeExposure)
                {
                    _shapeExposure = value;
                    Rebuild();
                }
            }
        }



        public AnimationCurve shape
        {
            get { return _shape; }
            set
            {
                if(_lastShape == null) _lastShape = new AnimationCurve();
                bool keyChange = false;
                if (value.keys.Length != _lastShape.keys.Length) keyChange = true;
                else
                {
                    for (int i = 0; i < value.keys.Length; i++)
                    {
                        if (value.keys[i].inTangent != _lastShape.keys[i].inTangent || value.keys[i].outTangent != _lastShape.keys[i].outTangent || value.keys[i].time != _lastShape.keys[i].time || value.keys[i].value != value.keys[i].value)
                        {
                            keyChange = true;
                            break;
                        }
                    }
                }
                if (keyChange) Rebuild();
                _lastShape.keys = new Keyframe[value.keys.Length];
                value.keys.CopyTo(_lastShape.keys, 0);
                _lastShape.preWrapMode = value.preWrapMode;
                _lastShape.postWrapMode = value.postWrapMode;
                _shape = value;

            }
        }

        [SerializeField]
        [HideInInspector]
        private int _slices = 1;
        [SerializeField]
        [HideInInspector]
        private bool _useShapeCurve = false;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _shape;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _lastShape;
        [SerializeField]
        [HideInInspector]
        private float _shapeExposure = 1f;



        protected override void Awake()
        {
            base.Awake();
            mesh.name = "path";
        }

        protected override void Reset()
        {
            base.Reset();
        }


        protected override void BuildMesh()
        {
           base.BuildMesh();
           GenerateVertices();
           MeshUtility.GeneratePlaneTriangles(ref tsMesh.triangles, _slices, sampleCount, false);
        }


        void GenerateVertices()
        {
            int vertexCount = (_slices + 1) * sampleCount;
            AllocateMesh(vertexCount, _slices * (sampleCount-1) * 6);
            int vertexIndex = 0;

            ResetUVDistance();

            bool hasOffset = offset != Vector3.zero;

            for (int i = 0; i < sampleCount; i++)
            {
                GetSample(i, evalResult);
                Vector3 center = Vector3.zero;
                try
                {
                   center = evalResult.position;
                } catch (System.Exception ex) { Debug.Log(ex.Message + " for i = " + i); return; }
                Vector3 right = evalResult.right;
                float resultSize = GetBaseSize(evalResult);
                if (hasOffset)
                {
                    center += (offset.x * resultSize) * right + (offset.y * resultSize) * evalResult.up + (offset.z * resultSize) * evalResult.forward;
                }
                float fullSize = size * resultSize;
                Vector3 lastVertPos = Vector3.zero;
                Quaternion rot = Quaternion.AngleAxis(rotation, evalResult.forward);
                if (uvMode == UVMode.UniformClamp || uvMode == UVMode.UniformClip) AddUVDistance(i);
                Color vertexColor = GetBaseColor(evalResult) * color;
                for (int n = 0; n < _slices + 1; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    float shapeEval = 0f;
                    if (_useShapeCurve) shapeEval = _shape.Evaluate(slicePercent);
                    tsMesh.vertices[vertexIndex] = center + rot * right * (fullSize * 0.5f) - rot * right * (fullSize * slicePercent) + rot * evalResult.up * (shapeEval * _shapeExposure);
                    CalculateUVs(evalResult.percent, 1f - slicePercent);
                    tsMesh.uv[vertexIndex] = Vector2.one * 0.5f + (Vector2)(Quaternion.AngleAxis(uvRotation + 180f, Vector3.forward) * (Vector2.one * 0.5f - uvs));
                    if (_slices > 1)
                    {
                        if (n < _slices)
                        {
                            float forwardPercent = ((float)(n + 1) / _slices);
                            shapeEval = 0f;
                            if (_useShapeCurve) shapeEval = _shape.Evaluate(forwardPercent);
                            Vector3 nextVertPos = center + rot * right * fullSize * 0.5f - rot * right * fullSize * forwardPercent + rot * evalResult.up * shapeEval * _shapeExposure;
                            Vector3 cross1 = -Vector3.Cross(evalResult.forward, nextVertPos - tsMesh.vertices[vertexIndex]).normalized;

                            if (n > 0)
                            {
                                Vector3 cross2 = -Vector3.Cross(evalResult.forward, tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                                tsMesh.normals[vertexIndex] = Vector3.Slerp(cross1, cross2, 0.5f);
                            } else tsMesh.normals[vertexIndex] = cross1;
                        }
                        else   tsMesh.normals[vertexIndex] = -Vector3.Cross(evalResult.forward, tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                    }
                    else
                    {
                        tsMesh.normals[vertexIndex] = evalResult.up;
                        if (rotation != 0f) tsMesh.normals[vertexIndex] = rot * tsMesh.normals[vertexIndex];
                    }
                    tsMesh.colors[vertexIndex] = vertexColor;
                    lastVertPos = tsMesh.vertices[vertexIndex];
                    vertexIndex++;
                }
            }
        }
    }
}
