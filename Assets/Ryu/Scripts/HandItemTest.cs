using UnityEngine;
using UnityEngine.InputSystem;

public class HandItemTest : MonoBehaviour
{
    private InputAction _aButton;
    private InputAction _bButton;
    private GameObject _testCube;
    private GameObject _testSphere;

    public GameObject TestCube => _testCube;
    public GameObject TestSphere => _testSphere;

    void Start()
    {
        Camera mainCamera = Camera.main;

        _testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _testCube.transform.SetParent(mainCamera.transform);
        _testCube.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
        _testCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        _testCube.SetActive(false);

        _testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _testSphere.transform.SetParent(mainCamera.transform);
        _testSphere.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
        _testSphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        _testSphere.SetActive(false);
    }

    void OnEnable()
    {
        _aButton = new InputAction(binding: "<XRController>{RightHand}/{PrimaryButton}"); // (A button)
        _aButton.performed += OnAButtonPressed;
        _aButton.canceled += OnAButtonReleased;
        _aButton.Enable();

        _bButton = new InputAction(binding: "<XRController>{RightHand}/{SecondaryButton}"); // (B button)
        _bButton.performed += OnBButtonPressed;
        _bButton.canceled += OnBButtonReleased;
        _bButton.Enable();
    }

    void OnDisable()
    {
        _aButton.performed -= OnAButtonPressed;
        _aButton.canceled -= OnAButtonReleased;
        _aButton?.Disable();

        _bButton.performed -= OnBButtonPressed;
        _bButton.canceled -= OnBButtonReleased;
        _bButton?.Disable();
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

    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        _testSphere.SetActive(true);
        Debug.Log("[HandItemTest] B 버튼 눌림 - 구체 표시");
    }

    private void OnBButtonReleased(InputAction.CallbackContext context)
    {
        _testSphere.SetActive(false);
        Debug.Log("[HandItemTest] B 버튼 뗌 - 구체 숨김");
    }
}
