using UnityEngine;

public class Elevator : MonoBehaviour
{
    private static readonly string _openTrigger = "Open";
    private static readonly string _closeTrigger = "Close";

    [SerializeField] private Animator _animator;

    private void Awake()
    {
        // 처음에는 재생 중지 (Open과 Close 두 상태밖에 없고, 초기 상태는 Open. 최초로 여는 신호를 보내기 전까지는 speed를 0으로 설정)
        _animator.speed = 0f;
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
        if (stateInfo.IsName("Close") && stateInfo.normalizedTime < 1.0f) return;

        _animator.SetTrigger(_openTrigger);
        // _animator.speed = 1f;
    }

    public void Close()
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Open") && stateInfo.normalizedTime < 1.0f) return;

        _animator.SetTrigger(_closeTrigger);
        // _animator.speed = 1f;
    }
}
