using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class Elevator : MonoBehaviour
{
    private static readonly int _openHash = Animator.StringToHash("Open");

    private static readonly string _openAnimationTrigger = "Open";
    private static readonly string _closeAnimationTrigger = "Close";

    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _doorCollider;
    [SerializeField] private bool _isExitElevator = false;

    [Header("Floor Announcement")]
    [SerializeField] private bool _playFloorAnnouncementOnOpen = true;
    [SerializeField] private AudioSource _floorAnnouncementAudioSource;
    [SerializeField] private AudioClip[] _floorAnnouncementClips;
    [SerializeField, Range(0f, 1f)] private float _floorAnnouncementVolume = 1f;
    [SerializeField] private bool _stopPreviousAnnouncement = true;
    [SerializeField] private TextMeshProUGUI _floorText;

    [Header("Triggers")]
    public Collider openTrigger;
    public Collider closeTrigger;

    [Header("Events")]
    [Tooltip("Open() 이 발동될 때 (Animator 트리거 직후) 호출. OneShotPlayer.Play 같은 사운드 연결용.")]
    public UnityEvent OnOpenTriggered;

    [Tooltip("Close() 가 발동될 때 (Animator 트리거 직후) 호출.")]
    public UnityEvent OnCloseTriggered;

    /// <summary>현재 문이 열린 상태인지. 외부에서 read-only 로 조회 가능 (BoolStateOneShot 감시 대상).</summary>
    public bool IsOpen { get; private set; } = true;   // 초기 상태가 Open 으로 시작한다고 가정

    private bool HasOpened => _animator != null && _animator.speed != 0f; // 열린 적이 있는지 판단

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_floorAnnouncementAudioSource == null)
        {
            _floorAnnouncementAudioSource = GetComponent<AudioSource>();
        }
        Initialize();
    }

    public void Open()
    {
        openTrigger.enabled = false;
        closeTrigger.enabled = true;
        _doorCollider.enabled = false;
        PlayFloorAnnouncement();

        // 최초 호출 — 초기 일시정지 상태를 풀기만 (Animator 가 Open 프레임에서 멈춰있다가 재생 시작)
        if (!HasOpened)
        {
            _animator.speed = 1f;
            IsOpen = true;
            OnOpenTriggered?.Invoke();
            return;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // // 이미 열려있거나 아직 다 닫히지 않았다면 아무것도 하지 않음
        // if (stateInfo.IsName("Open")) return;
        // if (stateInfo.IsName("Close") && stateInfo.normalizedTime < 1.0f) return;
        // 삭제: 도중에 트리거에 닿았을 때 이벤트가 씹혀서 진행이 안됨.

        _animator.SetTrigger(_openAnimationTrigger);
        // _animator.speed = 1f;
    }

    public void Close()
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // // 이미 닫혀있거나 아직 다 열리지 않았다면 아무것도 하지 않음
        // if (stateInfo.IsName("Close")) return;
        // if (stateInfo.IsName("Open") && stateInfo.normalizedTime < 1.0f) return;

        _animator.SetTrigger(_closeAnimationTrigger);
        OnCloseTriggered?.Invoke();
        // _animator.speed = 1f;

        openTrigger.enabled = true;
        closeTrigger.enabled = false;
        _doorCollider.enabled = true;
    }

    public void Initialize()
    {
        // 처음에는 재생 중지 (Open과 Close 두 상태밖에 없고, 초기 상태는 Open. 최초로 여는 신호를 보내기 전까지는 speed를 0으로 설정)
        _animator.Play(_openHash, 0, 0f);
        _animator.speed = 0f;
        _animator.Update(0f);

        _animator.ResetTrigger(_openAnimationTrigger);
        _animator.ResetTrigger(_closeAnimationTrigger);

        _doorCollider.enabled = true;
        openTrigger.enabled = !_isExitElevator; // 출구 엘리베이터는 openTrigger도 꺼진 상태로 시작
        closeTrigger.enabled = false;
    }

    /// <summary>
    /// 애니메이션 이벤트에 의해 호출됨
    /// 출구 엘리베이터에 한해서, 호출 시점에 다음 스테이지 로드
    /// </summary>
    public void OnDoorClosed()
    {
        if (!_isExitElevator) return;

        GameManager.Instance.GoToNextStage(transform);
    }

    public void SetFloorText(int floor)
    {
        _floorText.text = floor.ToString();
    }

    private void PlayFloorAnnouncement()
    {
        if (!_playFloorAnnouncementOnOpen || _isExitElevator) return;
        if (_floorAnnouncementAudioSource == null || _floorAnnouncementClips == null) return;
        if (GameManager.Instance == null) return;

        int clipIndex = GameManager.Instance.currentStage;
        if (clipIndex < 0 || clipIndex >= _floorAnnouncementClips.Length) return;

        AudioClip clip = _floorAnnouncementClips[clipIndex];
        if (clip == null) return;

        if (_stopPreviousAnnouncement)
        {
            _floorAnnouncementAudioSource.Stop();
        }

        _floorAnnouncementAudioSource.PlayOneShot(clip, _floorAnnouncementVolume);
    }
}
