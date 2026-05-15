using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRARTeam04.Player
{
    /// <summary>
    /// Snap Turn 과 Continuous Turn 의 절충안.
    /// 스틱을 좌/우로 한 번 밀면 _turnAmount 만큼의 각도를 _turnDuration 시간 동안 부드럽게 회전한다.
    /// - 즉시 점프하지 않으므로 "뚝뚝" 끊기지 않음.
    /// - 일정 각도 단위로만 회전하므로 시각 흐름이 길지 않아 멀미도 적음.
    /// - 스틱을 한 번 중립으로 되돌려야 다음 회전이 발동 (홀딩으로 무한 회전 방지).
    ///
    /// 셋업:
    /// 1) 기존 ContinuousTurnProvider / SnapTurnProvider 컴포넌트는 제거 (또는 비활성화).
    /// 2) XR Origin 의 같은 GameObject 또는 그 자식 빈 GameObject 에 이 컴포넌트 추가.
    /// 3) _xrOrigin 슬롯에 XR Origin (XR Rig) 컴포넌트를 가진 오브젝트를 드래그.
    /// 4) _turnAction 에 InputActionReference 연결 (예: XRI Right Locomotion/Turn).
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Smooth Snap Turn Provider (VR&AR Team 04)")]
    public class SmoothSnapTurnProvider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("회전시킬 XR Origin. 비워두면 부모/씬에서 자동 검색.")]
        private XROrigin _xrOrigin;

        [SerializeField, Tooltip("Vector2 형식의 턴 입력. XRI Right Locomotion/Turn 같은 InputActionReference 를 드래그하면 된다.")]
        private InputActionReference _turnAction;

        [Header("Turn Shape")]
        [SerializeField, Tooltip("한 번 회전 시 적용할 각도(도). 일반적으로 30~45.")]
        private float _turnAmount = 30f;

        [SerializeField, Tooltip("회전을 완료하기까지 걸리는 시간(초). 0.10~0.20 사이 권장.")]
        [Range(0.05f, 0.5f)]
        private float _turnDuration = 0.15f;

        [SerializeField, Tooltip("회전 가속/감속 곡선. 기본 EaseInOut.")]
        private AnimationCurve _turnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Input Behavior")]
        [SerializeField, Tooltip("스틱 |x| 가 이 값 이상이면 새 회전 발동.")]
        [Range(0.1f, 1f)]
        private float _activationThreshold = 0.5f;

        [SerializeField, Tooltip("스틱 |x| 가 이 값 이하로 떨어져야 다음 회전 발동 가능.")]
        [Range(0.0f, 1f)]
        private float _deactivationThreshold = 0.3f;

        [SerializeField, Tooltip("한 번 회전이 끝난 뒤 추가 입력을 잠깐 무시할 시간(초).")]
        private float _postTurnCooldown = 0.08f;

        // 내부 상태
        private bool _isTurning;
        private bool _stickReadyForNextTurn = true;
        private float _cooldownRemaining;

        private void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponentInParent<XROrigin>();
            if (_xrOrigin == null)
                _xrOrigin = FindAnyObjectByType<XROrigin>();
        }

        private void OnEnable()
        {
            if (_turnAction != null && _turnAction.action != null)
                _turnAction.action.Enable();
        }

        private void Update()
        {
            if (_isTurning) return;

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
                return;
            }

            if (_turnAction == null) return;
            var action = _turnAction.action;
            if (action == null) return;

            Vector2 input = action.ReadValue<Vector2>();
            float x = input.x;

            // 스틱이 중립권으로 돌아온 적이 있어야 다음 회전 발동 (홀딩 방지)
            if (Mathf.Abs(x) < _deactivationThreshold)
            {
                _stickReadyForNextTurn = true;
                return;
            }

            if (!_stickReadyForNextTurn) return;

            if (Mathf.Abs(x) >= _activationThreshold)
            {
                _stickReadyForNextTurn = false;
                float dir = Mathf.Sign(x);
                StartCoroutine(DoSmoothTurn(dir * _turnAmount));
            }
        }

        private IEnumerator DoSmoothTurn(float totalAngle)
        {
            _isTurning = true;

            float elapsed = 0f;
            float appliedSoFar = 0f;

            while (elapsed < _turnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _turnDuration);
                float eased = _turnCurve.Evaluate(t);
                float currentTotal = eased * totalAngle;
                float delta = currentTotal - appliedSoFar;
                appliedSoFar = currentTotal;

                if (_xrOrigin != null && Mathf.Abs(delta) > 1e-5f)
                {
                    // 카메라(머리) 위치를 축으로 회전 → 회전 후에도 시선 위치가 어긋나지 않음
                    _xrOrigin.RotateAroundCameraUsingOriginUp(delta);
                }

                yield return null;
            }

            // 누적 오차 보정
            float remainder = totalAngle - appliedSoFar;
            if (_xrOrigin != null && Mathf.Abs(remainder) > 1e-4f)
                _xrOrigin.RotateAroundCameraUsingOriginUp(remainder);

            _cooldownRemaining = _postTurnCooldown;
            _isTurning = false;
        }
    }
}
