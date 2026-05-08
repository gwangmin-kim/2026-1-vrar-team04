using UnityEngine;
using UnityEngine.InputSystem;

public class LeftHandItem : MonoBehaviour
{
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Transform _controllerTransform;
    [SerializeField] private Vector3 _localPosition = new Vector3(0f, 0f, 0.1f);
    [SerializeField] private Vector3 _localEulerAngles = Vector3.zero;

    private InputAction _xButton;
    private GameObject _spawnedItem;

    public bool IsHolding => _spawnedItem != null && _spawnedItem.activeInHierarchy;

    void Start()
    {
        Transform parent = _controllerTransform != null ? _controllerTransform : transform;

        if (_itemPrefab != null)
        {
            _spawnedItem = Instantiate(_itemPrefab, parent);
            _spawnedItem.transform.localPosition = _localPosition;
            _spawnedItem.transform.localEulerAngles = _localEulerAngles;
            _spawnedItem.SetActive(false);
        }
    }

    void OnEnable()
    {
        _xButton = new InputAction(binding: "<XRController>{LeftHand}/{PrimaryButton}");
        _xButton.performed += OnXButtonPressed;
        _xButton.Enable();
    }

    void OnDisable()
    {
        _xButton.performed -= OnXButtonPressed;
        _xButton?.Disable();
    }

    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        if (_spawnedItem == null)
        {
            return;
        }

        _spawnedItem.SetActive(!_spawnedItem.activeInHierarchy);
        Debug.Log($"[LeftHandItem] 아이템 {(_spawnedItem.activeInHierarchy ? "등장" : "소멸")}");
    }
}
