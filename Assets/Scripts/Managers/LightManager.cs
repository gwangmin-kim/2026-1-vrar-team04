using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance { get; private set; }

    [Header("Control Lights")]
    [SerializeField] private GameObject _corridorLightsRoot; // 모든 복도 조명을 담고 있는 루트 오브젝트
    [SerializeField] private Light[] _lightList; // 실제 제어할 빛 컴포넌트 리스트
    [SerializeField] private float _maxIntensity = 1f; // 밝기 최댓값

    [Header("Control Light Volumes")]
    [SerializeField] private Volume _lightOnVolume;
    [SerializeField] private Volume _lightOffVolume;

    [Header("Control Material")]
    [SerializeField] private Color _emissionColor = new Color32(255, 178, 105, 255); // 켜졌을 때 에미션 색깔
    [SerializeField] private float _maxEmissionIntensity = 33.89676f; // 켜졌을 때 에미션 강도
    [SerializeField] private Material _lightMaterial; // 천장 머터리얼 (emission 제어용)

    [Header("Flicker Settings")]
    [Tooltip("X: 최소 간격, Y: 최대 간격 (초 단위)")]
    [SerializeField] private Vector2 _flickerIntervalRange;
    [Tooltip("X: 최소 밝기, Y: 최대 밝기 (소등 중 깜빡일 비주얼 범위)")]
    [SerializeField] private Vector2 _flickerIntensityRange;
    private Coroutine _flickerCoroutine;

    // URP/HDRP의 표준 에미션 컬러 속성 이름 키워드
    private static readonly int _emissionColorProperty = Shader.PropertyToID("_EmissionColor");

    private void OnValidate()
    {
        if (_corridorLightsRoot != null)
        {
            _lightList = _corridorLightsRoot.GetComponentsInChildren<Light>();
        }
    }

    private void Awake()
    {
        Instance = this;

        TurnOn();
    }

    public void TurnOff()
    {
        foreach (var light in _lightList)
        {
            light.intensity = 0f;
        }

        if (_lightOffVolume != null) _lightOffVolume.enabled = true;
        if (_lightOnVolume != null) _lightOnVolume.enabled = false;

        UpdateEmission(0f);

        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }
        _flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    public void TurnOn()
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }

        foreach (var light in _lightList)
        {
            light.intensity = _maxIntensity;
        }

        if (_lightOffVolume != null) _lightOffVolume.enabled = false;
        if (_lightOnVolume != null) _lightOnVolume.enabled = true;

        UpdateEmission(_maxIntensity);
    }

    private void UpdateEmission(float currentLightIntensity)
    {
        if (_lightMaterial == null) return;

        // 밝기가 아주 미세하게라도 켜져 있는 상태라면
        if (currentLightIntensity > 0.001f)
        {
            // 정상 최대 밝기(_maxIntensity) 대비 현재 깜빡이는 밝기의 상대 비율 계산
            float intensityRatio = _maxIntensity > 0f ? currentLightIntensity / _maxIntensity : 0f;

            // 기존 최대 강도에 비율을 곱해 연동된 색상 생성
            Color finalColor = _emissionColor * (_maxEmissionIntensity * intensityRatio);

            _lightMaterial.SetColor(_emissionColorProperty, finalColor);
            _lightMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            // 밝기가 완전히 0에 가깝다면 에미션 기능을 완벽히 차단 (검은색)
            _lightMaterial.SetColor(_emissionColorProperty, Color.black);
            _lightMaterial.DisableKeyword("_EMISSION");
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // 설정된 Vector2 (X: 최소 수치, Y: 최대 수치) 사이의 무작위 값 추출
            float randomIntensity = Random.Range(_flickerIntensityRange.x, _flickerIntensityRange.y);
            float randomWaitTime = Random.Range(_flickerIntervalRange.x, _flickerIntervalRange.y);

            // 리스트 안의 모든 실시간 조명 컴포넌트 밝기 변경
            foreach (var light in _lightList)
            {
                if (light != null)
                {
                    light.intensity = randomIntensity;
                }
            }

            // 조명 컴포넌트의 실제 밝기에 비례하도록 머터리얼의 에미션 발광 세기도 실시간 업데이트
            UpdateEmission(randomIntensity);

            // 임의의 시간만큼 대기 후 다음 루프 실행
            yield return new WaitForSeconds(randomWaitTime);
        }
    }
}
