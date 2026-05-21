using UnityEngine;
using UnityEngine.Events;

public class Elevator : MonoBehaviour
{
    // 트리거 이름은 한 번만 hash 캐싱 — 오타·이름 변경 안전성 + 약간의 성능 이득
    private static readonly int _openTriggerHash  = Animator.StringToHash("Open");
    private static readonly int _closeTriggerHash = Animator.StringToHash("Close");

    [SerializeField] private Animator _animator;

    [Header("Events")]
    [Tooltip("Open() 이 발동될 때 (Animator 트리거 직후) 호출. OneShotPlayer.Play 같은 사운드 연결용.")]
    public UnityEvent OnOpenTriggered;

    [Tooltip("Close() 가 발동될 때 (Animator 트리거 직후) 호출.")]
    public UnityEvent OnCloseTriggered;

    /// <summary>현재 문이 열린 상태인지. 외부에서 read-only 로 조회 가능 (BoolStateOneShot 감시 대상).</summary>
    public bool IsOpen { get; private set; } = true;   // 초기 상태가 Open 으로 시작한다고 가정

    private bool _firstOpenDone = false;

    private void Awake()
    {
        // 처음에는 재생 중지 (Open과 Close 두 상태밖에 없고, 초기 상태는 Open. 최초로 여는 신호를 보내기 전까지는 speed를 0으로 설정)
        _animator.speed = 0f;
    }

    public void Open()
    {
        // 최초 호출 — 초기 일시정지 상태를 풀기만 (Animator 가 Open 프레임에서 멈춰있다가 재생 시작)
        if (!_firstOpenDone)
        {
            _firstOpenDone = true;
            _animator.speed = 1f;
            IsOpen = true;
            OnOpenTriggered?.Invoke();
            return;
        }

        // 이미 열린 상태면 멱등 처리 (중복 호출 시 사운드 중복 발사 방지)
        if (IsOpen) return;

        _animator.SetTrigger(_openTriggerHash);
        IsOpen = true;
        OnOpenTriggered?.Invoke();
    }

    public void Close()
    {
        // 최초 Open 도 아직 안 끝났으면 닫기 불가
        if (!_firstOpenDone) return;

        // 이미 닫힌 상태면 멱등 처리
        if (!IsOpen) return;

        _animator.SetTrigger(_closeTriggerHash);
        IsOpen = false;
        OnCloseTriggered?.Invoke();
    }
}
