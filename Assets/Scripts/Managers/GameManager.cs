using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // 싱글톤 오브젝트
    public static GameManager Instance { get; private set; }

    private static bool _hasQueuedEntrancePose;
    private static Vector3 _queuedEntranceLocalPosition;
    private static Quaternion _queuedEntranceLocalRotation;
    private static Quaternion _queuedEntranceLocalCameraRotation;

    [Header("Player")]
    public Transform player;
    [SerializeField] private VRARTeam04.Player.PlayerControlLock _playerControlLock;
    [SerializeField] private Transform _baseTransform; // 플레이어가 맨 처음 스폰되는 위치

    [Header("Stage States")]
    public int currentStage = 0;
    [SerializeField] private bool _isCleared = false;

    [Header("Stage Transition")]
    [SerializeField] private GameObject _baseCorridor; // 기본 복도 (맵 변형이 일어나는 경우 비활성화 되어있을 수 있음. 초기화 마다 활성화 상태 확인)
    [SerializeField] private Transform _stageRoot;
    [SerializeField] private GameObject[] _stages; // 8층부터 2층까지의 기믹을 위한 오브젝트들이 담겨 있음

    [SerializeField] private Elevator _elevatorIn; // 스테이지 입장용 엘리베이터
    [SerializeField] private Elevator _elevatorOut; // 스테이지 퇴장용 엘리베이터

    [SerializeField] private string _lobbySceneName = "LobbyMap";
    [SerializeField] private Transform _mop; // 대걸레 (이동 가능한 오브젝트라 매 스테이지 전환 시 위치 초기화 필요)
    [SerializeField] private Vector3 _mopInitPosition;
    [SerializeField] private Quaternion _mopInitRotation;

    [Header("VR Fade Setup")]
    [SerializeField] private MeshRenderer _fadeRenderer; // 카메라 앞 반구형 가리개의 MeshRenderer
    private Material _fadeMaterial;
    private int _baseColorPropId;

    private void OnValidate()
    {
        if (_stageRoot != null)
        {
            int childCount = _stageRoot.childCount;
            _stages = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                _stages[i] = _stageRoot.GetChild(i).gameObject;
            }
        }
        if (_mop != null)
        {
            _mopInitPosition = _mop.position;
            _mopInitRotation = _mop.rotation;
        }
        if (player != null)
        {
            _playerControlLock = player.GetComponent<VRARTeam04.Player.PlayerControlLock>();
        }
    }

    private void Awake()
    {
        // if (Instance == null)
        // {
        Instance = this;
        // DontDestroyOnLoad(gameObject);
        // }
        // else
        // {
        //     Destroy(gameObject);
        // }

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
        // 자동 할당
        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }
        if (_fadeRenderer == null)
        {
            _fadeRenderer = player.Find("Fading").GetComponent<MeshRenderer>();
        }

        if (_hasQueuedEntrancePose)
            StartCoroutine(StartGameFromQueuedEntranceRoutine());
        else
            StartGame();
        // RestartStage();
    }

    public void StartGame()
    {
        currentStage = 0;
        RestartStage();
    }

    public static void QueueInitialEntrancePose(Transform sourceElevator, Transform playerTransform)
    {
        if (sourceElevator == null || playerTransform == null)
            return;

        _queuedEntranceLocalPosition = sourceElevator.InverseTransformPoint(playerTransform.position);
        _queuedEntranceLocalRotation = Quaternion.Inverse(sourceElevator.rotation) * playerTransform.rotation;
        _queuedEntranceLocalCameraRotation = GetCameraRotationRelativeTo(sourceElevator, playerTransform);
        _hasQueuedEntrancePose = true;
    }

    private IEnumerator StartGameFromQueuedEntranceRoutine()
    {
        currentStage = 0;
        LoadStage(currentStage);

        yield return null;
        // if (player.TryGetComponent<CharacterController>(out var characterController)) characterController.enabled = false;

        TeleportPlayerFromLobbyEntranceOffset(
            _queuedEntranceLocalPosition,
            _queuedEntranceLocalRotation,
            _queuedEntranceLocalCameraRotation);
        _hasQueuedEntrancePose = false;
        InitStage();

        // if (characterController != null) characterController.enabled = true;
    }

    /// <summary>
    /// 플레이어가 입력에 따라 움직일 수 있는지를 결정
    /// </summary>
    /// <param name="enabled"></param>
    public void SetPlayerControllable(bool enabled)
    {
        if (enabled) _playerControlLock.Unlock();
        else _playerControlLock.Lock();
    }

    public void ClearStage()
    {
        if (_isCleared) return;

        _isCleared = true;
        _elevatorOut.openTrigger.enabled = true;
        // _elevatorOut.closeTrigger.enabled = true;
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
    public void GoToNextStage(Transform elevatorOut = null)
    {
        // 다음 스테이지가 없다면
        // stages의 마지막에 2층 프리팹이 할당되어 있어야 함
        if (currentStage + 1 >= _stages.Length)
        {
            _stages[currentStage].SetActive(false);
            StageSceneExitTraveler traveler = FindAnyObjectByType<StageSceneExitTraveler>();
            if (traveler != null)
            {
                Transform targetElevator = elevatorOut != null ? elevatorOut : _elevatorOut.transform;

                traveler.QueuePlayerPose(targetElevator, player);
                traveler.LoadLobbyAsGameplayFloor();
            }
            else
            {
                // 방어 코드: 혹시나 Game 씬에 LobbyModeController 배치를 깜빡했을 때를 대비한 전역 매니저 다이렉트 예외 처리
                Debug.LogWarning("[GameManager] Game 씬에 LobbyModeController 인스턴스를 찾을 수 없어 심리스 기능 없이 강제 전환합니다.");
                if (SceneLoadManager.Instance != null)
                {
                    SceneLoadManager.Instance.NextMode = LobbyModeController.LobbyMode.GameplayFloor;
                    SceneLoadManager.Instance.LoadSceneSeamless(_lobbySceneName);
                }
            }

            return;
        }

        // 다음 스테이지 로드
        LoadStage(currentStage + 1);
        TeleportPlayer(elevatorOut);
        InitStage();
    }

    private void InitStage()
    {
        _elevatorIn.Initialize();
        _elevatorOut.Initialize();

        _mop.SetPositionAndRotation(_mopInitPosition, _mopInitRotation);

        _isCleared = false;
        _elevatorOut.openTrigger.enabled = false;
    }

    /// <summary>
    /// 스테이지 로드
    /// </summary>
    /// <param name="index"></param>
    private void LoadStage(int index)
    {
        if (!_baseCorridor.activeSelf)
        {
            _baseCorridor.SetActive(true);
        }

        _stages[currentStage].SetActive(false);
        _stages[index].SetActive(true);
        currentStage = index;

        int floor = 8 - index;
        _elevatorIn.SetFloorText(floor);
        _elevatorOut.SetFloorText(floor);
    }

    /// <summary>
    /// 엘리베이터 내에서의 오프셋을 유지한 채로 퇴장용 엘리베이터에서 입장용 엘리베이터로 위치 이동
    /// </summary>
    private void TeleportPlayer(Transform elevatorOut)
    {
        if (elevatorOut == null) elevatorOut = _elevatorOut.transform;

        Vector3 positionOffset = elevatorOut.transform.InverseTransformPoint(player.position);
        Quaternion rotationOffset = Quaternion.Inverse(elevatorOut.transform.rotation) * player.rotation;
        Quaternion cameraRotationOffset = GetCameraRotationRelativeTo(elevatorOut, player);

        TeleportPlayerFromEntranceOffset(positionOffset, rotationOffset, cameraRotationOffset);
    }

    private void TeleportPlayerFromEntranceOffset(Vector3 localPosition, Quaternion localRotation, Quaternion localCameraRotation)
    {
        Vector3 newPosition = _elevatorIn.transform.TransformPoint(localPosition);
        Quaternion newRotation = _elevatorIn.transform.rotation * localRotation;

        player.SetPositionAndRotation(newPosition, newRotation);
        ApplyCameraRotationToCamera(localCameraRotation);
    }

    private void TeleportPlayerFromLobbyEntranceOffset(Vector3 localPosition, Quaternion localRotation, Quaternion localCameraRotation)
    {
        if (_baseTransform != null)
            localPosition.y = _elevatorIn.transform.InverseTransformPoint(_baseTransform.position).y;

        TeleportPlayerFromEntranceOffset(localPosition, localRotation, localCameraRotation);
    }

    private void ApplyCameraRotationToCamera(Quaternion localCameraRotation)
    {
        Transform cameraTransform = GetPlayerCameraTransform(player);
        if (cameraTransform == null)
            return;

        Quaternion targetCameraRotation = _elevatorIn.transform.rotation * localCameraRotation;
        cameraTransform.rotation = targetCameraRotation;
    }

    private static Quaternion GetCameraRotationRelativeTo(Transform reference, Transform playerTransform)
    {
        Transform cameraTransform = GetPlayerCameraTransform(playerTransform);
        Quaternion cameraRotation = cameraTransform != null ? cameraTransform.rotation : playerTransform.rotation;
        return Quaternion.Inverse(reference.rotation) * cameraRotation;
    }

    private static Transform GetPlayerCameraTransform(Transform playerTransform)
    {
        if (playerTransform == null)
            return null;

        Camera playerCamera = playerTransform.GetComponentInChildren<Camera>(true);
        return playerCamera != null ? playerCamera.transform : null;
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
