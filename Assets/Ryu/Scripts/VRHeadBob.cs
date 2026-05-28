using UnityEngine;
using UnityEngine.Events;

namespace VRARTeam04.Player
{
    /// <summary>
    /// 카메라 오프셋(=XR Origin 의 Camera Offset 자식)을 미세하게 sin 파로 흔들어 걸음 흔들림을 표현한다.
    /// + 정지 상태에서는 호흡에 의한 미세한 idle sway 를 적용해 "마네킹" 같은 정적감 제거.
    /// + 발이 바닥에 닿는 순간 OnFootstep 이벤트 발사 (외부 SurfaceFootstepPlayer 가 구독해서 표면별 사운드 재생).
    ///
    /// VR 멀미를 피하기 위해 모든 진폭은 일부러 매우 작게 두는 것을 추천.
    /// 헤드 트래킹이 적용되는 Main Camera 자체는 건드리지 않고, 그 부모인 Camera Offset 을 흔든다.
    ///
    /// 사용법:
    /// 1) XR Origin (XR Rig) 의 Camera Offset 게임오브젝트에 이 컴포넌트를 추가.
    /// 2) _bobAnchor 에 같은 Camera Offset 트랜스폼을 드래그 (자기 자신).
    /// 3) _xrOriginRoot 에 XR Origin 루트 트랜스폼을 드래그.
    /// 4) 발소리 처리는 같은 GameObject 에 SurfaceFootstepPlayer 컴포넌트를 추가하고
    ///    이 컴포넌트의 OnFootstep 이벤트에 연결하면 된다.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/VR Head Bob (VR&AR Team 04)")]
    public class VRHeadBob : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("실제로 흔들 트랜스폼. 보통 XR Origin 의 Camera Offset.")]
        private Transform _bobAnchor;

        [SerializeField, Tooltip("이동 속도를 측정할 루트. 보통 XR Origin 루트.")]
        private Transform _xrOriginRoot;

        [Header("Walk Bob Shape")]
        [SerializeField, Tooltip("초당 걸음 수. 1.8 정도면 일반적인 보행 빈도.")]
        private float _stepsPerSecond = 1.8f;

        [SerializeField, Tooltip("수직(위아래) 흔들림 진폭(미터). VR 에서는 0.02~0.03 정도가 안전.")]
        [Range(0f, 0.08f)]
        private float _verticalAmplitude = 0.022f;

        [SerializeField, Tooltip("수평(좌우) 흔들림 진폭(미터). 수직보다 살짝 작게.")]
        [Range(0f, 0.08f)]
        private float _horizontalAmplitude = 0.014f;

        [Header("Walk Speed Mapping")]
        [SerializeField, Tooltip("이 속도 이상으로 움직일 때 헤드밥 시작 (m/s).")]
        private float _minSpeed = 0.15f;

        [SerializeField, Tooltip("이 속도일 때 진폭 100% 도달 (m/s). MoveSpeed 와 동일하게 설정 권장.")]
        private float _fullAmplitudeSpeed = 1.4f;

        [SerializeField, Tooltip("진폭 변화 속도 (값이 클수록 더 빨리 켜지고 꺼짐).")]
        private float _amplitudeLerpSpeed = 6f;

        [Header("Strafe Lean (좌우 이동 시 기울기)")]
        [SerializeField, Tooltip("좌우 이동 시 카메라가 진행 방향으로 살짝 기울어진다.")]
        private bool _enableStrafeLean = true;

        [SerializeField, Tooltip("최대 기울기 각도(도). VR 멀미 방지 위해 1~3도 권장.")]
        [Range(0f, 6f)]
        private float _maxLeanDeg = 2f;

        [SerializeField, Tooltip("기울기 변화 속도. 클수록 더 즉각적으로 반응.")]
        private float _leanLerpSpeed = 5f;

        [SerializeField, Tooltip("기울기 방향이 반대로 들어가면 이 토글을 켜서 부호 반전.")]
        private bool _invertLean = false;

        [Header("Idle Micro-Sway (호흡 흔들림)")]
        [SerializeField, Tooltip("정지 상태에서 호흡으로 인한 미세 흔들림 활성화.")]
        private bool _enableIdleSway = true;

        [SerializeField, Tooltip("호흡 주기(초). 4초 = 분당 15회 호흡(편안한 상태).")]
        [Range(2f, 8f)]
        private float _breathingCycleSeconds = 4f;

        [SerializeField, Tooltip("정지 시 수직 흔들림 진폭(미터). 매우 작게(2~6mm).")]
        [Range(0f, 0.02f)]
        private float _idleVerticalAmplitude = 0.004f;

        [SerializeField, Tooltip("정지 시 좌우(롤) 기울기 진폭(도). 매우 작게.")]
        [Range(0f, 1f)]
        private float _idleRollAmplitudeDeg = 0.25f;

        [SerializeField, Tooltip("정지 시 미세한 좌우 이동 진폭(미터).")]
        [Range(0f, 0.01f)]
        private float _idleLateralAmplitude = 0.002f;

        [Header("Footstep Event")]
        [Tooltip("발이 바닥에 닿는 순간 호출. float 파라미터 = 현재 걸음 진폭(0..1), 외부에서 볼륨 가중치로 사용 가능.")]
        public UnityEvent<float> OnFootstep;

        [Header("Footstep Audio (Fallback, Optional)")]
        [SerializeField, Tooltip("SurfaceFootstepPlayer 를 안 쓰고 단일 발소리만 쓸 때 사용. 둘 다 설정하면 둘 다 재생되므로 보통은 비워둘 것.")]
        private AudioSource _footstepSource;
        [SerializeField] private AudioClip[] _footstepClips;

        [SerializeField, Tooltip("발소리 볼륨 랜덤 범위.")]
        private Vector2 _footstepVolumeRange = new Vector2(0.7f, 1.0f);

        [SerializeField, Tooltip("발소리 피치 랜덤 범위 (1 이 기본).")]
        private Vector2 _footstepPitchRange = new Vector2(0.92f, 1.08f);

        // 내부 상태
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _lastWorldPosition;
        private float _bobPhase;          // 라디안
        private float _idlePhase;         // 라디안
        private float _currentAmplitude;  // 0..1 (걸음)
        private float _previousSinValue;
        private float _currentLeanRatio;  // -1..+1 (-=왼쪽, +=오른쪽)

        private void Awake()
        {
            if (_bobAnchor == null) _bobAnchor = transform;
            _baseLocalPosition = _bobAnchor.localPosition;
            _baseLocalRotation = _bobAnchor.localRotation;

            if (_xrOriginRoot == null && _bobAnchor.parent != null)
                _xrOriginRoot = _bobAnchor.parent;

            if (_xrOriginRoot != null)
                _lastWorldPosition = _xrOriginRoot.position;

            // 시작 위상을 살짝 랜덤하게 - 같은 씬에 여러 캐릭터 있을 때 동기화 방지(미래 확장 대비)
            _idlePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnDisable()
        {
            if (_bobAnchor != null)
            {
                _bobAnchor.localPosition = _baseLocalPosition;
                _bobAnchor.localRotation = _baseLocalRotation;
            }
        }

        private void LateUpdate()
        {
            if (_bobAnchor == null || _xrOriginRoot == null) return;

            float dt = Mathf.Max(Time.deltaTime, 1e-5f);

            // ─── 1. 수평 속도 측정 ───────────────────────────
            Vector3 currentPos = _xrOriginRoot.position;
            Vector3 horizontalDelta = currentPos - _lastWorldPosition;
            horizontalDelta.y = 0f;
            float speed = horizontalDelta.magnitude / dt;
            _lastWorldPosition = currentPos;

            // ─── 2. 걸음 진폭 매핑 (0..1) ───────────────────
            float targetAmp = speed < _minSpeed
                ? 0f
                : Mathf.Clamp01((speed - _minSpeed) / Mathf.Max(_fullAmplitudeSpeed - _minSpeed, 1e-3f));

            _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmp, dt * _amplitudeLerpSpeed);

            // ─── 3. 걸음 bob 위상 갱신 ──────────────────────
            if (_currentAmplitude > 0.01f)
            {
                _bobPhase += dt * _stepsPerSecond * Mathf.PI * 2f;
                if (_bobPhase > Mathf.PI * 200f) _bobPhase -= Mathf.PI * 200f;
            }
            else
            {
                _bobPhase = 0f;
                _previousSinValue = 0f;
            }

            float walkSinVal = Mathf.Sin(_bobPhase);
            float walkCosHalfVal = Mathf.Cos(_bobPhase * 0.5f);

            Vector3 walkOffset = new Vector3(
                walkCosHalfVal * _horizontalAmplitude * _currentAmplitude,
                walkSinVal * _verticalAmplitude * _currentAmplitude,
                0f);

            // ─── 4. Idle sway 위상 + 오프셋 ──────────────────
            Vector3 idleOffset = Vector3.zero;
            float idleRollDeg = 0f;
            if (_enableIdleSway)
            {
                // 정지 상태 비율 = 1 - 걸음 진폭. 걸을 땐 자동으로 idle 약해짐.
                float idleWeight = 1f - _currentAmplitude;

                _idlePhase += dt * (Mathf.PI * 2f / Mathf.Max(_breathingCycleSeconds, 0.1f));
                if (_idlePhase > Mathf.PI * 200f) _idlePhase -= Mathf.PI * 200f;

                float breath = Mathf.Sin(_idlePhase);
                // 호흡은 들숨/날숨이 비대칭이라 cos(2x) 살짝 섞어 자연스러운 곡선
                float breathShaped = breath * 0.85f + Mathf.Sin(_idlePhase * 2f) * 0.15f;

                idleOffset = new Vector3(
                    Mathf.Cos(_idlePhase * 0.5f) * _idleLateralAmplitude,
                    breathShaped * _idleVerticalAmplitude,
                    0f) * idleWeight;

                idleRollDeg = Mathf.Sin(_idlePhase * 0.5f) * _idleRollAmplitudeDeg * idleWeight;
            }

            // ─── 5. Strafe lean ─────────────────────────────
            float strafeRollDeg = 0f;
            if (_enableStrafeLean)
            {
                // XR Origin 의 right 벡터(수평 성분만)에 속도 투영해서 횡속도 추출
                Vector3 originRight = _xrOriginRoot.right;
                originRight.y = 0f;

                float targetLeanRatio = 0f;
                if (originRight.sqrMagnitude > 1e-6f)
                {
                    originRight.Normalize();
                    Vector3 horizontalVelocity = horizontalDelta / dt;
                    float lateralSpeed = Vector3.Dot(horizontalVelocity, originRight);
                    targetLeanRatio = Mathf.Clamp(lateralSpeed / Mathf.Max(_fullAmplitudeSpeed, 0.1f), -1f, 1f);
                }

                _currentLeanRatio = Mathf.Lerp(_currentLeanRatio, targetLeanRatio, dt * _leanLerpSpeed);

                // 진행 방향으로 기울기: 오른쪽 이동(+) 시 카메라 오른쪽이 아래로(-Z roll)
                float signFlip = _invertLean ? 1f : -1f;
                strafeRollDeg = signFlip * _currentLeanRatio * _maxLeanDeg;
            }
            else
            {
                _currentLeanRatio = Mathf.Lerp(_currentLeanRatio, 0f, dt * _leanLerpSpeed);
            }

            // ─── 6. 적용 ────────────────────────────────────
            _bobAnchor.localPosition = _baseLocalPosition + walkOffset + idleOffset;
            _bobAnchor.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, idleRollDeg + strafeRollDeg);

            // ─── 7. 발소리 트리거: sin 이 음→양 교차 ────────
            if (speed >= _minSpeed && _currentAmplitude > 0.15f && _previousSinValue <= 0f && walkSinVal > 0f)
            {
                OnFootstep?.Invoke(_currentAmplitude);
                PlayFallbackFootstep();
            }
            _previousSinValue = walkSinVal;
        }

        /// <summary>Fallback 발소리. SurfaceFootstepPlayer 가 따로 있다면 보통은 비워둠.</summary>
        private void PlayFallbackFootstep()
        {
            if (_footstepSource == null || _footstepClips == null || _footstepClips.Length == 0) return;

            int idx = Random.Range(0, _footstepClips.Length);
            AudioClip clip = _footstepClips[idx];
            if (clip == null) return;

            _footstepSource.pitch = Random.Range(_footstepPitchRange.x, _footstepPitchRange.y);
            float volume = Random.Range(_footstepVolumeRange.x, _footstepVolumeRange.y);
            _footstepSource.PlayOneShot(clip, volume * _currentAmplitude);
        }
    }
}
