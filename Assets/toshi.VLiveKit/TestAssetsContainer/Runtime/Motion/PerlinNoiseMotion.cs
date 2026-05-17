using UnityEngine;

namespace toshi.VLiveKit.TestAssetsContainer
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("toshi/VLiveKit/Test Assets/Perlin Noise Motion")]
    public sealed class PerlinNoiseMotion : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool animateInEditMode;
        [SerializeField] private bool useLocalSpace = true;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool captureBaseOnEnable = true;
        [SerializeField] private bool randomizeSeedOnEnable;
        [SerializeField] private float seed = 1f;
        [SerializeField] private float timeOffset;

        [Header("Position")]
        [SerializeField] private Vector3 positionAmplitude = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 positionFrequency = Vector3.one;

        [Header("Rotation")]
        [SerializeField] private Vector3 rotationAmplitude;
        [SerializeField] private Vector3 rotationFrequency = Vector3.one;

        [Header("Scale")]
        [SerializeField] private Vector3 scaleAmplitude;
        [SerializeField] private Vector3 scaleFrequency = Vector3.one;

        [Header("Debug")]
        [SerializeField] private Vector3 currentPositionOffset;
        [SerializeField] private Vector3 currentRotationOffset;
        [SerializeField] private Vector3 currentScaleOffset;

        private Vector3 baseLocalPosition;
        private Vector3 baseWorldPosition;
        private Quaternion baseLocalRotation;
        private Quaternion baseWorldRotation;
        private Vector3 baseLocalScale;
        private bool hasBaseTransform;

        private Transform Target => target != null ? target : transform;

        private void Reset()
        {
            target = transform;
            CaptureBaseTransform();
        }

        private void OnEnable()
        {
            if (target == null)
            {
                target = transform;
            }

            if (randomizeSeedOnEnable)
            {
                RandomizeSeed();
            }

            if (captureBaseOnEnable || !hasBaseTransform)
            {
                CaptureBaseTransform();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !animateInEditMode)
            {
                return;
            }

            if (!hasBaseTransform)
            {
                CaptureBaseTransform();
            }

            ApplyMotion(GetTime());
        }

        [ContextMenu("Capture Current As Base")]
        public void CaptureBaseTransform()
        {
            var motionTarget = Target;
            baseLocalPosition = motionTarget.localPosition;
            baseWorldPosition = motionTarget.position;
            baseLocalRotation = motionTarget.localRotation;
            baseWorldRotation = motionTarget.rotation;
            baseLocalScale = motionTarget.localScale;
            hasBaseTransform = true;
        }

        [ContextMenu("Restore Base Transform")]
        public void RestoreBaseTransform()
        {
            if (!hasBaseTransform)
            {
                return;
            }

            var motionTarget = Target;
            if (useLocalSpace)
            {
                motionTarget.localPosition = baseLocalPosition;
                motionTarget.localRotation = baseLocalRotation;
            }
            else
            {
                motionTarget.position = baseWorldPosition;
                motionTarget.rotation = baseWorldRotation;
            }

            motionTarget.localScale = baseLocalScale;
            currentPositionOffset = Vector3.zero;
            currentRotationOffset = Vector3.zero;
            currentScaleOffset = Vector3.zero;
        }

        [ContextMenu("Randomize Seed")]
        public void RandomizeSeed()
        {
            seed = Random.Range(0f, 10000f);
        }

        private float GetTime()
        {
            if (!Application.isPlaying)
            {
                return Time.realtimeSinceStartup + timeOffset;
            }

            return (useUnscaledTime ? Time.unscaledTime : Time.time) + timeOffset;
        }

        private void ApplyMotion(float time)
        {
            currentPositionOffset = EvaluateOffset(positionAmplitude, positionFrequency, time, seed);
            currentRotationOffset = EvaluateOffset(rotationAmplitude, rotationFrequency, time, seed + 17.13f);
            currentScaleOffset = EvaluateOffset(scaleAmplitude, scaleFrequency, time, seed + 41.71f);

            var motionTarget = Target;
            if (useLocalSpace)
            {
                motionTarget.localPosition = baseLocalPosition + currentPositionOffset;
                motionTarget.localRotation = baseLocalRotation * Quaternion.Euler(currentRotationOffset);
            }
            else
            {
                motionTarget.position = baseWorldPosition + currentPositionOffset;
                motionTarget.rotation = baseWorldRotation * Quaternion.Euler(currentRotationOffset);
            }

            motionTarget.localScale = baseLocalScale + currentScaleOffset;
        }

        private static Vector3 EvaluateOffset(Vector3 amplitude, Vector3 frequency, float time, float seedBase)
        {
            return new Vector3(
                EvaluateAxis(amplitude.x, frequency.x, time, seedBase),
                EvaluateAxis(amplitude.y, frequency.y, time, seedBase + 5.31f),
                EvaluateAxis(amplitude.z, frequency.z, time, seedBase + 9.77f));
        }

        private static float EvaluateAxis(float amplitude, float frequency, float time, float axisSeed)
        {
            if (Mathf.Approximately(amplitude, 0f) || Mathf.Approximately(frequency, 0f))
            {
                return 0f;
            }

            var value = Mathf.PerlinNoise(axisSeed, time * Mathf.Abs(frequency));
            return (value * 2f - 1f) * amplitude;
        }
    }
}
