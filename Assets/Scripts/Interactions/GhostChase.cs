using UnityEngine;

public class GhostChase : MonoBehaviour, ILightable
{
    private static readonly int _dissolveAmountID = Shader.PropertyToID("_Dissolve");
    private static readonly int _baseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor"); // 파티클 투명도 제어용

    private static readonly string _chaseTrigger = "Chase";

    private Renderer _dissolveRenderer;
    private Renderer _particleRenderer;
    private MaterialPropertyBlock _propBlock;

    [Header("Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _chaseSpeed;
    [SerializeField] private Collider _collider;
    [SerializeField] private float _timeToTrigger; // 없애기 위해 비춰야 하는 시간

    private bool _isChasing = false;
    private float _accumulatedTime = 0f;

    [Header("Texture")]
    [SerializeField] private Transform _mesh;
    [SerializeField] private Texture2D _baseMap;

    [Header("Animation")]
    [SerializeField] private Animator _animator;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem _smokeParticle;

    private void Awake()
    {
        _dissolveRenderer = _mesh.GetComponent<Renderer>();
        _particleRenderer = _smokeParticle.GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        _dissolveRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(_baseMapID, _baseMap);
        _dissolveRenderer.SetPropertyBlock(_propBlock);
    }

    private void Update()
    {
        if (_isChasing)
        {
            Vector3 targetDirection = _target.position - transform.position;
            targetDirection.y = 0f;
            targetDirection.Normalize();

            transform.rotation = Quaternion.LookRotation(targetDirection);
            transform.position += _chaseSpeed * Time.deltaTime * targetDirection;
        }
    }

    private void TriggerChase()
    {
        // 콜라이더 해제(더 이상 감지되지 않도록)
        _collider.enabled = false;

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
}
