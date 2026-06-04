using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Animator))]
[AddComponentMenu("Horror/Lobby Start Elevator (VR&AR Team 04)")]
public class LobbyStartElevator : MonoBehaviour
{
    private static readonly string OpenTrigger = "Open";
    private static readonly string CloseTrigger = "Close";

    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _doorCollider;
    [SerializeField] private bool _initializeClosedOnAwake = true;
    [SerializeField] private string _closedStateName = "Close";
    [SerializeField, Range(0f, 1f)] private float _closedStateNormalizedTime = 1f;
    [SerializeField] private string _gameplaySceneName = "Game";
    [SerializeField] private LayerMask _playerLayers = 1 << 3;

    [Header("Events")]
    public UnityEvent OnOpenTriggered;
    public UnityEvent OnCloseTriggered;
    public UnityEvent OnBeforeLoadGameplay;

    public bool IsOpen { get; private set; }

    private Transform _pendingPlayerRoot;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_initializeClosedOnAwake)
            InitializeClosed();
    }

    public void InitializeClosed()
    {
        IsOpen = false;

        if (_doorCollider != null)
            _doorCollider.enabled = true;

        if (_animator == null)
            return;

        _animator.ResetTrigger(OpenTrigger);
        _animator.ResetTrigger(CloseTrigger);

        if (string.IsNullOrEmpty(_closedStateName))
            return;

        int closedStateHash = Animator.StringToHash(_closedStateName);
        if (!_animator.HasState(0, closedStateHash))
            return;

        _animator.Play(closedStateHash, 0, _closedStateNormalizedTime);
        _animator.Update(0f);
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        if (_doorCollider != null)
            _doorCollider.enabled = false;

        if (_animator != null)
            _animator.SetTrigger(OpenTrigger);

        OnOpenTriggered?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        if (_doorCollider != null)
            _doorCollider.enabled = true;

        if (_animator != null)
            _animator.SetTrigger(CloseTrigger);

        OnCloseTriggered?.Invoke();
    }

    public void Close(Collider other)
    {
        Transform playerRoot = GetPlayerRoot(other);
        if (playerRoot == null)
            return;

        _pendingPlayerRoot = playerRoot;
        Close();
    }

    public void OnDoorClosed()
    {
        if (_pendingPlayerRoot == null)
            return;

        GameManager.QueueInitialEntrancePose(transform, _pendingPlayerRoot);
        LoadGameplay();
    }

    public void LoadGameplay()
    {
        if (string.IsNullOrEmpty(_gameplaySceneName))
        {
            Debug.LogWarning("[LobbyStartElevator] Gameplay scene name is empty.", this);
            return;
        }

        OnBeforeLoadGameplay?.Invoke();
        SceneManager.LoadSceneAsync(_gameplaySceneName);
    }

    public void LoadGameplay(Collider other)
    {
        Close(other);
    }

    private Transform GetPlayerRoot(Collider other)
    {
        if (other == null)
            return null;

        Transform playerRoot = null;

        for (Transform current = other.transform; current != null; current = current.parent)
        {
            if ((_playerLayers.value & (1 << current.gameObject.layer)) != 0)
                playerRoot = current;
        }

        return playerRoot;
    }
}
