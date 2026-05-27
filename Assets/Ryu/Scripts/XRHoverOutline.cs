using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRBaseInteractable))]
public sealed class XRHoverOutline : MonoBehaviour
{
    [SerializeField] private bool _rayInteractorOnly = true;
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _outlineWidth = 0.025f;
    [SerializeField] private Behaviour[] _outlineEffects;

    private XRBaseInteractable _interactable;
    private int _rayHoverCount;
    private int _hoverCount;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        EnsureOutlineEffects();
        SetOutline(false);
    }

    private void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEntered);
        _interactable.hoverExited.RemoveListener(OnHoverExited);
        _interactable.selectEntered.RemoveListener(OnSelectEntered);
        _interactable.selectExited.RemoveListener(OnSelectExited);
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
        SetOutline(hasHover && !_interactable.isSelected);
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
