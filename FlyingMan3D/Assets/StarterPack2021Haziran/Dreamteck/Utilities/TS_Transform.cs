using UnityEngine;
using System.Collections;

namespace Dreamteck
{
    [System.Serializable]
    public class TS_Transform
    {
        public Vector3 position
        {
            get { return new Vector3(posX, posY, posZ); }
            set
            {
                setPosition = true;
                setLocalPosition = false;
                posX = value.x;
                posY = value.y;
                posZ = value.z;
            }
        }
        public Quaternion rotation
        {
            get { return new Quaternion(rotX, rotY, rotZ, rotW); }
            set
            {
                setRotation = true;
                setLocalRotation = false;
                rotX = value.x;
                rotY = value.y;
                rotZ = value.z;
                rotW = value.w;
            }
        }
        public Vector3 scale
        {
            get { return new Vector3(scaleX, scaleY, scaleZ); }
            set
            {
                setScale = true;
                scaleX = value.x;
                scaleY = value.y;
                scaleZ = value.z;
            }
        }

        public Vector3 lossyScale
        {
            get { return new Vector3(lossyScaleX, lossyScaleY, lossyScaleZ); }
            set
            {
                setScale = true;
                lossyScaleX = value.x;
                lossyScaleY = value.y;
                lossyScaleZ = value.z;
            }
        }

        public Vector3 localPosition
        {
            get { return new Vector3(lposX, lposY, lposZ); }
            set
            {
                setLocalPosition = true;
                setPosition = false;
                lposX = value.x;
                lposY = value.y;
                lposZ = value.z;
            }
        }
        public Quaternion localRotation
        {
            get { return new Quaternion(lrotX, lrotY, lrotZ, lrotW); }
            set
            {
                setLocalRotation = true;
                setRotation = false;
                lrotX = value.x;
                lrotY = value.y;
                lrotZ = value.z;
                lrotW = value.w;
            }
        }

        private bool setPosition = false;
        private bool setRotation = false;
        private bool setScale = false;
        private bool setLocalPosition = false;
        private bool setLocalRotation = false;

        public Transform transform
        {
            get
            {
                return _transform;
            }
        }

        [SerializeField]
        [HideInInspector]
        private Transform _transform;

        [SerializeField]
        [HideInInspector]
        private volatile float posX = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float posY = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float posZ = 0f;

        [SerializeField]
        [HideInInspector]
        private volatile float scaleX = 1f;
        [SerializeField]
        [HideInInspector]
        private volatile float scaleY = 1f;
        [SerializeField]
        [HideInInspector]
        private volatile float scaleZ = 1f;

        [SerializeField]
        [HideInInspector]
        private volatile float lossyScaleX = 1f;
        [SerializeField]
        [HideInInspector]
        private volatile float lossyScaleY = 1f;
        [SerializeField]
        [HideInInspector]
        private volatile float lossyScaleZ = 1f;

        [SerializeField]
        [HideInInspector]
        private volatile float rotX = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float rotY = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float rotZ = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float rotW = 0f;


        [SerializeField]
        [HideInInspector]
        private volatile float lposX = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float lposY = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float lposZ = 0f;

        [SerializeField]
        [HideInInspector]
        private volatile float lrotX = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float lrotY = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float lrotZ = 0f;
        [SerializeField]
        [HideInInspector]
        private volatile float lrotW = 0f;
#if UNITY_EDITOR
        private volatile bool isPlaying = false;
#endif

        public TS_Transform(Transform input)
        {
            SetTransform(input);
        }

        /// <summary>
        /// Update the TS_Transform. Call this regularly on every frame you need it to update. Should ALWAYS be called from the main thread
        /// </summary>
        public void Update()
        {
            if (transform == null) return;
#if UNITY_EDITOR
            isPlaying = Application.isPlaying;
#endif
            if (setPosition) _transform.position = position;
            else if (setLocalPosition) _transform.localPosition = localPosition;
            else
            {
                position = _transform.position;
                localPosition = _transform.localPosition;
            }

            if (setScale) _transform.localScale = scale;
            else scale = _transform.localScale;
            lossyScale = _transform.lossyScale;
            

            if (setRotation) _transform.rotation = rotation;
            else if (setLocalRotation) _transform.localRotation = localRotation;
            else
            {
                rotation = _transform.rotation;
                localRotation = _transform.localRotation;
            }
            setPosition = setLocalPosition = setRotation = setLocalRotation = setScale = false;
        }

        /// <summary>
        /// Set the transform reference. Should ALWAYS be called from the main thread
        /// </summary>
        /// <param name="input">Transform reference</param>
        public void SetTransform(Transform input)
        {
            _transform = input;
            setPosition = setLocalPosition = setRotation = setLocalRotation = setScale = false;
            Update();
        }

        /// <summary>
        /// Returns true if there's any change in the transform. Should ALWAYS be called from the main thread
        /// </summary>
        /// <returns></returns>
        public bool HasChange()
        {
            return HasPositionChange() || HasRotationChange() || HasScaleChange();
        }

        /// <summary>
        /// Returns true if there's a change in the position. Should ALWAYS be called from the main thread
        /// </summary>
        /// <returns></returns>
        public bool HasPositionChange()
        {
            return posX != _transform.position.x || posY != _transform.position.y || posZ != _transform.position.z;
        }

        /// <summary>
        /// Returns true if there is a change in the rotation. Should ALWAYS be called from the main thread
        /// </summary>
        /// <returns></returns>
        public bool HasRotationChange()
        {
            return rotX != _transform.rotation.x || rotY != _transform.rotation.y || rotZ != _transform.rotation.z || rotW != _transform.rotation.w;
        }

        /// <summary>
        /// Returns true if there is a change in the scale. Should ALWAYS be called from the main thread
        /// </summary>
        /// <returns></returns>
        public bool HasScaleChange()
        {
            return lossyScaleX != _transform.lossyScale.x || lossyScaleY != _transform.lossyScale.y || lossyScaleZ != _transform.lossyScale.z;
        }

        /// <summary>
        /// Thread-safe TransformPoint
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 TransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.TransformPoint(point);
#endif
            Vector3 scaled = new Vector3(point.x * lossyScaleX, point.y * lossyScaleY, point.z * lossyScaleZ);
            Vector3 rotated = rotation * scaled;
            return position + rotated;
        }

        /// <summary>
        /// Thread-safe TransformDirection
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Vector3 TransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.TransformDirection(direction);
#endif
            return TransformPoint(direction) - position;
        }

        /// <summary>
        /// Thread-safe InverseTransformPoint
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 InverseTransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.InverseTransformPoint(point);
#endif
            return InverseTransformDirection(point - position);
        }

        /// <summary>
        /// Thread-safe InverseTransformDirection
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Vector3 InverseTransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!isPlaying) return transform.InverseTransformDirection(direction);
#endif
            Vector3 rotated = Quaternion.Inverse(rotation) * direction;
            return new Vector3(rotated.x / lossyScaleX, rotated.y / lossyScaleY, rotated.z / lossyScaleZ);
        }

        public T GetComponent<T>()
        {
            return _transform.GetComponent<T>();
        }

    }
}