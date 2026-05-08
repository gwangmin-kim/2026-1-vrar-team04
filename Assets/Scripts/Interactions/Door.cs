using DG.Tweening;
using UnityEngine;

public class Door : MonoBehaviour, IGrabbable
{
    [Header("Door Parts")]
    [SerializeField] private Transform _door; // 움직일 실제 문 부품
    [SerializeField] private Transform _knob; // 컨트롤러가 달려있을 지점

    private Vector3 _initialLookDirection; // 처음 문의 정렬 방향을 나타내는 벡터 (그랩 시 돌아갈 각도를 계산하기 위한 기준 방향)
    private Vector3 _initialForward; // 문이 열리는 방향을 결정하는 벡터 (문이 한쪽으로만 열리고, 반대편으로 돌아가지 않도록 하기 위함)

    [Header("Trigger Event")]
    // 플레이어가 복도의 특정 구간을 넘어가면(트리거에 닿으면) 문이 스르륵 열리는 방식
    [SerializeField] private float _openAngle; // 이벤트 발동 시 열리는 각도
    [SerializeField] private float _openTime; // 문이 열리는 시간
    [SerializeField] private bool _isEventTriggered = false;

    [Header("Grab Controller")]
    // 플레이어가 잡고 움직이는 부분, 인게임에선 보이지 않으며, 문은 잡혀있는 상태에서 해당 트랜스폼의 위치로 유도되며 열리고 닫힘
    [SerializeField] private Transform _controller;
    [SerializeField] private bool _isGrabbed = false;
    [SerializeField] private float _maxAngle; // 열리는 최대 각도
    [SerializeField] private float _maxDistance; // 잡고 움직일 수 있는 최대 거리, 너무 멀어지면 놓침
    [SerializeField] private float _smoothTime; // 부드럽게 움직이는 시간
    [SerializeField] private float _closeAngle; // 이 각도 미만으로 떨어지면 닫히는 판정
    [SerializeField] private bool _isClosed = false; // 한 번 닫히면 다시 상호작용 불가능

    // 각도는 항상 양수값으로 설정
    // 실제 회전을 반영할 때 방향을 고려해서 부호 결정
    private float _currentAngle = 0f;
    private float _targetAngle = 0f;
    private float _rotVelocity = 0f;

    private float _checkInterval = 0.1f; // 매프레임 계산하지 않고 이 간격마다 타겟 각도 재설정
    private float _checkTimer = 0f;

    // 올바르게 대처 완료 시 호출할 함수
    private void OnComplete()
    {

    }

    private void Awake()
    {
        // 실사용할 에셋의 트랜스폼을 고려해서 방향을 설정해야 함
        _initialLookDirection = -_door.right;
        _initialForward = -_door.forward;
    }

    private void Update()
    {
        if (!_isEventTriggered) return;

        if (_isGrabbed && !_isClosed)
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0f)
            {
                UpdateTargetAngle();
                CheckGrabCondition();

                _checkTimer = _checkInterval;
            }
        }

        _currentAngle = Mathf.SmoothDampAngle(_currentAngle, _targetAngle, ref _rotVelocity, _smoothTime);
        _door.localRotation = Quaternion.Euler(0f, -_currentAngle, 0f);

        if (_currentAngle < _closeAngle)
        {
            _targetAngle = 0f;
            _isClosed = true;
            Release();

            OnComplete();
        }
    }

    private void UpdateTargetAngle()
    {
        Vector3 targetDirection = _controller.position - _door.position;
        targetDirection.y = 0;
        targetDirection.Normalize();

        _targetAngle = (Vector3.Dot(_initialForward, targetDirection) <= 0f) ? 0f :
            Mathf.Min(Vector3.Angle(_initialLookDirection, targetDirection), _maxAngle);
    }

    private void CheckGrabCondition()
    {
        Vector3 deltaPosition = _controller.position - _knob.position;

        if (deltaPosition.sqrMagnitude > _maxDistance * _maxDistance)
        {
            Release();
        }
    }

    public void Open()
    {
        if (_isEventTriggered) return;

        Vector3 to = _openAngle * Vector3.down;
        _door.DORotate(to, _openTime).SetEase(Ease.InOutSine).OnComplete(OpenAnimationCallback);
    }

    private void OpenAnimationCallback()
    {
        _isEventTriggered = true;
        _currentAngle = _openAngle;
        _targetAngle = _openAngle;
    }

    public void Grab()
    {
        if (_isGrabbed || _isClosed) return;
        _isGrabbed = true;
        _controller.SetParent(null);
    }

    public void Release()
    {
        _isGrabbed = false;
        _controller.SetParent(_knob);
        _controller.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_knob.position, _maxDistance);
    }
#endif
}
