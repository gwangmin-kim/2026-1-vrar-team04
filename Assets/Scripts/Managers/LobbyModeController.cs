using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VRARTeam04.Player;

[AddComponentMenu("Horror/Lobby Mode Controller (VR&AR Team 04)")]
public class LobbyModeController : MonoBehaviour
{
    public enum LobbyMode
    {
        MainMenu,
        GameplayFloor
    }

    private static LobbyMode? _nextMode;
    private static bool _hasQueuedGameplayFloorPose;
    private static Vector3 _queuedGameplayFloorLocalPosition;
    private static Quaternion _queuedGameplayFloorLocalRotation;
    private static Quaternion _queuedGameplayFloorLocalCameraRotation;

    [Header("Scene Mode")]
    [SerializeField] private LobbyMode _initialMode = LobbyMode.MainMenu;

    [Header("Scenes")]
    [SerializeField] private string _gameplaySceneName = "Game";
    [SerializeField] private string _lobbySceneName = "LobbyMap";

    [Header("Menu Mode Roots")]
    [SerializeField] private GameObject[] _menuUiRoots;
    [SerializeField] private GameObject[] _startUiRoots;
    [SerializeField] private GameObject[] _quitUiRoots;

    [Header("Gameplay Floor Roots")]
    [SerializeField] private GameObject[] _gameplayRoots;

    [Header("Player Spawn")]
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private Transform _menuSpawnPoint;
    [SerializeField] private Transform _gameplayFloorSpawnPoint;
    [SerializeField] private Transform _gameplayFloorEntranceReference;
    [SerializeField] private bool _teleportPlayerOnModeEnter = true;

    [Header("Player UI Interaction")]
    [SerializeField] private XRUIInteractionToggle[] _uiInteractionToggles;

    [Header("Menu Start Flow")]
    [SerializeField] private GameObject[] _corridorBlockers;
    [SerializeField] private LobbyStartElevator _startElevator;
    [SerializeField] private LayerMask _playerLayers = 1 << 3;

    [Header("Audio")]
    [SerializeField] private OneShotPlayer _selectSound;
    [SerializeField] private OneShotPlayer _backSound;

    [Header("Events")]
    public UnityEvent OnEnterMenuMode;
    public UnityEvent OnEnterGameplayFloorMode;
    public UnityEvent OnBeforeStartGame;
    public UnityEvent OnBeforeQuit;

    public LobbyMode CurrentMode { get; private set; }
    public bool IsStartCorridorUnlocked { get; private set; }

    private void Start()
    {
        SetMode(_nextMode ?? _initialMode);
        _nextMode = null;
    }

    public void ShowMenuMode()
    {
        SetMode(LobbyMode.MainMenu);
    }

    public void ShowGameplayFloorMode()
    {
        SetMode(LobbyMode.GameplayFloor);
    }

    public void StartGame()
    {
        _selectSound?.Play();
        HideStartUi();
        OnBeforeStartGame?.Invoke();
        UnlockStartCorridor();
    }

    public void QuitGame()
    {
        _selectSound?.Play();
        HideQuitUi();
        OnBeforeQuit?.Invoke();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LoadLobbyAsMenu()
    {
        LoadLobby(LobbyMode.MainMenu);
    }

    public void LoadLobbyAsGameplayFloor()
    {
        LoadLobby(LobbyMode.GameplayFloor);
    }

    public void PlayBackSound()
    {
        _backSound?.Play();
    }

    public void HideStartUi()
    {
        SetRootsActive(_startUiRoots, false);
    }

    public void HideQuitUi()
    {
        SetRootsActive(_quitUiRoots, false);
    }

    public void UnlockStartCorridor()
    {
        IsStartCorridorUnlocked = true;
        SetRootsActive(_corridorBlockers, false);
    }

    public void OpenStartElevator()
    {
        if (!IsStartCorridorUnlocked || _startElevator == null)
            return;

        _startElevator.Open();
    }

    public void OpenStartElevator(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        OpenStartElevator();
    }

    public void LoadGameplayScene()
    {
        if (string.IsNullOrEmpty(_gameplaySceneName))
        {
            Debug.LogWarning("[LobbyModeController] Gameplay scene name is empty.", this);
            return;
        }

        SceneManager.LoadSceneAsync(_gameplaySceneName);
    }

    public void LoadGameplayScene(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        LoadGameplayScene();
    }

    public static void LoadAsMenu(string sceneName = "LobbyMap")
    {
        _nextMode = LobbyMode.MainMenu;
        SceneManager.LoadSceneAsync(sceneName);
    }

    public static void LoadAsGameplayFloor(string sceneName = "LobbyMap")
    {
        _nextMode = LobbyMode.GameplayFloor;
        SceneManager.LoadSceneAsync(sceneName);
    }

    public static void LoadAsGameplayFloor(string sceneName, Transform sourceElevator, Transform playerRoot)
    {
        QueueGameplayFloorPose(sourceElevator, playerRoot);
        LoadAsGameplayFloor(sceneName);
    }

    public static void QueueGameplayFloorPose(Transform sourceElevator, Transform playerRoot)
    {
        if (sourceElevator == null || playerRoot == null)
            return;

        _queuedGameplayFloorLocalPosition = sourceElevator.InverseTransformPoint(playerRoot.position);
        _queuedGameplayFloorLocalRotation = Quaternion.Inverse(sourceElevator.rotation) * playerRoot.rotation;
        _queuedGameplayFloorLocalCameraRotation = GetCameraRotationRelativeTo(sourceElevator, playerRoot);
        _hasQueuedGameplayFloorPose = true;
    }

    private void LoadLobby(LobbyMode mode)
    {
        if (string.IsNullOrEmpty(_lobbySceneName))
        {
            Debug.LogWarning("[LobbyModeController] Lobby scene name is empty.", this);
            return;
        }

        _nextMode = mode;
        SceneManager.LoadSceneAsync(_lobbySceneName);
    }

    private void SetMode(LobbyMode mode)
    {
        CurrentMode = mode;

        bool menuMode = mode == LobbyMode.MainMenu;
        SetRootsActive(_menuUiRoots, menuMode);
        SetRootsActive(_startUiRoots, menuMode);
        SetRootsActive(_quitUiRoots, menuMode);
        SetRootsActive(_gameplayRoots, !menuMode);
        SetRootsActive(_corridorBlockers, menuMode);
        SetUIInteraction(menuMode);
        IsStartCorridorUnlocked = !menuMode;

        if (_teleportPlayerOnModeEnter)
            TeleportPlayerOnModeEnter(mode);

        if (menuMode)
            OnEnterMenuMode?.Invoke();
        else
            OnEnterGameplayFloorMode?.Invoke();
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

    private void SetUIInteraction(bool enabled)
    {
        if (_uiInteractionToggles == null)
            return;

        foreach (var toggle in _uiInteractionToggles)
        {
            if (toggle != null)
                toggle.SetUIInteraction(enabled);
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if ((_playerLayers.value & (1 << other.gameObject.layer)) != 0)
            return true;

        for (Transform current = other.transform; current != null; current = current.parent)
        {
            if ((_playerLayers.value & (1 << current.gameObject.layer)) != 0)
                return true;
        }

        return false;
    }

    private void TeleportPlayerOnModeEnter(LobbyMode mode)
    {
        EnsurePlayerRoot();

        if (_playerRoot == null)
        {
            Debug.LogWarning("[LobbyModeController] Player root is not assigned, so spawn teleport was skipped.", this);
            return;
        }

        if (mode == LobbyMode.GameplayFloor && _hasQueuedGameplayFloorPose)
        {
            TeleportPlayerFromGameplayFloorOffset(
                _queuedGameplayFloorLocalPosition,
                _queuedGameplayFloorLocalRotation,
                _queuedGameplayFloorLocalCameraRotation);
            _hasQueuedGameplayFloorPose = false;
            return;
        }

        TeleportPlayerToModeSpawn(mode);
    }

    private void TeleportPlayerToModeSpawn(LobbyMode mode)
    {
        Transform spawnPoint = mode == LobbyMode.MainMenu
            ? _menuSpawnPoint
            : _gameplayFloorSpawnPoint;

        if (spawnPoint == null)
            return;

        _playerRoot.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    private void TeleportPlayerFromGameplayFloorOffset(Vector3 localPosition, Quaternion localRotation, Quaternion localCameraRotation)
    {
        Transform reference = _gameplayFloorEntranceReference != null
            ? _gameplayFloorEntranceReference
            : _gameplayFloorSpawnPoint;

        if (reference == null)
        {
            Debug.LogWarning("[LobbyModeController] Gameplay floor entrance reference is not assigned, so queued teleport was skipped.", this);
            return;
        }

        Vector3 newPosition = reference.TransformPoint(localPosition);
        Quaternion newRotation = reference.rotation * localRotation;

        _playerRoot.SetPositionAndRotation(newPosition, newRotation);
        ApplyCameraRotationToCamera(reference, localCameraRotation);
    }

    private void ApplyCameraRotationToCamera(Transform reference, Quaternion localCameraRotation)
    {
        Transform cameraTransform = GetPlayerCameraTransform(_playerRoot);
        if (cameraTransform == null)
            return;

        cameraTransform.rotation = reference.rotation * localCameraRotation;
    }

    private void EnsurePlayerRoot()
    {
        if (_playerRoot == null)
        {
            var playerLock = FindAnyObjectByType<PlayerControlLock>();
            if (playerLock != null)
                _playerRoot = playerLock.transform;
        }
    }

    private static Quaternion GetCameraRotationRelativeTo(Transform reference, Transform playerRoot)
    {
        Transform cameraTransform = GetPlayerCameraTransform(playerRoot);
        Quaternion cameraRotation = cameraTransform != null ? cameraTransform.rotation : playerRoot.rotation;
        return Quaternion.Inverse(reference.rotation) * cameraRotation;
    }

    private static Transform GetPlayerCameraTransform(Transform playerRoot)
    {
        if (playerRoot == null)
            return null;

        Camera playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
        return playerCamera != null ? playerCamera.transform : null;
    }
}
