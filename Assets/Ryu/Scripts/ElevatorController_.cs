using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 엘리베이터 시퀀스 컨트롤러.
///
/// 시퀀스 흐름:
/// 1) 도착 ding (_bingClip)
/// 2) 문 열림 (_doorOpenClip) → OnDoorOpened UnityEvent
/// 3) 탑승 대기 (_enterDuration)
/// 4) 문 닫힘 → OnDoorClosed
/// 5) 이동 시작 (_movingClip 루프) → OnTravelStarted
/// 6) 이동 (_travelDuration) — 이 사이에 페이드·씬 전환 같은 거 OnTravelStarted 에 훅
/// 7) 멈춤 (_stoppedClip) → OnTravelEnded
/// 8) 도착 ding → 문 열림
/// 9) 시퀀스 종료 → OnSequenceComplete
///
/// 8번출구 스타일이면 4번~9번 사이에 *층 안의 변화*가 일어남.
/// </summary>
[AddComponentMenu("Horror/Elevator Controller (VR&AR Team 04)")]
public class ElevatorController : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField, Tooltip("사운드를 재생할 AudioSource. PlayOneShot 호출용. 비워두면 자동 검색.")]
    private AudioSource _audioSource;

    [Header("Audio Clips")]
    [SerializeField, Tooltip("문 열림 효과음 (elevator_open.wav).")]
    private AudioClip _doorOpenClip;

    [SerializeField, Tooltip("도착/출발 ding (elevator_bing.wav).")]
    private AudioClip _bingClip;

    [SerializeField, Tooltip("이동 중 루프 사운드 (elevator_down.wav).")]
    private AudioClip _movingClip;

    [SerializeField, Tooltip("멈춤 효과음 (elevator_stopped.wav).")]
    private AudioClip _stoppedClip;

    [SerializeField, Tooltip("문 닫힘 사운드. 비워두면 _doorOpenClip 을 재사용 (피치 살짝 낮춰서).")]
    private AudioClip _doorCloseClip;

    [Header("Sequence Timing")]
    [SerializeField, Tooltip("도착 ding 부터 문 열림까지 대기 (초).")]
    private float _bingToOpenDelay = 0.4f;

    [SerializeField, Tooltip("문 열린 후 탑승 가능 대기 시간 (초).")]
    private float _enterDuration = 4.0f;

    [SerializeField, Tooltip("문 닫히고 이동 시작까지 대기 (초).")]
    private float _closeToMoveDelay = 0.8f;

    [SerializeField, Tooltip("이동 지속 시간 (초). 8번출구 스타일이면 이 사이에 층 변화 발생.")]
    private float _travelDuration = 6.0f;

    [SerializeField, Tooltip("이동 종료 후 도착 ding 까지 대기 (초).")]
    private float _stopToBingDelay = 0.5f;

    [Header("Sequence Options")]
    [SerializeField, Tooltip("시퀀스 시작 시 도착 ding 부터 재생할지. OFF 면 문 열림부터 시작.")]
    private bool _startWithBing = true;

    [SerializeField, Tooltip("최종 도착 후 자동으로 문 다시 열어줄지.")]
    private bool _openAfterArrive = true;

    [Header("Events")]
    [Tooltip("문이 열리는 *시작* 시점에 호출. 문 애니메이션 트리거 연결.")]
    public UnityEvent OnDoorOpened;

    [Tooltip("문이 닫히는 *시작* 시점에 호출.")]
    public UnityEvent OnDoorClosed;

    [Tooltip("이동이 시작되는 시점에 호출. 화면 페이드·씬 전환·중력 변경 등 훅 지점.")]
    public UnityEvent OnTravelStarted;

    [Tooltip("이동이 끝나는 시점에 호출. 페이드 인·씬 정리 등.")]
    public UnityEvent OnTravelEnded;

    [Tooltip("전체 시퀀스가 끝난 시점에 호출.")]
    public UnityEvent OnSequenceComplete;

    private bool _isRunning;
    public bool IsRunning => _isRunning;

    private void Awake()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 전체 시퀀스 시작. 이미 실행 중이면 무시.
    /// 보통 외부 트리거(버튼·트리거 콜라이더)에서 호출.
    /// </summary>
    public void StartSequence()
    {
        if (_isRunning) return;
        StartCoroutine(RunSequence());
    }

    /// <summary>도착 + 문 열림만 (시퀀스 시작용).</summary>
    public void ArriveAndOpen()
    {
        if (_isRunning) return;
        StartCoroutine(ArriveAndOpenRoutine());
    }

    /// <summary>문 닫고 출발 (탑승 후 외부 트리거용).</summary>
    public void CloseAndTravel()
    {
        if (_isRunning) return;
        StartCoroutine(CloseAndTravelRoutine());
    }

    private IEnumerator RunSequence()
    {
        _isRunning = true;

        if (_startWithBing)
        {
            yield return ArriveAndOpenRoutine_Inner();
            yield return new WaitForSeconds(_enterDuration);
        }

        yield return CloseAndTravelRoutine_Inner();

        if (_openAfterArrive)
        {
            yield return new WaitForSeconds(_stopToBingDelay);
            yield return ArriveAndOpenRoutine_Inner();
        }

        OnSequenceComplete?.Invoke();
        _isRunning = false;
    }

    private IEnumerator ArriveAndOpenRoutine()
    {
        _isRunning = true;
        yield return ArriveAndOpenRoutine_Inner();
        _isRunning = false;
    }

    private IEnumerator ArriveAndOpenRoutine_Inner()
    {
        PlayOneShot(_bingClip);
        yield return new WaitForSeconds(_bingToOpenDelay);
        PlayOneShot(_doorOpenClip);
        OnDoorOpened?.Invoke();
    }

    private IEnumerator CloseAndTravelRoutine()
    {
        _isRunning = true;
        yield return CloseAndTravelRoutine_Inner();
        _isRunning = false;
    }

    private IEnumerator CloseAndTravelRoutine_Inner()
    {
        // 문 닫힘
        if (_doorCloseClip != null) PlayOneShot(_doorCloseClip);
        else PlayOneShot(_doorOpenClip, pitch: 0.85f); // 같은 클립을 살짝 낮게 재생해서 닫힘 느낌
        OnDoorClosed?.Invoke();
        yield return new WaitForSeconds(_closeToMoveDelay);

        // 이동
        OnTravelStarted?.Invoke();
        if (_movingClip != null && _audioSource != null)
        {
            _audioSource.clip = _movingClip;
            _audioSource.loop = true;
            _audioSource.pitch = 1f;
            _audioSource.Play();
        }
        yield return new WaitForSeconds(_travelDuration);

        // 정지
        if (_audioSource != null && _audioSource.clip == _movingClip)
        {
            _audioSource.Stop();
            _audioSource.loop = false;
        }
        PlayOneShot(_stoppedClip);
        OnTravelEnded?.Invoke();
    }

    private void PlayOneShot(AudioClip clip, float pitch = 1f, float volume = 1f)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip, volume);
    }
}
