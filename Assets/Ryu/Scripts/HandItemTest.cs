using UnityEngine;
using UnityEngine.InputSystem;

public class HandItemTest : MonoBehaviour
{
    private InputAction _aButton;
    private GameObject _testCube;

    void Start()
    {
        Camera mainCamera = Camera.main;

        _testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _testCube.transform.SetParent(mainCamera.transform);
        _testCube.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
        _testCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        _testCube.SetActive(false);
    }

    void OnEnable()
    {
        _aButton = new InputAction(binding: "<XRController>{RightHand}/{PrimaryButton}");
        _aButton.performed += OnAButtonPressed;
        _aButton.canceled += OnAButtonReleased;
        _aButton.Enable();
    }

    void OnDisable()
    {
        _aButton.performed -= OnAButtonPressed;
        _aButton.canceled -= OnAButtonReleased;
        _aButton?.Disable();
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        _testCube.SetActive(true);
        Debug.Log("[HandItemTest] A 버튼 눌림 - 큐브 표시");
    }

    private void OnAButtonReleased(InputAction.CallbackContext context)
    {
        _testCube.SetActive(false);
        Debug.Log("[HandItemTest] A 버튼 뗌 - 큐브 숨김");
    }
}
