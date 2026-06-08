using UnityEngine;
using UnityEngine.Events;
using VRARTeam04.Player;

/// <summary>
/// 엔딩 시퀀스(화이트 페이드)가 끝난 뒤 플레이어 눈앞에 엔딩 UI를 띄우고,
/// 재시작/종료 입력을 처리한다.
///
/// 게임플레이 플로어 모드에서는 (1) XRUIInteractionToggle 이 UI 입력을 꺼두고,
/// (2) 엔딩 트리거가 PlayerControlLock 으로 레이 인터랙터를 비활성화하기 때문에
/// UI 를 띄워도 클릭할 수 없다. Show() 가 이 둘을 다시 켜서 UI 를 사용 가능하게 만든다.
/// </summary>
[AddComponentMenu("Horror/Ending UI Controller (VR&AR Team 04)")]
public sealed class EndingUIController : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("엔딩 UI 월드 스페이스 캔버스 루트. 평소에는 비활성 상태로 둔다.")]
    [SerializeField] private GameObject _endingUiRoot;

    [Header("Placement")]
    [Tooltip("표시할 때 플레이어 카메라 정면으로 UI 를 재배치할지 여부. 끄면 캔버스가 에디터에 배치된 위치를 그대로 사용한다.")]
    [SerializeField] private bool _faceCameraOnShow = true;
    [SerializeField] private Transform _playerCamera;
    [SerializeField] private float _distanceFromCamera = 1.5f;
    [SerializeField] private float _verticalOffset = 0f;

    [Header("Player Input Restore")]
    [Tooltip("UI 를 띄울 때 레이 인터랙터를 다시 켜기 위해 플레이어 잠금을 해제한다.")]
    [SerializeField] private PlayerControlLock _playerControlLock;
    [SerializeField] private XRUIInteractionToggle _uiInteractionToggle;
    [Tooltip("보통 비워둔다. 이동은 PlayerControlLock 이 막는다. PlayerControlLock 이 못 잡는 별도 이동 스크립트가 있을 때만 추가로 지정.")]
    [SerializeField] private Behaviour[] _movementToDisable;

    [Header("Event System (XR UI Input)")]
    [Tooltip("게임플레이 모드의 EventSystem (InputSystemUIInputModule). XR 레이로 UI 클릭이 안 되므로 UI 표시 중 끈다.")]
    [SerializeField] private GameObject _gameModeEventSystem;
    [Tooltip("XR UI Input Module 이 달린 EventSystem (메뉴 모드 것과 동일 설정). UI 표시 중 켠다. 평소엔 비활성.")]
    [SerializeField] private GameObject _xrUiEventSystem;

    [Header("Visuals")]
    [Tooltip("UI 를 띄울 때 즉시 끌 오브젝트. 화이트 페이드 메시는 트리거의 페이드아웃이 처리하므로 여기 넣지 말 것. (그 외 가림 요소가 있을 때만 사용)")]
    [SerializeField] private GameObject[] _hideOnShow;

    [Header("Restart / Quit")]
    [SerializeField] private string _lobbySceneName = "LobbyMap";

    // [Header("Audio")]
    // [SerializeField] private OneShotPlayer _selectSound;

    [Header("Fade Effect")]
    [SerializeField] private LobbyFadeInOut _fadeInOut;
    [SerializeField] private float _fadeDuration = 1f;

    [Header("Events")]
    public UnityEvent OnEndingUiShown;
    public UnityEvent OnBeforeRestart;
    public UnityEvent OnBeforeQuit;

    public bool IsShown { get; private set; }

    private void Awake()
    {
        if (_endingUiRoot != null)
            _endingUiRoot.SetActive(false);
    }

    /// <summary>
    /// 엔딩 UI 를 띄우고, 플레이어가 UI 를 클릭할 수 있도록 입력을 복구한다.
    /// 엔딩 트리거 시퀀스 마지막에서 호출한다.
    /// </summary>
    public void Show()
    {
        if (IsShown)
            return;

        IsShown = true;

        // 1) 레이 인터랙터가 다시 동작하도록 플레이어 잠금 해제.
        //    단, 이동(로코모션)은 계속 잠가 둔다 — 엔딩 UI 중엔 걷지 못하되 UI 만 클릭.
        if (_playerControlLock != null)
            _playerControlLock.UnlockKeepingMovementLocked();

        // 2) EventSystem 을 XR UI Input Module 쪽으로 교체한다.
        //    게임플레이 모드 EventSystem 은 InputSystemUIInputModule 이라 XR 레이로 UI 클릭이 안 된다.
        //    (EventSystem 은 동시에 하나만 활성이어야 하므로 끄고 → 켠다.)
        if (_gameModeEventSystem != null)
            _gameModeEventSystem.SetActive(false);
        if (_xrUiEventSystem != null)
            _xrUiEventSystem.SetActive(true);

        // 3) 게임플레이 모드에서 꺼져 있던 UI 입력을 다시 켠다(레이 → UI 클릭).
        if (_uiInteractionToggle != null)
            _uiInteractionToggle.SetUIInteraction(true);

        // 4) 시야는 자유롭게 두되, 원하면 이동만 막는다.
        SetBehavioursEnabled(_movementToDisable, false);

        // 5) 흰 화면 등 UI 를 가릴 수 있는 오브젝트를 끈다.
        SetObjectsActive(_hideOnShow, false);

        if (_endingUiRoot != null)
        {
            if (_faceCameraOnShow)
                PlaceInFrontOfCamera();

            _endingUiRoot.SetActive(true);
        }

        OnEndingUiShown?.Invoke();
    }

    /// <summary>
    /// 재시작: 게임 상태를 완전히 초기화한 뒤 메인 메뉴 모드로 LobbyMap 을 다시 로드한다.
    /// (LobbyMap 재로드 시 GameManager 등 모든 씬 상태가 새로 초기화된다.)
    /// </summary>
    public void Restart()
    {
        // _selectSound?.Play();
        OnBeforeRestart?.Invoke();

        _fadeInOut.FadeOut(_fadeDuration, () =>
        {
            // 1. 현재 씬에 배치되어 있는 LobbyModeController 인스턴스를 동적으로 탐색합니다.
            LobbyModeController lobbyController = FindAnyObjectByType<LobbyModeController>();

            if (lobbyController != null)
            {
                // 2. 리팩토링된 인스턴스 메서드를 통해 메인 메뉴 모드로 전환 및 심리스 로드를 수행합니다.
                lobbyController.LoadAsMenu(_lobbySceneName);
            }
            else
            {
                // 방어 코드: 만약 현재 씬에 LobbyModeController 배치를 깜빡했거나 없는 경우를 대비한 예외 처리
                Debug.LogWarning("[EndingUIController] 현재 씬에 LobbyModeController 인스턴스를 찾을 수 없어 심리스 기능 없이 강제 전환합니다.");
                if (SceneLoadManager.Instance != null)
                {
                    SceneLoadManager.Instance.NextMode = LobbyModeController.LobbyMode.MainMenu;
                    SceneLoadManager.Instance.LoadSceneSeamless(_lobbySceneName);
                }
            }
        });
    }

    /// <summary>
    /// 종료: 애플리케이션을 종료한다.
    /// </summary>
    public void Quit()
    {
        // _selectSound?.Play();
        OnBeforeQuit?.Invoke();

        _fadeInOut.FadeOut(_fadeDuration, () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        });
    }

    private void PlaceInFrontOfCamera()
    {
        Transform cam = ResolveCamera();
        if (cam == null)
            return;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = cam.position + forward * _distanceFromCamera;
        position.y += _verticalOffset;

        _endingUiRoot.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }

    private Transform ResolveCamera()
    {
        if (_playerCamera != null)
            return _playerCamera;

        if (Camera.main != null)
        {
            _playerCamera = Camera.main.transform;
            return _playerCamera;
        }

        return null;
    }

    private static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        foreach (var behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (var obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
