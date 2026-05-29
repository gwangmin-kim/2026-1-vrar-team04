using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 형광등이 자연스럽게 깜빡이게 만들고, 깜빡일 때 flickering.wav 같은 사운드를 동기화한다.
///
/// 동작 방식:
/// - 평소엔 _baseIntensity 로 안정적으로 켜져있음
/// - 일정 시간(_stableDurationRange) 지나면 "burst" 발동: 짧게 여러 번 깜빡임
/// - burst 중에는 0(꺼짐)과 랜덤 강도(켜짐) 사이를 빠르게 왕복
/// - 깜빡일 때마다(또는 burst 시작 시 한 번) 사운드 재생
/// - 외부에서 TriggerFlicker() 호출하면 즉시 한 번 발동 (예: 유령 근접 시)
/// </summary>
[AddComponentMenu("Horror/Flickering Light (VR&AR Team 04)")]
public class FlickeringLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("깜빡일 라이트. 비워두면 같은 GameObject 에서 자동 검색.")]
    private Light _light;

    [SerializeField, Tooltip("사운드를 재생할 AudioSource. 비워두면 같은 GameObject 에서 자동 검색.")]
    private AudioSource _audioSource;

    [SerializeField, Tooltip("깜빡일 때 재생할 클립 (예: flickering.wav).")]
    private AudioClip _flickerClip;

    [Header("Stable State (안정 구간)")]
    [SerializeField, Tooltip("평상시 라이트 강도.")]
    private float _baseIntensity = 1f;

    [SerializeField, Tooltip("다음 burst 까지 대기 시간 (초). 호러 분위기는 길수록 좋음.")]
    private Vector2 _stableDurationRange = new Vector2(6f, 18f);

    [Header("Flicker Burst (깜빡임 한 묶음)")]
    [SerializeField, Tooltip("한 burst 안에서 깜빡일 횟수 (min, max).")]
    private Vector2Int _flickerCountRange = new Vector2Int(3, 8);

    [SerializeField, Tooltip("켜짐 상태 지속 시간 (초). 짧을수록 다급한 느낌.")]
    private Vector2 _flickerOnTime = new Vector2(0.05f, 0.18f);

    [SerializeField, Tooltip("꺼짐 상태 지속 시간 (초).")]
    private Vector2 _flickerOffTime = new Vector2(0.02f, 0.10f);

    [SerializeField, Tooltip("깜빡일 때 강도 배수 랜덤. 1 이상이면 가끔 더 밝게 튐.")]
    private Vector2 _flickerIntensityMultiplier = new Vector2(0.15f, 1.15f);

    [Header("Audio")]
    [SerializeField, Tooltip("ON 되는 매 순간마다 사운드 재생. OFF 면 burst 시작에 한 번만.")]
    private bool _playSoundEachFlicker = true;

    [SerializeField, Tooltip("사운드 볼륨 랜덤 범위.")]
    private Vector2 _audioVolumeRange = new Vector2(0.6f, 1.0f);

    [SerializeField, Tooltip("사운드 피치 랜덤 범위.")]
    private Vector2 _audioPitchRange = new Vector2(0.92f, 1.08f);

    [Header("Auto Start")]
    [SerializeField, Tooltip("OnEnable 시 자동으로 깜빡임 루프 시작.")]
    private bool _autoStart = true;

    [Header("Events")]
    public UnityEvent OnFlickerStart;
    public UnityEvent OnFlickerEnd;

    private Coroutine _loopCoroutine;
    private bool _isInBurst;

    private void Awake()
    {
        if (_light == null) _light = GetComponent<Light>();
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (_light != null) _light.intensity = _baseIntensity;
        if (_autoStart) StartLoop();
    }

    private void OnDisable()
    {
        StopLoop();
        if (_light != null) _light.intensity = _baseIntensity;
    }

    /// <summary>자동 루프 시작.</summary>
    public void StartLoop()
    {
        if (_loopCoroutine != null) return;
        _loopCoroutine = StartCoroutine(FlickerLoop());
    }

    /// <summary>자동 루프 중단. 라이트는 base intensity 로 복귀.</summary>
    public void StopLoop()
    {
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
        _isInBurst = false;
        if (_light != null) _light.intensity = _baseIntensity;
    }

    /// <summary>외부 트리거: 지금 즉시 한 번 깜빡임. (예: 유령 근접, 점프스케어 직전)</summary>
    public void TriggerFlicker()
    {
        if (_isInBurst) return;
        StopLoop();
        StartCoroutine(SingleBurstThenResume());
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            float wait = Random.Range(_stableDurationRange.x, _stableDurationRange.y);
            yield return new WaitForSeconds(wait);
            yield return DoBurst();
        }
    }

    private IEnumerator SingleBurstThenResume()
    {
        yield return DoBurst();
        if (_autoStart) StartLoop();
    }

    private IEnumerator DoBurst()
    {
        _isInBurst = true;
        OnFlickerStart?.Invoke();

        if (!_playSoundEachFlicker) PlayFlickerSound();

        int count = Random.Range(_flickerCountRange.x, _flickerCountRange.y + 1);
        for (int i = 0; i < count; i++)
        {
            // 꺼짐
            if (_light != null) _light.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(_flickerOffTime.x, _flickerOffTime.y));

            // 켜짐 (랜덤 강도)
            if (_light != null)
            {
                float mult = Random.Range(_flickerIntensityMultiplier.x, _flickerIntensityMultiplier.y);
                _light.intensity = _baseIntensity * mult;
            }
            if (_playSoundEachFlicker) PlayFlickerSound();
            yield return new WaitForSeconds(Random.Range(_flickerOnTime.x, _flickerOnTime.y));
        }

        // 안정 상태 복귀
        if (_light != null) _light.intensity = _baseIntensity;
        _isInBurst = false;
        OnFlickerEnd?.Invoke();
    }

    private void PlayFlickerSound()
    {
        if (_audioSource == null || _flickerClip == null) return;
        _audioSource.pitch = Random.Range(_audioPitchRange.x, _audioPitchRange.y);
        _audioSource.PlayOneShot(_flickerClip, Random.Range(_audioVolumeRange.x, _audioVolumeRange.y));
    }
}
