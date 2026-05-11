using UnityEngine;

namespace VRARTeam04.Player
{
    /// <summary>
    /// 카메라 오프셋(=XR Origin 의 Camera Offset 자식)을 미세하게 sin 파로 흔들어 걸음 흔들림을 표현한다.
    /// - VR 멀미를 피하기 위해 진폭은 일부러 매우 작게(2~3cm) 두는 것을 추천.
    /// - 헤드 트래킹이 적용되는 Main Camera 자체는 건드리지 않고, 그 부모인 Camera Offset 을 흔든다.
    /// - bob 사이클이 바닥에 닿는 순간 footstepSource 로 발소리를 재생한다.
    ///
    /// 사용법:
    /// 1) XR Origin (XR Rig) 의 Camera Offset 게임오브젝트에 이 컴포넌트를 추가.
    /// 2) _bobAnchor 에 같은 Camera Offset 트랜스폼을 드래그 (자기 자신).
    /// 3) _xrOriginRoot 에 XR Origin 루트 트랜스폼을 드래그. (수평 이동 속도 측정용)
    /// 4) _footstepSource 와 _footstepClips 를 채우면 발소리도 함께 재생.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/VR Head Bob (VR&AR Team 04)")]
    public class VRHeadBob : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("실제로 흔들 트랜스폼. 보통 XR Origin 의 Camera Offset.")]
        private Transform _bobAnchor;

        [SerializeField, Tooltip("이동 속도를 측정할 루트. 보통 XR Origin 루트.")]
        private Transform _xrOriginRoot;

        [Header("Bob Shape")]
        [SerializeField, Tooltip("초당 걸음 수. 1.8 정도면 일반적인 보행 빈도.")]
        private float _stepsPerSecond = 1.8f;

        [SerializeField, Tooltip("수직(위아래) 흔들림 진폭(미터). VR 에서는 0.02~0.03 정도가 안전.")]
        [Range(0f, 0.08f)]
        private float _verticalAmplitude = 0.022f;

        [SerializeField, Tooltip("수평(좌우) 흔들림 진폭(미터). 수직보다 살짝 작게.")]
        [Range(0f, 0.08f)]
        private float _horizontalAmplitude = 0.014f;

        [Header("Speed Mapping")]
        [SerializeField, Tooltip("이 속도 이상으로 움직일 때 헤드밥 시작 (m/s).")]
        private float _minSpeed = 0.15f;

        [SerializeField, Tooltip("이 속도일 때 진폭 100% 도달 (m/s). MoveSpeed 와 동일하게 설정 권장.")]
        private float _fullAmplitudeSpeed = 1.4f;

        [SerializeField, Tooltip("진폭 변화 속도 (값이 클수록 더 빨리 켜지고 꺼짐).")]
        private float _amplitudeLerpSpeed = 6f;

        [Header("Footstep Audio (Optional)")]
        [SerializeField] private AudioSource _footstepSource;
        [SerializeField] private AudioClip[] _footstepClips;

        [SerializeField, Tooltip("발소리 볼륨 랜덤 범위.")]
        private Vector2 _footstepVolumeRange = new Vector2(0.7f, 1.0f);

        [SerializeField, Tooltip("발소리 피치 랜덤 범위 (1 이 기본).")]
        private Vector2 _footstepPitchRange = new Vector2(0.92f, 1.08f);

        // 내부 상태
        private Vector3 _baseLocalPosition;
        private Vector3 _lastWorldPosition;
        private float _bobPhase;          // 라디안
        private float _currentAmplitude;  // 0..1
        private float _previousSinValue;

        private void Awake()
        {
            if (_bobAnchor == null) _bobAnchor = transform;
            _baseLocalPosition = _bobAnchor.localPosition;

            if (_xrOriginRoot == null && _bobAnchor.parent != null)
                _xrOriginRoot = _bobAnchor.parent;

            if (_xrOriginRoot != null)
                _lastWorldPosition = _xrOriginRoot.position;
        }

        private void OnDisable()
        {
            // 컴포넌트 꺼지면 흔들림 원위치
            if (_bobAnchor != null)
                _bobAnchor.localPosition = _baseLocalPosition;
        }

        private void LateUpdate()
        {
            if (_bobAnchor == null || _xrOriginRoot == null) return;

            // 수평 속도 측정 (XR Origin 본체가 얼마나 움직였는가)
            Vector3 currentPos = _xrOriginRoot.position;
            Vector3 horizontalDelta = currentPos - _lastWorldPosition;
            horizontalDelta.y = 0f;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            float speed = horizontalDelta.magnitude / dt;
            _lastWorldPosition = currentPos;

            // 속도 -> 목표 진폭(0..1) 매핑
            float targetAmp;
            if (speed < _minSpeed)
                targetAmp = 0f;
            else
                targetAmp = Mathf.Clamp01((speed - _minSpeed) / Mathf.Max(_fullAmplitudeSpeed - _minSpeed, 1e-3f));

            _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmp, dt * _amplitudeLerpSpeed);

            // bob 사이클 진행 (멈춰있으면 위상 리셋)
            if (_currentAmplitude > 0.01f)
            {
                _bobPhase += dt * _stepsPerSecond * Mathf.PI * 2f;
                if (_bobPhase > Mathf.PI * 200f) _bobPhase -= Mathf.PI * 200f; // 오버플로우 방지
            }
            else
            {
                _bobPhase = 0f;
                _previousSinValue = 0f;
            }

            // 흔들림 적용 - 수직은 sin(t), 수평은 cos(t/2) 로 8자 곡선
            float sinVal = Mathf.Sin(_bobPhase);
            float cosHalfVal = Mathf.Cos(_bobPhase * 0.5f);

            Vector3 offset = new Vector3(
                cosHalfVal * _horizontalAmplitude * _currentAmplitude,
                sinVal * _verticalAmplitude * _currentAmplitude,
                0f);

            _bobAnchor.localPosition = _baseLocalPosition + offset;

            // 발소리: sin 이 음 -> 양 으로 교차하는 순간 = 발이 바닥에 닿는 순간 (한 사이클당 1번)
            if (_currentAmplitude > 0.15f && _previousSinValue <= 0f && sinVal > 0f)
            {
                PlayFootstep();
            }
            _previousSinValue = sinVal;
        }

        private void PlayFootstep()
        {
            if (_footstepSource == null || _footstepClips == null || _footstepClips.Length == 0) return;

            int idx = Random.Range(0, _footstepClips.Length);
            AudioClip clip = _footstepClips[idx];
            if (clip == null) return;

            _footstepSource.pitch = Random.Range(_footstepPitchRange.x, _footstepPitchRange.y);
            float volume = Random.Range(_footstepVolumeRange.x, _footstepVolumeRange.y);
            // 진폭(=실제 걷는 정도)에 비례해 볼륨 감쇠
            _footstepSource.PlayOneShot(clip, volume * _currentAmplitude);
        }
    }
}
