using UnityEngine;
using UnityEngine.Rendering;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance { get; private set; }

    [Header("Control Lights")]
    [SerializeField] private GameObject _corridorLightsRoot; // 모든 복도 조명을 담고 있는 루트 오브젝트
    [SerializeField] private Light[] _lightList;

    [Header("Control Light Volumes")]
    [SerializeField] private Volume _lightOnVolume;
    [SerializeField] private Volume _lightOffVolume;

    [Header("Control Material")]
    [SerializeField] private Color _emissionColor = new Color32(255, 178, 105, 255); // 켜졌을 때 에미션 색깔
    [SerializeField] private float _maxEmissionIntensity = 33.89676f; // 켜졌을 때 에미션 강도
    [SerializeField] private Material _lightMaterial; // 천장 머터리얼 (emission 제어용)

    private bool _isLightOn = true;

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
        _isLightOn = false;

        if (_corridorLightsRoot != null) _corridorLightsRoot.SetActive(false);

        if (_lightOffVolume != null) _lightOffVolume.enabled = true;
        if (_lightOnVolume != null) _lightOnVolume.enabled = false;

        UpdateEmission(false);
    }

    public void TurnOn()
    {
        _isLightOn = true;

        if (_corridorLightsRoot != null) _corridorLightsRoot.SetActive(true);

        if (_lightOffVolume != null) _lightOffVolume.enabled = false;
        if (_lightOnVolume != null) _lightOnVolume.enabled = true;

        UpdateEmission(true);
    }

    // private void ExtractInitialEmissionSettings()
    // {
    //     if (_lightMaterial == null)
    //     {
    //         Debug.LogWarning("LightManager: 형광등 머터리얼(_lampMaterial)이 할당되지 않아 에미션 값을 추출할 수 없습니다.");
    //         return;
    //     }

    //     // 셰이더의 '_EmissionColor'로부터 HDR 컬러 값을 가져옵니다.
    //     // 유니티 내부에서 HDR 강도가 적용된 컬러는 [RGB * (2^Intensity)] 형태로 저장되어 있습니다.
    //     Color rawHdrColor = _lightMaterial.GetColor(_emissionColorProperty);

    //     // RGB 채널 중 가장 큰 값을 찾아 현재의 대략적인 밝기 스케일(강도)을 계산합니다.
    //     float maxChannel = Mathf.Max(rawHdrColor.r, rawHdrColor.g, rawHdrColor.b);

    //     if (maxChannel > 1f)
    //     {
    //         // 색상 값이 1을 초과하는 HDR 상태라면, 최대 채널 값을 Intensity(강도)로 규정합니다.
    //         _maxEmissionIntensity = maxChannel;

    //         // 강도로 나누어주어 순수한 0~1 사이의 원본 LDR 컬러(색상 톤)만 추출합니다.
    //         _emissionColor = new Color(rawHdrColor.r / maxChannel, rawHdrColor.g / maxChannel, rawHdrColor.b / maxChannel, rawHdrColor.a);
    //     }
    //     else
    //     {
    //         // HDR 강도가 들어가 있지 않은 일반 컬러(최대값이 1 이하)라면 강도는 1, 컬러는 그대로 사용합니다.
    //         _maxEmissionIntensity = maxChannel > 0 ? maxChannel : 1f; // 만약 0이라면 기본값 1
    //         _emissionColor = rawHdrColor;
    //     }

    //     // Debug.Log($"[LightManager] 머터리얼 분석 완료 -> 추출된 컬러: {_emissionColor}, 추출된 강도: {_maxEmissionIntensity}");
    // }

    private void UpdateEmission(bool isOn)
    {
        if (_lightMaterial == null) return;

        if (isOn)
        {
            // 기본 설정된 에미션 컬러와 강도를 곱해 켜진 상태 적용
            Color finalColor = _emissionColor * _maxEmissionIntensity;
            _lightMaterial.SetColor(_emissionColorProperty, finalColor);
            _lightMaterial.EnableKeyword("_EMISSION"); // 에미션 기능 활성화 키워드 보장
        }
        else
        {
            // 검은색을 주어 빛을 완전히 끔
            _lightMaterial.SetColor(_emissionColorProperty, Color.black);
            _lightMaterial.DisableKeyword("_EMISSION");
        }
    }
}
