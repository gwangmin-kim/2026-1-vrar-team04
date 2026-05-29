using UnityEngine;

/// <summary>
/// Cleaner 의 linearSpeed 에 비례해서 mopping.wav 같은 지속음을 재생한다.
/// - 일정 속도 이상으로 움직이면 사운드가 페이드인
/// - 멈추면 부드럽게 페이드아웃
/// - 피치도 속도에 따라 미세 변동 (옵션)
///
/// 셋업:
/// 1) Mop GameObject 에 추가. (이미 Cleaner 가 붙어있어야 함)
/// 2) AudioSource 컴포넌트도 추가 (자동 검색)
/// 3) AudioSource 의 AudioClip 에 mopping.wav 드래그
/// 4) AudioSource 설정: Loop ON, Play On Awake OFF (이 스크립트가 자동 재생함)
///
/// 같은 패턴으로 발걸음 외 다른 지속음 (예: 청소기 모터, 에어컨 진동) 에도 응용 가능.
/// </summary>
[AddComponentMenu("Horror/Mopping Sound (VR&AR Team 04)")]
public class MoppingSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("속도를 읽어올 Cleaner. 비워두면 같은 GameObject 에서 자동 검색.")]
    private Cleaner _cleaner;

    [SerializeField, Tooltip("재생할 AudioSource. 비워두면 같은 GameObject 에서 자동 검색. Loop=ON 권장.")]
    private AudioSource _audioSource;

    [SerializeField, Tooltip("대걸레질 중 재생할 루프 사운드.")]
    private AudioClip _moppingClip;

    [Header("Volume Curve")]
    [SerializeField, Tooltip("이 속도 이하면 무음 (m/s).")]
    private float _minSpeed = 0.1f;

    [SerializeField, Tooltip("이 속도일 때 풀 볼륨 도달 (m/s).")]
    private float _fullVolumeSpeed = 1.0f;

    [SerializeField, Tooltip("최대 볼륨 (0~1).")]
    [Range(0f, 1f)]
    private float _maxVolume = 0.8f;

    [SerializeField, Tooltip("볼륨 변화 속도. 클수록 즉각 반응.")]
    private float _volumeLerpSpeed = 8f;

    [Header("Pitch")]
    [SerializeField, Tooltip("ON 이면 속도가 빠를수록 피치도 올라감.")]
    private bool _pitchFollowsSpeed = true;

    [SerializeField, Tooltip("정지 직전 피치 (보통 살짝 낮게).")]
    private float _minPitch = 0.9f;

    [SerializeField, Tooltip("최대 속도일 때 피치.")]
    private float _maxPitch = 1.1f;

    [SerializeField, Tooltip("피치 변화 속도.")]
    private float _pitchLerpSpeed = 6f;

    private float _currentVolume;
    private float _currentPitch = 1f;

    private void Awake()
    {
        if (_cleaner == null) _cleaner = GetComponent<Cleaner>();
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

        if (_audioSource != null)
        {
            if (_moppingClip != null)
                _audioSource.clip = _moppingClip;

            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0f;
        }
    }

    private void OnEnable()
    {
        if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }

    private void OnDisable()
    {
        if (_audioSource != null) _audioSource.volume = 0f;
        _currentVolume = 0f;
    }

    private void Update()
    {
        if (_audioSource == null || _cleaner == null) return;

        float speed = _cleaner.linearSpeed;

        // 목표 볼륨 계산
        float targetVolume = 0f;
        if (speed >= _minSpeed)
        {
            float t = Mathf.Clamp01((speed - _minSpeed) / Mathf.Max(_fullVolumeSpeed - _minSpeed, 0.01f));
            targetVolume = t * _maxVolume;
        }

        _currentVolume = Mathf.Lerp(_currentVolume, targetVolume, Time.deltaTime * _volumeLerpSpeed);
        _audioSource.volume = _currentVolume;

        // 피치
        if (_pitchFollowsSpeed)
        {
            float pitchT = Mathf.Clamp01(speed / Mathf.Max(_fullVolumeSpeed, 0.01f));
            float targetPitch = Mathf.Lerp(_minPitch, _maxPitch, pitchT);
            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * _pitchLerpSpeed);
            _audioSource.pitch = _currentPitch;
        }
    }
}
