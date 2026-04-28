using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerInputLogger : MonoBehaviour
{
    [SerializeField] private InputActionAsset xriActions;

    private InputAction leftPrimary;
    private InputAction leftSecondary;
    private InputAction rightPrimary;
    private InputAction rightSecondary;
    private InputAction leftMenu;

    void OnEnable()
    {
        Bind("XRI Left Interaction", "Select", "Left Grip");
        Bind("XRI Left Interaction", "Activate", "Left Trigger");
        Bind("XRI Right Interaction", "Select", "Right Grip");
        Bind("XRI Right Interaction", "Activate", "Right Trigger");

        xriActions?.Enable();

        leftPrimary = new InputAction(binding: "<XRController>{LeftHand}/{PrimaryButton}");
        leftSecondary = new InputAction(binding: "<XRController>{LeftHand}/{SecondaryButton}");
        rightPrimary = new InputAction(binding: "<XRController>{RightHand}/{PrimaryButton}");
        rightSecondary = new InputAction(binding: "<XRController>{RightHand}/{SecondaryButton}");
        leftMenu = new InputAction(binding: "<XRController>{LeftHand}/{MenuButton}");

        leftPrimary.performed += _ => Debug.Log("[Controller] Left Primary (X) pressed");
        leftSecondary.performed += _ => Debug.Log("[Controller] Left Secondary (Y) pressed");
        rightPrimary.performed += _ => Debug.Log("[Controller] Right Primary (A) pressed");
        rightSecondary.performed += _ => Debug.Log("[Controller] Right Secondary (B) pressed");
        leftMenu.performed += _ => Debug.Log("[Controller] Left Menu pressed");

        leftPrimary.Enable();
        leftSecondary.Enable();
        rightPrimary.Enable();
        rightSecondary.Enable();
        leftMenu.Enable();
    }

    void OnDisable()
    {
        xriActions?.Disable();
        leftPrimary?.Disable();
        leftSecondary?.Disable();
        rightPrimary?.Disable();
        rightSecondary?.Disable();
        leftMenu?.Disable();
    }

    void Bind(string mapName, string actionName, string logName)
    {
        var action = xriActions?.FindAction($"{mapName}/{actionName}");
        if (action != null)
            action.performed += _ => Debug.Log($"[Controller] {logName} pressed");
        else
            Debug.LogWarning($"[Controller] Action not found: {mapName}/{actionName}");
    }
}
