using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerInputLogger : MonoBehaviour
{
    [SerializeField] private InputActionAsset _xriActions;

    private InputAction _leftPrimary;
    private InputAction _leftSecondary;
    private InputAction _rightPrimary;
    private InputAction _rightSecondary;
    private InputAction _leftMenu;

    void OnEnable()
    {
        Bind("XRI Left Interaction", "Select", "Left Grip");
        Bind("XRI Left Interaction", "Activate", "Left Trigger");
        Bind("XRI Right Interaction", "Select", "Right Grip");
        Bind("XRI Right Interaction", "Activate", "Right Trigger");

        _xriActions?.Enable();

        _leftPrimary = new InputAction(binding: "<XRController>{LeftHand}/{PrimaryButton}");
        _leftSecondary = new InputAction(binding: "<XRController>{LeftHand}/{SecondaryButton}");
        _rightPrimary = new InputAction(binding: "<XRController>{RightHand}/{PrimaryButton}");
        _rightSecondary = new InputAction(binding: "<XRController>{RightHand}/{SecondaryButton}");
        _leftMenu = new InputAction(binding: "<XRController>{LeftHand}/{MenuButton}");

        _leftPrimary.performed += _ => Debug.Log("[Controller] Left Primary (X) pressed");
        _leftSecondary.performed += _ => Debug.Log("[Controller] Left Secondary (Y) pressed");
        _rightPrimary.performed += _ => Debug.Log("[Controller] Right Primary (A) pressed");
        _rightSecondary.performed += _ => Debug.Log("[Controller] Right Secondary (B) pressed");
        _leftMenu.performed += _ => Debug.Log("[Controller] Left Menu pressed");

        _leftPrimary.Enable();
        _leftSecondary.Enable();
        _rightPrimary.Enable();
        _rightSecondary.Enable();
        _leftMenu.Enable();
    }

    void OnDisable()
    {
        _xriActions?.Disable();
        _leftPrimary?.Disable();
        _leftSecondary?.Disable();
        _rightPrimary?.Disable();
        _rightSecondary?.Disable();
        _leftMenu?.Disable();
    }

    void Bind(string mapName, string actionName, string logName)
    {
        var action = _xriActions?.FindAction($"{mapName}/{actionName}");
        if (action != null)
        {
            action.performed += _ => Debug.Log($"[Controller] {logName} pressed");
        }
        else
        {
            Debug.LogWarning($"[Controller] Action not found: {mapName}/{actionName}");
        }
    }
}
