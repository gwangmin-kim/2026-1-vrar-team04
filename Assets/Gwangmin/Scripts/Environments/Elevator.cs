using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Elevator : MonoBehaviour
{
    private static readonly int _openHash = Animator.StringToHash("Open");

    private static readonly string _openAnimationTrigger = "Open";
    private static readonly string _closeAnimationTrigger = "Close";

    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _doorCollider;
    [SerializeField] private bool _isExitElevator = false;

    [Header("Triggers")]
    public Collider openTrigger;
    public Collider closeTrigger;

    private void Awake()
    {
        if (_animator == null)
        {
            GetComponent<Animator>();
        }
    }

    public void Open()
    {
        // 최초 호출 시에만 적용
        if (_animator.speed == 0f)
        {
            _animator.speed = 1f;
            return;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // 이미 열려있거나 아직 다 닫히지 않았다면 아무것도 하지 않음
        if (stateInfo.IsName("Open")) return;
        if (stateInfo.IsName("Close") && stateInfo.normalizedTime < 1.0f) return;

        _animator.SetTrigger(_openAnimationTrigger);
        // _animator.speed = 1f;

        openTrigger.enabled = false;
        closeTrigger.enabled = true;
        _doorCollider.enabled = false;
    }

    public void Close()
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // 이미 닫혀있거나 아직 다 열리지 않았다면 아무것도 하지 않음
        if (stateInfo.IsName("Close")) return;
        if (stateInfo.IsName("Open") && stateInfo.normalizedTime < 1.0f) return;

        _animator.SetTrigger(_closeAnimationTrigger);
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
        openTrigger.enabled = true;
        closeTrigger.enabled = false;
    }

    /// <summary>
    /// 애니메이션 이벤트에 의해 호출됨
    /// 출구 엘리베이터에 한해서, 호출 시점에 다음 스테이지 로드
    /// </summary>
    public void OnDoorClosed()
    {
        if (!_isExitElevator) return;

        GameManager.Instance.GoToNextStage();
    }
}
