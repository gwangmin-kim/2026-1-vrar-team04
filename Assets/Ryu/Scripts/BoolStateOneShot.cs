using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 임의의 컴포넌트가 가진 bool 필드/프로퍼티의 변화를 감시해서 사운드를 재생한다.
/// 상호작용 스크립트(Door, Lever, Flashlight 등)는 사운드 존재를 *전혀 모름*.
/// 이 컴포넌트만 추가하면 외부에서 상태 변화만으로 사운드가 자동 재생됨.
///
/// 사용 예:
///   Door 의 _isClosed 가 true 가 될 때 wooden_door.wav 재생
///   Lever 의 _isPowered 가 true 가 될 때 lever_latch.wav 재생
///   Flashlight 의 _isTurnedOn 이 토글될 때 flashlight.wav 재생 (양방향)
///
/// 한 GameObject 에 여러 개 붙여서 *서로 다른 bool* 을 감시할 수 있다.
/// (예: Door 에 _isEventTriggered 감시용 1개 + _isClosed 감시용 1개)
///
/// 동작:
/// - 매 프레임 reflection 으로 target 컴포넌트의 지정된 필드/프로퍼티 값 폴링
/// - 이전 프레임과 다르면 방향(false→true / true→false) 에 맞는 클립 배열에서 랜덤 재생
/// - private 필드도 BindingFlags.NonPublic 으로 접근 (SerializeField 로 노출된 private bool 도 OK)
/// </summary>
[AddComponentMenu("XR/Audio/Bool State OneShot (VR&AR Team 04)")]
public class BoolStateOneShot : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("감시할 컴포넌트. 같은 GameObject 의 Door, Lever, Flashlight 등을 드래그.")]
    private MonoBehaviour _target;

    [SerializeField, Tooltip("감시할 bool 필드 또는 프로퍼티 이름. private 도 OK. (예: _isClosed, _isPowered, _isTurnedOn)")]
    private string _booleanName = "";

    [Header("Audio")]
    [SerializeField, Tooltip("재생할 AudioSource. 비워두면 같은 GameObject 에서 자동 검색.")]
    private AudioSource _audioSource;

    [SerializeField, Tooltip("false → true 가 될 때 재생할 클립들 (여러 개면 랜덤).")]
    private AudioClip[] _onTrueClips;

    [SerializeField, Tooltip("true → false 가 될 때 재생할 클립들. 같은 사운드면 onTrue 와 같이 채워도 됨.")]
    private AudioClip[] _onFalseClips;

    [Header("Variation (반복감 줄이기)")]
    [SerializeField] private Vector2 _volumeRange = new Vector2(0.85f, 1.0f);
    [SerializeField] private Vector2 _pitchRange  = new Vector2(0.95f, 1.05f);
    [SerializeField, Tooltip("연속 호출 방지 쿨다운 (초).")]
    private float _cooldown = 0.05f;

    [Header("Initial Trigger")]
    [SerializeField, Tooltip("시작 시 bool 이 이미 true 면 onTrueClips 한 번 재생.")]
    private bool _playOnStartIfTrue = false;

    [SerializeField, Tooltip("시작 시 bool 이 이미 false 면 onFalseClips 한 번 재생.")]
    private bool _playOnStartIfFalse = false;

    [Header("Events (Optional)")]
    [Tooltip("값이 false → true 로 바뀐 직후 호출.")]
    public UnityEvent OnBecameTrue;

    [Tooltip("값이 true → false 로 바뀐 직후 호출.")]
    public UnityEvent OnBecameFalse;

    // 내부
    private FieldInfo _field;
    private PropertyInfo _property;
    private bool _lastValue;
    private bool _initialized;
    private float _lastPlayTime = -999f;

    private void Awake()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        ResolveMember();
    }

    private void Start()
    {
        if (!TryRead(out bool current)) return;
        _lastValue = current;
        _initialized = true;

        if (current && _playOnStartIfTrue) PlayTrue();
        else if (!current && _playOnStartIfFalse) PlayFalse();
    }

    private void Update()
    {
        if (!_initialized || _target == null) return;
        if (!TryRead(out bool current)) return;
        if (current == _lastValue) return;

        _lastValue = current;
        if (current) PlayTrue();
        else PlayFalse();
    }

    private void ResolveMember()
    {
        _field = null;
        _property = null;
        if (_target == null || string.IsNullOrEmpty(_booleanName)) return;

        var type = _target.GetType();
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        _field = type.GetField(_booleanName, BF);
        if (_field != null && _field.FieldType != typeof(bool)) _field = null;

        if (_field == null)
        {
            _property = type.GetProperty(_booleanName, BF);
            if (_property != null && _property.PropertyType != typeof(bool)) _property = null;
        }

        if (_field == null && _property == null)
        {
            Debug.LogWarning(
                $"[BoolStateOneShot] '{_booleanName}' 이라는 bool 필드/프로퍼티를 {_target.GetType().Name} 에서 찾을 수 없음.",
                this);
        }
    }

    private bool TryRead(out bool value)
    {
        value = false;
        try
        {
            if (_field != null)    { value = (bool)_field.GetValue(_target);    return true; }
            if (_property != null) { value = (bool)_property.GetValue(_target); return true; }
        }
        catch { /* swallow — reflection failure, treat as no read */ }
        return false;
    }

    private void PlayTrue()
    {
        PlayFromArray(_onTrueClips);
        OnBecameTrue?.Invoke();
    }

    private void PlayFalse()
    {
        PlayFromArray(HasClips(_onFalseClips) ? _onFalseClips : _onTrueClips);
        OnBecameFalse?.Invoke();
    }

    private void PlayFromArray(AudioClip[] clips)
    {
        if (_audioSource == null) return;
        if (clips == null || clips.Length == 0) return;
        if (Time.time - _lastPlayTime < _cooldown) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
        _audioSource.PlayOneShot(clip, Random.Range(_volumeRange.x, _volumeRange.y));
        _lastPlayTime = Time.time;
    }

    private static bool HasClips(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return false;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null) return true;
        }

        return false;
    }

    /// <summary>외부에서 강제로 onTrue 사운드 재생.</summary>
    public void ForcePlayTrue()  => PlayFromArray(_onTrueClips);

    /// <summary>외부에서 강제로 onFalse 사운드 재생.</summary>
    public void ForcePlayFalse() => PlayFromArray(HasClips(_onFalseClips) ? _onFalseClips : _onTrueClips);

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터에서 필드명 바꾸면 즉시 재해결 (편의)
        if (Application.isPlaying) ResolveMember();
    }
#endif
}
