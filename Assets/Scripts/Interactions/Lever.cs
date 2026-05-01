using UnityEngine;

public class Lever : MonoBehaviour, IGrabbable
{
    [Header("Lever Parts")]
    [SerializeField] private Transform _lever; // 움직이는 부분
    [SerializeField] private Transform _knob; // 컨트롤러가 달려있을 지점

    private Vector3 _initialLookDirection; // 처음 레버의 정렬 방향을 나타내는 벡터
    private Vector3 _initialForward; // 레버가 돌아갈 방향을 결정하는 벡터 (반대 방향으로 돌아가지 못하도록 하기 위함)

    [Header("Grab Controller")]
    [SerializeField] private Transform _controller;
    [SerializeField] private bool _isGrabbed = false;
    [SerializeField] private float _maxDistance; // 잡고 움직일 수 있는 최대 거리, 너무 멀어지면 놓침
    [SerializeField] private float _smoothTime; // 부드럽게 움직이는 시간
    [SerializeField] private float _thresholdAngle; // 켜지는 판정이 되는 각도
    [SerializeField] private bool _isPowered = false; // 한 번 켜지면 상호작용 불가능

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
        _initialLookDirection = _lever.up;
        _initialForward = _lever.forward;
    }

    private void Update()
    {
        if (_isGrabbed && !_isPowered)
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
        _lever.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);

        if (_currentAngle >= _thresholdAngle)
        {
            _targetAngle = 180f;
            _isPowered = true;
            Release();

            OnComplete();
        }
    }

    private void UpdateTargetAngle()
    {
        Vector3 targetDirection = _controller.position - _lever.position;

        _targetAngle = (Vector3.Dot(_initialForward, targetDirection) <= 0f) ? 0f :
                    Vector3.Angle(_initialLookDirection, targetDirection);
    }

    private void CheckGrabCondition()
    {
        Vector3 deltaPosition = _controller.position - _knob.position;

        if (deltaPosition.sqrMagnitude > _maxDistance * _maxDistance)
        {
            Release();
        }
    }


    public void Grab()
    {
        if (_isGrabbed || _isPowered) return;
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
