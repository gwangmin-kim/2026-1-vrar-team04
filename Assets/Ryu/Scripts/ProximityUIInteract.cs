using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ProximityUIInteract : MonoBehaviour
{
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LeftHandItem _leftHandItem;
    [SerializeField] private float _maxDistance = 3f;

    private XRBaseInteractable _interactable;
    private InputAction _yButton;
    private bool _isHovered = false;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);

        if (_playerTransform == null && Camera.main != null)
        {
            _playerTransform = Camera.main.transform;
            Debug.Log("[ProximityUIInteract] _playerTransform 자동 할당: Main Camera");
        }
    }

    void OnEnable()
    {
        _yButton = new InputAction(binding: "<XRController>{LeftHand}/{SecondaryButton}");
        _yButton.performed += OnYButtonPressed;
        _yButton.Enable();
    }

    void OnDisable()
    {
        _yButton.performed -= OnYButtonPressed;
        _yButton?.Disable();
    }

    void OnDestroy()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    void Update()
    {
        if (_uiPanel == null || _playerTransform == null || _leftHandItem == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool isClose = distance <= _maxDistance;
        bool showUI = _isHovered && isClose && _leftHandItem.IsHolding;

        _uiPanel.SetActive(showUI);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        _isHovered = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        _isHovered = false;
    }

    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        if (_uiPanel != null && _uiPanel.activeInHierarchy)
        {
            gameObject.SetActive(false);
            _uiPanel.SetActive(false);
            Debug.Log("[ProximityUIInteract] 오브젝트 사라짐");
        }
    }
}
