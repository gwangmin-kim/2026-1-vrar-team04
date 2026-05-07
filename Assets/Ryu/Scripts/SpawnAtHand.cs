using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SpawnAtHand : MonoBehaviour
{
    private XRBaseInteractable _interactable;
    private GameObject _heldItem;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        _interactable.selectEntered.AddListener(OnSelected);
    }

    void Start()
    {
        _heldItem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _heldItem.name = "HeldItem";
        _heldItem.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _heldItem.SetActive(false);
    }

    void OnDestroy()
    {
        _interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Transform handTransform = ((MonoBehaviour)args.interactorObject).transform;

        _heldItem.SetActive(true);
        _heldItem.transform.SetParent(handTransform);
        _heldItem.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        _heldItem.transform.localRotation = Quaternion.identity;

        gameObject.SetActive(false);
        Debug.Log("[SpawnAtHand] 아이템이 손에 등장");
    }

    public GameObject GetHeldItem()
    {
        return _heldItem;
    }
}
