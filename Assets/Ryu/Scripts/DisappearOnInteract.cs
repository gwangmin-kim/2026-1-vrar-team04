using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DisappearOnInteract : MonoBehaviour
{
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
        if (_isHovered)
        {
            gameObject.SetActive(false);
            Debug.Log("[DisappearOnInteract] 오브젝트 사라짐");
        }
    }
}
