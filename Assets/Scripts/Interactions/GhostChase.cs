using UnityEngine;

public class GhostChase : MonoBehaviour, ILightable
{
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
    [SerializeField] private Transform _waypointRoot; // 해당 오브젝트의 자식으로 경로 설정
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _turnaroundSmoothTime = 0.5f;
    [SerializeField] private float _thresholdDistance = 1.5f; // 붙잡히는 판정이 되는 임계 거리
    private Vector3 _targetDirection = Vector3.zero;
    private Vector3 _currentVelocity = Vector3.zero;
    private Transform _player;

    [Header("Death Cutscene")]
    [SerializeField] private DeathCutsceneManager _deathCutsceneManager;

    [Header("Settings")]
    // [SerializeField] private Transform _target;
    [SerializeField] private Transform _baseTransform;
    [SerializeField] private float _chaseSpeed;
    [SerializeField] private Collider _collider;
    [SerializeField] private float _timeToTrigger; // 없애기 위해 비춰야 하는 시간

    private bool _isChasing = false;
    private int _currentWaypointIndex = 0;
    private Transform _currentTarget;
    private float _accumulatedTime = 0f;

    [Header("Texture")]
    [SerializeField] private Transform _mesh;
    [SerializeField] private Texture2D _baseMap;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _chaseTrigger = "Chase";

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
        _currentWaypointIndex = 0;
        _currentTarget = null;
        _accumulatedTime = 0f;

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

            Vector3 deltaPosition = _currentTarget.position - transform.position;
            deltaPosition.y = 0f;
            _targetDirection = deltaPosition.normalized;
            Vector3 direction = Vector3.SmoothDamp(transform.forward, _targetDirection, ref _currentVelocity, _turnaroundSmoothTime);

            transform.rotation = Quaternion.LookRotation(direction);
            transform.position += _chaseSpeed * Time.deltaTime * direction;

            if (Vector3.SqrMagnitude(deltaPosition) < 0.01f)
            {
                _currentWaypointIndex++;
                _currentTarget = _waypoints[_currentWaypointIndex];
            }

            Vector3 playerDelta = _player.position - transform.position;

            if (Vector3.SqrMagnitude(playerDelta) < _thresholdDistance * _thresholdDistance)
            {
                _isChasing = false;
                _deathCutsceneManager.StartDeathCutscene();
            }
        }
    }

    private void TriggerChase()
    {
        // 콜라이더 해제(더 이상 감지되지 않도록)
        _collider.enabled = false;
        _player = GameManager.Instance.player;
        _currentWaypointIndex = 0;
        _currentTarget = _waypoints[0];
        _isChasing = true;
        _animator.SetTrigger(_chaseTrigger);
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
