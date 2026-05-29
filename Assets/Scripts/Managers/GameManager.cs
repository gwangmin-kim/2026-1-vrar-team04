using UnityEngine;
using System;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // 싱글톤 오브젝트
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    public Transform player;
    [SerializeField] private Transform _baseTransform; // 플레이어가 맨 처음 스폰되는 위치

    [Header("Stage States")]
    public int currentStage = 0;
    [SerializeField] private bool _isCleared = false;

    [Header("Stage Transition")]
    [SerializeField] private Transform _stageRoot;
    [SerializeField] private GameObject[] _stages; // 8층부터 2층까지의 기믹을 위한 오브젝트들이 담겨 있음
    [SerializeField] private Elevator _elevatorIn; // 스테이지 입장용 엘리베이터
    [SerializeField] private Elevator _elevatorOut; // 스테이지 퇴장용 엘리베이터

    [Header("VR Fade Setup")]
    [SerializeField] private MeshRenderer _fadeRenderer; // 카메라 앞 반구형 가리개의 MeshRenderer
    private Material _fadeMaterial;
    private int _baseColorPropId;

    private void OnValidate()
    {
        int childCount = _stageRoot.childCount;
        _stages = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _stages[i] = _stageRoot.GetChild(i).gameObject;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        foreach (var stage in _stages)
        {
            stage.SetActive(false);
        }

        if (_fadeRenderer != null)
        {
            _fadeMaterial = _fadeRenderer.material;
            // URP 기본 셰이더의 컬러 및 알파 제어용 프로퍼티 ID
            _baseColorPropId = Shader.PropertyToID("_BaseColor");
        }
    }

    private void Start()
    {
        // StartGame();
        RestartStage();
    }

    public void StartGame()
    {
        currentStage = 0;
        RestartStage();
    }

    /// <summary>
    /// 플레이어가 입력에 따라 움직일 수 있는지를 결정
    /// </summary>
    /// <param name="enabled"></param>
    public void SetPlayerControllable(bool enabled)
    {

    }

    public void ClearStage()
    {
        if (_isCleared) return;

        _isCleared = true;
        _elevatorOut.openTrigger.enabled = true;
        _elevatorOut.closeTrigger.enabled = true;
    }

    /// <summary>
    /// 스테이지 클리어에 실패했을 때 해당 스테이지를 완전히 초기화
    /// </summary>
    public void RestartStage()
    {
        LoadStage(currentStage);
        player.SetPositionAndRotation(_baseTransform.position, _baseTransform.rotation);
        InitStage();
    }

    /// <summary>
    /// 스테이지 클리어 후 다음 스테이지 초기화
    /// </summary>
    public void GoToNextStage()
    {
        currentStage++;
        if (currentStage >= _stages.Length)
        {
            // 1층으로 이동
        }
        else
        {
            // 다음 스테이지 로드
            LoadStage(currentStage);
            TeleportPlayer();
            InitStage();
        }
    }

    private void InitStage()
    {
        _elevatorIn.Initialize();
        _elevatorOut.Initialize();

        _isCleared = false;
        _elevatorOut.openTrigger.enabled = false;
    }

    /// <summary>
    /// 스테이지 로드
    /// </summary>
    /// <param name="index"></param>
    private void LoadStage(int index)
    {
        _stages[currentStage].SetActive(false);
        _stages[index].SetActive(true);
        currentStage = index;
    }

    /// <summary>
    /// 엘리베이터 내에서의 오프셋을 유지한 채로 퇴장용 엘리베이터에서 입장용 엘리베이터로 위치 이동
    /// </summary>
    private void TeleportPlayer()
    {
        Vector3 positionOffset = _elevatorOut.transform.InverseTransformPoint(player.position);
        Vector3 newPosition = _elevatorIn.transform.TransformPoint(positionOffset);
        Quaternion newRotation = _elevatorIn.transform.rotation
                                * Quaternion.Inverse(_elevatorOut.transform.rotation)
                                * player.rotation;

        player.SetPositionAndRotation(newPosition, newRotation);
    }

    /// <summary>
    /// 화면을 검게 암전시키는 함수 (Fade Out)
    /// </summary>
    /// <param name="duration">걸리는 시간</param>
    /// <param name="onComplete">완료 후 실행할 함수</param>
    public void FadeOut(float duration, Action onComplete = null)
    {
        if (_fadeMaterial == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 혹시 비활성화되어 있다면 켜기
        _fadeRenderer.gameObject.SetActive(true);

        // 현재 머티리얼 컬러 가져오기
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // DOTween으로 알파값만 1f(검은색 100%)로 보간
        DOTween.To(() => currentColor.a, x =>
        {
            currentColor.a = x;
            _fadeMaterial.SetColor(_baseColorPropId, currentColor);
        }, 1f, duration)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            // 완료 시 수행할 동작 실행
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 투명해지면서 화면을 다시 보여주는 함수 (Fade In)
    /// </summary>
    /// <param name="duration">걸리는 시간</param>
    /// <param name="onComplete">완료 후 실행할 함수</param>
    public void FadeIn(float duration, Action onComplete = null)
    {
        if (_fadeMaterial == null)
        {
            onComplete?.Invoke();
            return;
        }

        _fadeRenderer.gameObject.SetActive(true);
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // DOTween으로 알파값만 0f(완전 투명)로 보간
        DOTween.To(() => currentColor.a, x =>
        {
            currentColor.a = x;
            _fadeMaterial.SetColor(_baseColorPropId, currentColor);
        }, 0f, duration)
        .SetEase(Ease.InQuad)
        .OnComplete(() =>
        {
            // 완전 투명해지면 드로우콜 낭비를 방지하기 위해 가리개 끄기
            _fadeRenderer.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
}
