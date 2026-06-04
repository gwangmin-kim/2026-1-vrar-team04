using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using VRARTeam04.Player;

public class GhostLight : MonoBehaviour, ILightable
{
    private static readonly int _dissolveAmountID = Shader.PropertyToID("_Dissolve");
    private static readonly int _baseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor"); // 파티클 투명도 제어용

    private Renderer _dissolveRenderer;
    private Renderer _particleRenderer;
    private MaterialPropertyBlock _propBlock;

    [Header("Settings")]
    [SerializeField] private Collider _collider;
    [SerializeField] private float _timeToTrigger; // 없애기 위해 비춰야 하는 시간
    [SerializeField] private Collider _blockCollider; // distortion plane의 콜라이더 (완료 전까지 통과 불가능하도록 막는 역할)

    private float _accumulatedTime = 0f;
    private bool _isDisappearing;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _lightTrigger = "Light";

    [Header("Dissolve Effect")]
    [SerializeField] private Transform _mesh;
    [SerializeField] private Texture2D _baseMap;
    [SerializeField] private float _dissolveDuration;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem _smokeParticle;

    [Header("Events")]
    public UnityEvent OnEnabled;
    public UnityEvent OnLighted;

    // 올바르게 대처 완료 시 호출할 함수
    private void OnComplete()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.ClearStage();
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
        OnEnabled?.Invoke();
    }

    private void Disappear()
    {
        if (_isDisappearing) return;
        _isDisappearing = true;

        // 콜라이더 해제(더 이상 감지되지 않도록)
        _collider.enabled = false;
        _animator.SetTrigger(_lightTrigger);
        OnLighted?.Invoke();

        // 완료 로직 호출 (게임 매니저에게 알림)
        OnComplete();

        // 새로운 파티클 생성 중지
        if (_smokeParticle.isPlaying) _smokeParticle.Stop();

        DOTween.To(() => 0f, x =>
        {
            // 디졸브 효과
            _dissolveRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_dissolveAmountID, x);
            _dissolveRenderer.SetPropertyBlock(_propBlock);

            // 기존 파티클 투명도 제어
            _particleRenderer.GetPropertyBlock(_propBlock);
            Color particleColor = Color.black;
            particleColor.a = 1f - x;
            _propBlock.SetColor(_baseColorID, particleColor);
            _particleRenderer.SetPropertyBlock(_propBlock);
        }, 1f, _dissolveDuration)
        .SetEase(Ease.InQuad)
        .OnComplete(() =>
        {
            // 연출 종료 후 GO 비활성화
            gameObject.SetActive(false);
            _blockCollider.enabled = false;
        });
    }

    public void Light(float deltaTime)
    {
        _accumulatedTime += deltaTime;

        if (_accumulatedTime >= _timeToTrigger)
        {
            Disappear();
        }
    }
}
