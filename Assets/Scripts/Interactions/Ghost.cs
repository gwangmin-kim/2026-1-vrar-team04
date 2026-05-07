using DG.Tweening;
using UnityEngine;

public class Ghost : MonoBehaviour, ILightable
{
    private static readonly int _dissolveAmountID = Shader.PropertyToID("_Dissolve");
    private static readonly int _baseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor"); // 파티클 투명도 제어용

    private Renderer _dissolveRenderer;
    private Renderer _particleRenderer;
    private MaterialPropertyBlock _propBlock;

    [Header("Settings")]
    [SerializeField] private Collider _collider;
    [SerializeField] private float _disappearTime; // 없애기 위해 비춰야 하는 시간
    private float _accumulatedTime = 0f;

    [Header("Dissolve Effect")]
    [SerializeField] private Transform _mesh;
    [SerializeField] private Texture2D _baseMap;
    [SerializeField] private float _dissolveDuration;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem _smokeParticle;

    // 올바르게 대처 완료 시 호출할 함수
    private void OnComplete()
    {

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

    private void Disappear()
    {
        // 콜라이더 해제(더 이상 감지되지 않도록)
        _collider.enabled = false;

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
        });
    }

    public void Light(float deltaTime)
    {
        _accumulatedTime += deltaTime;

        if (_accumulatedTime >= _disappearTime)
        {
            Disappear();
        }
    }
}
