using UnityEngine;

/// <summary>
/// 한 번씩 재생되는 사운드 (door creak, light switch, lever, etc.) 를 위한 만능 컴포넌트.
///
/// 사용법:
/// 1) 사운드가 *나는 위치*의 GameObject (예: Door, Lever) 에 추가.
/// 2) AudioSource 컴포넌트도 같이 있어야 함. _audioSource 가 비어있으면 Awake 에서 자동 검색.
/// 3) Clips 배열에 클립 1~여러 개 드래그 (여러 개면 매번 랜덤 선택해서 반복감 ↓).
/// 4) 재생 방법 두 가지:
///    A. 인스펙터에서 UnityEvent 슬롯에 이 컴포넌트의 Play() 또는 PlayAt() 메서드 드래그
///       (예: Door.cs 의 OnOpen UnityEvent → OneShotPlayer.Play)
///    B. 다른 스크립트에서 GetComponent&lt;OneShotPlayer&gt;().Play() 직접 호출
///
/// 동일 GameObject 에 여러 OneShotPlayer 를 붙여서 "OpenSound", "CloseSound", "LockedSound" 처럼
/// 상황별로 분리해서 쓰는 것도 가능. AudioSource 는 공유해도 되고 각자 가져도 됨.
/// </summary>
[AddComponentMenu("XR/Audio/One Shot Player (VR&AR Team 04)")]
public class OneShotPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("재생할 AudioSource. 비워두면 같은 GameObject 에서 자동 검색.")]
    private AudioSource _audioSource;

    [Header("Clips")]
    [SerializeField, Tooltip("재생할 클립 리스트. 여러 개면 매번 랜덤 선택.")]
    private AudioClip[] _clips;

    [Header("Variation (반복감 줄이기)")]
    [SerializeField, Tooltip("볼륨 랜덤 범위. (min, max).")]
    private Vector2 _volumeRange = new Vector2(0.85f, 1.0f);

    [SerializeField, Tooltip("피치 랜덤 범위. 1 = 원본 속도. ±0.05~0.1 정도가 자연스러움.")]
    private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Behaviour")]
    [SerializeField, Tooltip("쿨다운 (초). 너무 빠르게 연속 호출되면 무시.")]
    private float _cooldown = 0.05f;

    [SerializeField, Tooltip("동시에 같은 클립 재생을 허용할지. OFF 면 새로 호출되면 이전 소리 중단.")]
    private bool _allowOverlap = true;

    private float _lastPlayTime = -999f;
    private bool _isLooping;

    private void Awake()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 같은 GameObject 위치에서 사운드 재생.
    /// UnityEvent 슬롯에 드래그할 때 이 메서드 선택.
    /// </summary>
    public void Play()
    {
        if (!CanPlay()) return;

        AudioClip clip = PickClip();
        if (clip == null) return;

        _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);

        if (_allowOverlap)
        {
            _audioSource.PlayOneShot(clip, Random.Range(_volumeRange.x, _volumeRange.y));
        }
        else
        {
            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.volume = Random.Range(_volumeRange.x, _volumeRange.y);
            _audioSource.Play();
        }

        _lastPlayTime = Time.time;
    }

    /// <summary>
    /// 임의의 월드 위치에서 사운드 재생. 적이 멀리서 으르렁거리는 경우 등.
    /// </summary>
    public void PlayAt(Vector3 worldPosition)
    {
        if (!CanPlay(requireAudioSource: false)) return;
        AudioClip clip = PickClip();
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, worldPosition,
            Random.Range(_volumeRange.x, _volumeRange.y));
        _lastPlayTime = Time.time;
    }

    /// <summary>
    /// 외부에서 특정 인덱스의 클립을 콕 집어 재생하고 싶을 때.
    /// </summary>
    public void PlayIndex(int index)
    {
        if (!CanPlay()) return;
        if (_clips == null || index < 0 || index >= _clips.Length) return;
        var clip = _clips[index];
        if (clip == null) return;
        _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
        _audioSource.PlayOneShot(clip, Random.Range(_volumeRange.x, _volumeRange.y));
        _lastPlayTime = Time.time;
    }

    public void PlayLoop()
    {
        if (_audioSource == null) return;

        AudioClip clip = PickClip();
        if (clip == null) return;

        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.loop = true;
        _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
        _audioSource.volume = Random.Range(_volumeRange.x, _volumeRange.y);
        _audioSource.Play();
        _isLooping = true;
    }

    public void StopLoop()
    {
        if (_audioSource == null || !_isLooping) return;

        _audioSource.Stop();
        _audioSource.loop = false;
        _isLooping = false;
    }

    private bool CanPlay()
    {
        return CanPlay(requireAudioSource: true);
    }

    private bool CanPlay(bool requireAudioSource)
    {
        if (requireAudioSource && _audioSource == null) return false;
        if (Time.time - _lastPlayTime < _cooldown) return false;
        return true;
    }

    private AudioClip PickClip()
    {
        if (_clips == null || _clips.Length == 0) return null;
        return _clips[Random.Range(0, _clips.Length)];
    }
}