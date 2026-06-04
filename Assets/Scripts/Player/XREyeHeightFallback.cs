using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace VRARTeam04.Player
{
    [AddComponentMenu("XR/Locomotion/XR Eye Height Fallback (VR&AR Team 04)")]
    [DefaultExecutionOrder(10000)]
    public sealed class XREyeHeightFallback : MonoBehaviour
    {
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private float _fallbackEyeHeight = 1.7f;
        [SerializeField] private float _minimumValidEyeHeight = 0.6f;
        [SerializeField] private float _maximumValidEyeHeight = 2.1f;
        [SerializeField] private float _startupDelay = 0.5f;
        [SerializeField] private bool _applyOnceOnStart = true;
        [SerializeField] private bool _keepApplyingWhileOutsideRange = true;
        [SerializeField] private bool _debugLog = true;

        private static bool s_hasCachedCameraOffsetY;
        private static float s_cachedCameraOffsetY;

        private Coroutine _startupRoutine;
        private bool _startupDelayFinished;

        private void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponent<XROrigin>();
        }

        private void OnEnable()
        {
            ApplyCachedCameraOffsetY();

            if (_applyOnceOnStart)
                _startupRoutine = StartCoroutine(ApplyAfterStartupDelay());
        }

        private void OnDisable()
        {
            if (_startupRoutine != null)
            {
                StopCoroutine(_startupRoutine);
                _startupRoutine = null;
            }
        }

        private void LateUpdate()
        {
            if (_keepApplyingWhileOutsideRange && _startupDelayFinished)
                ApplyIfNeeded(false);
        }

        [ContextMenu("Apply Eye Height Fallback If Needed")]
        public void ApplyIfNeeded()
        {
            ApplyIfNeeded(true);
        }

        private void ApplyIfNeeded(bool logWhenValid)
        {
            if (_xrOrigin == null)
            {
                Debug.LogWarning("XREyeHeightFallback needs an XROrigin reference.", this);
                return;
            }

            float currentEyeHeight = _xrOrigin.CameraInOriginSpaceHeight;
            if (currentEyeHeight >= _minimumValidEyeHeight && currentEyeHeight <= _maximumValidEyeHeight)
            {
                CacheCameraOffsetY();

                if (_debugLog && logWhenValid)
                    Debug.Log($"[XREyeHeightFallback] Eye height is valid: {currentEyeHeight:F3}m", this);
                return;
            }

            var offsetObject = _xrOrigin.CameraFloorOffsetObject;
            if (offsetObject == null)
            {
                Debug.LogWarning("XROrigin has no Camera Floor Offset Object.", this);
                return;
            }

            var offsetTransform = offsetObject.transform;
            var localPosition = offsetTransform.localPosition;
            float correctionY = _fallbackEyeHeight - currentEyeHeight;
            localPosition.y += correctionY;
            offsetTransform.localPosition = localPosition;
            CacheCameraOffsetY(localPosition.y);

            foreach (var headBob in offsetObject.GetComponents<VRHeadBob>())
                headBob.AddBaseLocalPositionOffset(Vector3.up * correctionY);

            if (_debugLog)
            {
                Debug.Log(
                    $"[XREyeHeightFallback] Eye height was outside valid range ({currentEyeHeight:F3}m). " +
                    $"Adjusted Camera Offset Y to {localPosition.y:F3} so eye height targets {_fallbackEyeHeight:F3}m.",
                    this);
            }
        }

        private IEnumerator ApplyAfterStartupDelay()
        {
            if (_startupDelay > 0f)
                yield return new WaitForSeconds(_startupDelay);
            else
                yield return null;

            ApplyIfNeeded();
            _startupDelayFinished = true;
            _startupRoutine = null;
        }

        private void ApplyCachedCameraOffsetY()
        {
            if (!s_hasCachedCameraOffsetY || _xrOrigin == null)
                return;

            var offsetObject = _xrOrigin.CameraFloorOffsetObject;
            if (offsetObject == null)
                return;

            var offsetTransform = offsetObject.transform;
            var localPosition = offsetTransform.localPosition;
            float correctionY = s_cachedCameraOffsetY - localPosition.y;
            if (Mathf.Approximately(correctionY, 0f))
                return;

            localPosition.y = s_cachedCameraOffsetY;
            offsetTransform.localPosition = localPosition;

            foreach (var headBob in offsetObject.GetComponents<VRHeadBob>())
                headBob.AddBaseLocalPositionOffset(Vector3.up * correctionY);
        }

        private void CacheCameraOffsetY()
        {
            if (_xrOrigin == null || _xrOrigin.CameraFloorOffsetObject == null)
                return;

            CacheCameraOffsetY(_xrOrigin.CameraFloorOffsetObject.transform.localPosition.y);
        }

        private static void CacheCameraOffsetY(float cameraOffsetY)
        {
            s_cachedCameraOffsetY = cameraOffsetY;
            s_hasCachedCameraOffsetY = true;
        }
    }
}
