using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VRARTeam04.Player;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("Horror/Gameplay Floor Ending Trigger (VR&AR Team 04)")]
public sealed class GameplayFloorEndingTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private LayerMask _playerLayers = 1 << 3;
    [SerializeField] private bool _triggerOnlyOnce = true;

    [Header("Player")]
    [SerializeField] private PlayerControlLock _playerControlLock;
    [SerializeField] private bool _lockPlayerOnTrigger = true;

    [Header("Light Blast")]
    [SerializeField] private bool _turnOnLightManager = true;
    [SerializeField] private GameObject[] _lightBlastRoots;
    [SerializeField] private Light[] _blastLights;
    [SerializeField] private float _targetIntensity = 120f;
    [SerializeField] private float _targetRange = 35f;
    [SerializeField] private float _blastRampDuration = 3f;

    [Header("White Overlay")]
    [SerializeField] private CanvasGroup _whiteOverlay;
    [SerializeField] private MeshRenderer _whiteFadeRenderer;
    [SerializeField] private float _targetOverlayAlpha = 1f;
    [SerializeField] private float _whiteFadeBrightness = 2.5f;
    [SerializeField] private float _whiteFadeRampPower = 1f;

    [Header("End")]
    [SerializeField] private float _endDelay = 1.25f;
    [Tooltip("켜면 페이드 후 바로 종료하지 않고 엔딩 UI(재시작/종료)를 띄운다.")]
    [SerializeField] private bool _showEndingUi = true;
    [SerializeField] private EndingUIController _endingUiController;
    [Tooltip("엔딩 UI 를 띄울 때 흰 화면/블라스트를 부드럽게 되돌려 UI 가 드러나게 한다.")]
    [SerializeField] private bool _fadeOutOnEndingUi = true;
    [SerializeField] private float _fadeOutDuration = 1.5f;
    [Tooltip("엔딩 UI 를 띄우지 않을 때(_showEndingUi=false)만 사용. 페이드 후 애플리케이션을 종료한다.")]
    [SerializeField] private bool _quitApplication = true;

    [Header("Audio")]
    [SerializeField] private OneShotPlayer _endingSound;

    [Header("Events")]
    public UnityEvent OnEndingStarted;
    public UnityEvent OnBeforeEndGame;

    private float[] _initialIntensities;
    private float[] _initialRanges;
    private Material _whiteFadeMaterial;
    private int _whiteFadeColorProperty;
    private int _whiteFadeEmissionProperty;
    private bool _whiteFadeHasEmission;
    private bool _triggered;

    private void Reset()
    {
        var triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        var triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        CacheLightValues();
        SetRootsActive(_lightBlastRoots, false);

        if (_whiteOverlay != null)
            _whiteOverlay.alpha = 0f;

        SetupWhiteFadeRenderer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggerOnlyOnce && _triggered)
            return;

        if (!IsPlayerCollider(other))
            return;

        _triggered = true;
        StartCoroutine(EndingSequence(other));
    }

    private IEnumerator EndingSequence(Collider playerCollider)
    {
        if (_playerControlLock == null)
            _playerControlLock = playerCollider.GetComponentInParent<PlayerControlLock>();

        if (_lockPlayerOnTrigger && _playerControlLock != null)
            _playerControlLock.Lock();

        if (_turnOnLightManager && LightManager.Instance != null)
            LightManager.Instance.TurnOn();

        SetRootsActive(_lightBlastRoots, true);
        _endingSound?.Play();
        OnEndingStarted?.Invoke();

        yield return RampLightBlast();

        if (_endDelay > 0f)
            yield return new WaitForSeconds(_endDelay);

        OnBeforeEndGame?.Invoke();

        if (_showEndingUi && _endingUiController != null)
        {
            _endingUiController.Show();

            if (_fadeOutOnEndingUi)
                yield return FadeOutBlast();
        }
        else
        {
            EndGame();
        }
    }

    private IEnumerator FadeOutBlast()
    {
        float duration = Mathf.Max(0.001f, _fadeOutDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - SmoothStep(Mathf.Clamp01(elapsed / duration));
            ApplyLightBlast(t);
            yield return null;
        }

        ApplyLightBlast(0f);
    }

    private IEnumerator RampLightBlast()
    {
        float duration = Mathf.Max(0.001f, _blastRampDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(Mathf.Clamp01(elapsed / duration));
            ApplyLightBlast(t);
            yield return null;
        }

        ApplyLightBlast(1f);
    }

    private void ApplyLightBlast(float t)
    {
        if (_blastLights != null)
        {
            for (int i = 0; i < _blastLights.Length; i++)
            {
                var blastLight = _blastLights[i];
                if (blastLight == null)
                    continue;

                float startIntensity = GetCachedValue(_initialIntensities, i, blastLight.intensity);
                float startRange = GetCachedValue(_initialRanges, i, blastLight.range);
                blastLight.enabled = true;
                blastLight.intensity = Mathf.Lerp(startIntensity, _targetIntensity, t);
                blastLight.range = Mathf.Lerp(startRange, _targetRange, t);
            }
        }

        if (_whiteOverlay != null)
            _whiteOverlay.alpha = Mathf.Lerp(0f, _targetOverlayAlpha, t);

        if (_whiteFadeMaterial != null)
        {
            float flashT = Mathf.Pow(Mathf.Clamp01(t), Mathf.Max(0.01f, _whiteFadeRampPower));
            Color color = Color.white * Mathf.Lerp(1f, _whiteFadeBrightness, flashT);
            color.a = Mathf.Lerp(0f, _targetOverlayAlpha, flashT);
            _whiteFadeMaterial.SetColor(_whiteFadeColorProperty, color);

            if (_whiteFadeHasEmission)
            {
                _whiteFadeMaterial.EnableKeyword("_EMISSION");
                _whiteFadeMaterial.SetColor(_whiteFadeEmissionProperty, Color.white * _whiteFadeBrightness * flashT);
            }

            if (_whiteFadeRenderer != null)
                _whiteFadeRenderer.gameObject.SetActive(color.a > 0f);
        }
    }

    private void CacheLightValues()
    {
        if (_blastLights == null)
            return;

        _initialIntensities = new float[_blastLights.Length];
        _initialRanges = new float[_blastLights.Length];

        for (int i = 0; i < _blastLights.Length; i++)
        {
            var blastLight = _blastLights[i];
            if (blastLight == null)
                continue;

            _initialIntensities[i] = blastLight.intensity;
            _initialRanges[i] = blastLight.range;
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        for (Transform current = other.transform; current != null; current = current.parent)
        {
            if ((_playerLayers.value & (1 << current.gameObject.layer)) != 0)
                return true;
        }

        return false;
    }

    private void SetupWhiteFadeRenderer()
    {
        if (_whiteFadeRenderer == null && GameManager.Instance != null && GameManager.Instance.player != null)
        {
            Transform fade = GameManager.Instance.player.Find("Fading");
            if (fade != null)
                _whiteFadeRenderer = fade.GetComponent<MeshRenderer>();
        }

        if (_whiteFadeRenderer == null)
            return;

        _whiteFadeMaterial = _whiteFadeRenderer.material;
        _whiteFadeColorProperty = _whiteFadeMaterial.HasProperty("_BaseColor")
            ? Shader.PropertyToID("_BaseColor")
            : Shader.PropertyToID("_Color");
        _whiteFadeEmissionProperty = Shader.PropertyToID("_EmissionColor");
        _whiteFadeHasEmission = _whiteFadeMaterial.HasProperty(_whiteFadeEmissionProperty);

        Color color = Color.white;
        color.a = 0f;
        _whiteFadeMaterial.SetColor(_whiteFadeColorProperty, color);
        if (_whiteFadeHasEmission)
            _whiteFadeMaterial.SetColor(_whiteFadeEmissionProperty, Color.black);

        _whiteFadeRenderer.gameObject.SetActive(false);
    }

    private void EndGame()
    {
        if (!_quitApplication)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static float GetCachedValue(float[] values, int index, float fallback)
    {
        if (values == null || index < 0 || index >= values.Length)
            return fallback;

        return values[index];
    }

    private static void SetRootsActive(GameObject[] roots, bool active)
    {
        if (roots == null)
            return;

        foreach (var root in roots)
        {
            if (root != null)
                root.SetActive(active);
        }
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
