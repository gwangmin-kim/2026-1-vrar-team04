using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class XRHoverOutline : MonoBehaviour
{
    [SerializeField] private XRBaseInteractable _interactableSource;
    [SerializeField] private bool _rayInteractorOnly = true;
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _outlineWidth = 0.01f;
    [SerializeField] private Behaviour[] _outlineEffects;

    private int _rayHoverCount;
    private int _hoverCount;

    private void Awake()
    {
        ResolveInteractable();
        EnsureOutlineEffects();
        SetOutline(false);
    }

    private void OnEnable()
    {
        if (!ResolveInteractable())
            return;

        _interactableSource.hoverEntered.AddListener(OnHoverEntered);
        _interactableSource.hoverExited.AddListener(OnHoverExited);
        _interactableSource.selectEntered.AddListener(OnSelectEntered);
        _interactableSource.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        if (_interactableSource != null)
        {
            _interactableSource.hoverEntered.RemoveListener(OnHoverEntered);
            _interactableSource.hoverExited.RemoveListener(OnHoverExited);
            _interactableSource.selectEntered.RemoveListener(OnSelectEntered);
            _interactableSource.selectExited.RemoveListener(OnSelectExited);
        }

        _rayHoverCount = 0;
        _hoverCount = 0;
        SetOutline(false);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (_rayInteractorOnly && !(args.interactorObject is XRRayInteractor))
            return;

        if (args.interactorObject is XRRayInteractor)
            _rayHoverCount++;

        _hoverCount++;
        RefreshOutline();
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (_rayInteractorOnly && !(args.interactorObject is XRRayInteractor))
            return;

        if (args.interactorObject is XRRayInteractor)
            _rayHoverCount = Mathf.Max(0, _rayHoverCount - 1);

        _hoverCount = Mathf.Max(0, _hoverCount - 1);

        RefreshOutline();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        SetOutline(false);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        RefreshOutline();
    }

    private void RefreshOutline()
    {
        var hasHover = _rayInteractorOnly ? _rayHoverCount > 0 : _hoverCount > 0;
        SetOutline(hasHover && (_interactableSource == null || !_interactableSource.isSelected));
    }

    private bool ResolveInteractable()
    {
        if (_interactableSource != null)
            return true;

        _interactableSource = GetComponent<XRBaseInteractable>();
        if (_interactableSource == null)
            _interactableSource = GetComponentInParent<XRBaseInteractable>();
        if (_interactableSource == null)
            _interactableSource = GetComponentInChildren<XRBaseInteractable>();

        if (_interactableSource != null)
            return true;

        Debug.LogWarning("XRHoverOutline requires an XRBaseInteractable, XRGrabInteractable, or another XR interactable on this object, a parent, or a child.", this);
        enabled = false;
        return false;
    }

    private void EnsureOutlineEffects()
    {
        if (_outlineEffects != null && _outlineEffects.Length > 0)
            return;

        var outline = GetComponent<SimpleMeshOutline>();
        if (outline == null)
            outline = gameObject.AddComponent<SimpleMeshOutline>();

        outline.Configure(_outlineColor, _outlineWidth);
        _outlineEffects = new Behaviour[] { outline };
    }

    private void OnValidate()
    {
        var outline = GetComponent<SimpleMeshOutline>();
        if (outline != null)
            outline.Configure(_outlineColor, _outlineWidth);
    }

    private void SetOutline(bool visible)
    {
        if (_outlineEffects == null)
            return;

        foreach (var outlineEffect in _outlineEffects)
        {
            if (outlineEffect != null)
                outlineEffect.enabled = visible;
        }
    }
}
