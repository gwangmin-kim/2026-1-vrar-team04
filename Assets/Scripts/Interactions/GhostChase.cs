using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class GhostChase : MonoBehaviour, ILightable
{
    private static readonly int _runHash = Animator.StringToHash("Run");
    private static readonly int _screamHash = Animator.StringToHash("Scream");
    private static readonly int _chaseHash = Animator.StringToHash("Chase");
    private static readonly int _attackStartHash = Animator.StringToHash("AttackStart");
    private static readonly int _attackHash = Animator.StringToHash("Attack");

    private static readonly int _dissolveAmountID = Shader.PropertyToID("_Dissolve");
    private static readonly int _baseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor"); // 파티클 투명도 제어용

    private Renderer _dissolveRenderer;
    private Renderer _particleRenderer;
    private MaterialPropertyBlock _propBlock;

    [Header("Chase Setting")]
    [SerializeField] private float _screamDelay; // 트리거 발동 시 모션으로 인한 딜레이 시간
    [SerializeField] private Transform _waypointRoot; // 해당 오브젝트의 자식으로 경로 설정
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _turnaroundSmoothTime;
    [SerializeField] private float _thresholdDistance = 1.5f; // 붙잡히는 판정이 되는 임계 거리
    public UnityEvent onChaseEvent; // 추격 시작 시 발생하는 이벤트
    private Vector3 _targetDirection = Vector3.zero;
    private Vector3 _currentVelocity = Vector3.zero;
    private Transform _player;

    [Header("Death Cutscene")]
    [SerializeField] private DeathCutsceneManager _deathCutsceneManager;

    [Header("Settings")]
    [SerializeField] private Transform _modelRoot; // 모델의 루트 트랜스폼 회전이 틀어지는 걸 초기화
    [SerializeField] private Transform _baseTransform;
    [SerializeField] private float _chaseSpeed;
    [SerializeField] private float _runSpeed;
    [SerializeField] private Collider _collider;
    [SerializeField] private float _timeToTrigger; // 없애기 위해 비춰야 하는 시간

    [SerializeField] private bool _isChasing = false;
    [SerializeField] private int _currentWaypointIndex = 0;
    private float _accumulatedTime = 0f;

    [Header("Texture")]
    [SerializeField] private Transform _mesh;
    [SerializeField] private Texture2D _baseMap;

    [Header("Animation")]
    [SerializeField] private Animator _animator;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem _smokeParticle;

    // 추적 경로의 웨이포인트들을 자동으로 할당
    private void OnValidate()
    {
        if (_waypointRoot == null) return;

        int childCount = _waypointRoot.childCount;
        _waypoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _waypoints[i] = _waypointRoot.GetChild(i);
        }
    }

    private void Awake()
    {
        _dissolveRenderer = _mesh.GetComponent<Renderer>();
        _particleRenderer = _smokeParticle.GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        _dissolveRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(_baseMapID, _baseMap);
        _dissolveRenderer.SetPropertyBlock(_propBlock);
    }

    private void OnEnable()
    {
        transform.SetPositionAndRotation(_baseTransform.position, _baseTransform.rotation);
        _isChasing = false;
        _collider.enabled = true;
        _currentWaypointIndex = 0;
        _accumulatedTime = 0f;
        _modelRoot.localRotation = Quaternion.identity;

        _animator.Play("Idle", 0, 0f);
        _animator.Update(0f);

        _animator.ResetTrigger(_chaseHash);
        _animator.ResetTrigger(_attackStartHash);
        _animator.ResetTrigger(_attackHash);
    }

    private void Update()
    {
        if (_isChasing)
        {
            // Vector3 targetDirection = _target.position - transform.position;
            // targetDirection.y = 0f;
            // targetDirection.Normalize();

            // transform.rotation = Quaternion.LookRotation(targetDirection);
            // transform.position += _chaseSpeed * Time.deltaTime * targetDirection;

            if (_waypoints == null || _waypoints.Length == 0 || _currentWaypointIndex >= _waypoints.Length) return;

            Transform currentTarget = _waypoints[_currentWaypointIndex];

            Vector3 deltaPosition = currentTarget.position - transform.position;
            deltaPosition.y = 0f;
            _targetDirection = deltaPosition.normalized;
            Vector3 direction = Vector3.SmoothDamp(transform.forward, _targetDirection, ref _currentVelocity, _turnaroundSmoothTime);

            Vector3 newPosition = transform.position + _chaseSpeed * Time.deltaTime * direction;
            Quaternion newRotation = Quaternion.LookRotation(direction);
            transform.SetPositionAndRotation(newPosition, newRotation);

            if (Vector3.SqrMagnitude(deltaPosition) < 0.01f)
            {
                _currentWaypointIndex++;

                if (_currentWaypointIndex >= _waypoints.Length)
                {
                    _isChasing = false;
                    return;
                }
            }

            Vector3 playerDelta = _player.position - transform.position;

            if (Vector3.SqrMagnitude(playerDelta) < _thresholdDistance * _thresholdDistance)
            {
                _isChasing = false;
                _deathCutsceneManager.StartDeathCutscene();
                _animator.SetTrigger(_attackStartHash);
            }
        }
    }

    public void SetTargetIndex(int index)
    {
        _currentWaypointIndex = index;
    }

    private void TriggerChase()
    {
        _collider.enabled = false; // 콜라이더 해제(더 이상 감지되지 않도록)
        _player = GameManager.Instance.player;
        _currentWaypointIndex = 0;
        _animator.SetTrigger(_screamHash);
        onChaseEvent?.Invoke();

        StartCoroutine(ReserveChase());
    }

    public void StartRun()
    {
        _animator.SetTrigger(_runHash);
        _chaseSpeed = _runSpeed;
    }

    private IEnumerator ReserveChase()
    {
        yield return new WaitForSeconds(_screamDelay);

        _animator.SetTrigger(_chaseHash);
        _isChasing = true;
    }

    public void Light(float deltaTime)
    {
        _accumulatedTime += deltaTime;

        if (_accumulatedTime >= _timeToTrigger)
        {
            TriggerChase();
        }
    }

    // 경로 시각화
    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < _waypoints.Length - 1; i++)
        {
            if (_waypoints[i] != null && _waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                Gizmos.DrawSphere(_waypoints[i].position, 0.2f);
            }
        }
        if (_waypoints[_waypoints.Length - 1] != null)
            Gizmos.DrawSphere(_waypoints[_waypoints.Length - 1].position, 0.2f);
    }
}
