using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ProximityUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _maxDistance = 3f;

    private XRBaseInteractable _interactable;
    private InputAction _leftPrimaryButton;
    private bool _isHovered = false;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);
    }

    void OnEnable()
    {
        _leftPrimaryButton = new InputAction(binding: "<XRController>{LeftHand}/{PrimaryButton}");
        _leftPrimaryButton.performed += OnLeftPrimaryPressed;
        _leftPrimaryButton.Enable();
    }

    void OnDisable()
    {
        _leftPrimaryButton.performed -= OnLeftPrimaryPressed;
        _leftPrimaryButton?.Disable();
    }

    void OnDestroy()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    void Update()
    {
        if (_uiPanel == null || _playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool isClose = distance <= _maxDistance;

        _uiPanel.SetActive(_isHovered && isClose);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        _isHovered = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        _isHovered = false;
    }

    private void OnLeftPrimaryPressed(InputAction.CallbackContext context)
    {
        if (_uiPanel != null && _uiPanel.activeInHierarchy)
        {
            gameObject.SetActive(false);
            Debug.Log("[ProximityUI] 오브젝트 사라짐");
        }
    }
}
