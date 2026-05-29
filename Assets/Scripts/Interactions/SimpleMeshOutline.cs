using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class SimpleMeshOutline : MonoBehaviour
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _outlineWidth = 0.025f;
    [SerializeField] private Transform _rendererRoot;

    private readonly List<GameObject> _outlineObjects = new List<GameObject>();
    private Material _outlineMaterial;

    private void OnEnable()
    {
        BuildOutline();
    }

    private void OnDisable()
    {
        ClearOutline();
    }

    private void OnValidate()
    {
        if (_outlineMaterial == null)
            return;

        ApplyMaterialProperties();
    }

    public void Configure(Color outlineColor, float outlineWidth)
    {
        _outlineColor = outlineColor;
        _outlineWidth = Mathf.Max(0f, outlineWidth);

        if (_outlineMaterial != null)
            ApplyMaterialProperties();
    }

    private void BuildOutline()
    {
        ClearOutline();

        var shader = Shader.Find("Hidden/Ryu/SimpleMeshOutline");
        if (shader == null)
        {
            Debug.LogWarning("SimpleMeshOutline shader was not found.", this);
            return;
        }

        _outlineMaterial = new Material(shader)
        {
            name = "Runtime Simple Mesh Outline",
            hideFlags = HideFlags.HideAndDontSave,
        };
        ApplyMaterialProperties();

        var root = _rendererRoot != null ? _rendererRoot : transform;
        foreach (var meshRenderer in root.GetComponentsInChildren<MeshRenderer>())
            CreateMeshOutline(meshRenderer);

        foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            CreateSkinnedMeshOutline(skinnedRenderer);
    }

    private void CreateMeshOutline(MeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null)
            return;

        var sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
            return;

        var outlineObject = CreateOutlineObject(sourceRenderer.transform, sourceRenderer.gameObject.layer);
        var outlineFilter = outlineObject.AddComponent<MeshFilter>();
        var outlineRenderer = outlineObject.AddComponent<MeshRenderer>();

        outlineFilter.sharedMesh = sourceFilter.sharedMesh;
        ConfigureRenderer(outlineRenderer, sourceRenderer);
    }

    private void CreateSkinnedMeshOutline(SkinnedMeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
            return;

        var outlineObject = CreateOutlineObject(sourceRenderer.transform, sourceRenderer.gameObject.layer);
        var outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();

        outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
        outlineRenderer.rootBone = sourceRenderer.rootBone;
        outlineRenderer.bones = sourceRenderer.bones;
        outlineRenderer.localBounds = sourceRenderer.localBounds;
        ConfigureRenderer(outlineRenderer, sourceRenderer);
    }

    private GameObject CreateOutlineObject(Transform parent, int layer)
    {
        var outlineObject = new GameObject("__SimpleMeshOutline")
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = layer,
        };

        var outlineTransform = outlineObject.transform;
        outlineTransform.SetParent(parent, false);
        outlineTransform.localPosition = Vector3.zero;
        outlineTransform.localRotation = Quaternion.identity;
        outlineTransform.localScale = Vector3.one;

        _outlineObjects.Add(outlineObject);
        return outlineObject;
    }

    private void ConfigureRenderer(Renderer outlineRenderer, Renderer sourceRenderer)
    {
        outlineRenderer.sharedMaterial = _outlineMaterial;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = sourceRenderer.sortingOrder;
        outlineRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
    }

    private void ApplyMaterialProperties()
    {
        _outlineMaterial.SetColor(OutlineColorId, _outlineColor);
        _outlineMaterial.SetFloat(OutlineWidthId, _outlineWidth);
    }

    private void ClearOutline()
    {
        foreach (var outlineObject in _outlineObjects)
        {
            if (outlineObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(outlineObject);
            else
                DestroyImmediate(outlineObject);
        }

        _outlineObjects.Clear();

        if (_outlineMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_outlineMaterial);
        else
            DestroyImmediate(_outlineMaterial);

        _outlineMaterial = null;
    }
}
