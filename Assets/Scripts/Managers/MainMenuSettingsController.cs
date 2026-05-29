using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRARTeam04.Player;

/// <summary>
/// Main menu settings bridge for UI sliders/toggles.
/// Keeps values in PlayerPrefs and applies them to optional runtime components.
/// </summary>
[AddComponentMenu("Horror/Main Menu Settings Controller (VR&AR Team 04)")]
public class MainMenuSettingsController : MonoBehaviour
{
    private const string MasterVolumeKey = "vrar.team04.settings.masterVolume";
    private const string MoveSpeedKey = "vrar.team04.settings.moveSpeed";
    private const string TurnSpeedKey = "vrar.team04.settings.turnSpeed";
    private const string VignetteKey = "vrar.team04.vignette.enabled";

    [Header("Menu")]
    [SerializeField] private MainMenuController _mainMenuController;

    [Header("Fallback UI")]
    [SerializeField] private bool _buildFallbackUiWhenMissing = true;
    [SerializeField] private TMP_FontAsset _fallbackFontAsset;

    [Header("UI Controls")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _moveSpeedSlider;
    [SerializeField] private Slider _turnSpeedSlider;
    [SerializeField] private Toggle _vignetteToggle;

    [Header("UI Labels (Optional)")]
    [SerializeField] private TMP_Text _masterVolumeValueLabel;
    [SerializeField] private TMP_Text _moveSpeedValueLabel;
    [SerializeField] private TMP_Text _turnSpeedValueLabel;
    [SerializeField] private TMP_Text _vignetteValueLabel;

    [Header("Targets (Optional)")]
    [Tooltip("Usually the SmoothDynamicMoveProvider or ContinuousMoveProvider on XR Origin.")]
    [SerializeField] private Component _moveProvider;

    [Tooltip("Usually the ContinuousTurnProvider on XR Origin.")]
    [SerializeField] private Component _turnProvider;

    [SerializeField] private VignetteToggle _vignetteSetting;

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume = 0.8f;
    [SerializeField, Min(0.1f)] private float _defaultMoveSpeed = 1.4f;
    [SerializeField, Min(1f)] private float _defaultTurnSpeed = 60f;
    [SerializeField] private bool _defaultVignetteEnabled = false;

    private void Awake()
    {
        AutoAssignTargets();
        BuildFallbackUiIfNeeded();
        ConfigureSliderRanges();
        BindControlEvents();
    }

    private void OnEnable()
    {
        LoadApplyAndSyncControls();
    }

    public void SetMasterVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
        AudioListener.volume = clamped;
        UpdateMasterVolumeLabel(clamped);
        PlayerPrefs.Save();
    }

    public void SetMoveSpeed(float value)
    {
        float clamped = Mathf.Max(0.1f, value);
        PlayerPrefs.SetFloat(MoveSpeedKey, clamped);
        TrySetFloat(_moveProvider, clamped, "moveSpeed", "m_MoveSpeed");
        UpdateMoveSpeedLabel(clamped);
        PlayerPrefs.Save();
    }

    public void SetTurnSpeed(float value)
    {
        float clamped = Mathf.Max(1f, value);
        PlayerPrefs.SetFloat(TurnSpeedKey, clamped);
        TrySetFloat(_turnProvider, clamped, "turnSpeed", "m_TurnSpeed");
        UpdateTurnSpeedLabel(clamped);
        PlayerPrefs.Save();
    }

    public void SetVignetteEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(VignetteKey, enabled ? 1 : 0);

        if (_vignetteSetting != null)
            _vignetteSetting.SetEnabled(enabled);

        UpdateVignetteLabel(enabled);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        SetMasterVolume(_defaultMasterVolume);
        SetMoveSpeed(_defaultMoveSpeed);
        SetTurnSpeed(_defaultTurnSpeed);
        SetVignetteEnabled(_defaultVignetteEnabled);
        SyncControlsFromCurrentValues();
    }

    public void CloseSettings()
    {
        if (_mainMenuController != null)
            _mainMenuController.CloseSettings();
    }

    public void Initialize(MainMenuController mainMenuController)
    {
        _mainMenuController = mainMenuController;
    }

    private void LoadApplyAndSyncControls()
    {
        SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, _defaultMasterVolume));
        SetMoveSpeed(PlayerPrefs.GetFloat(MoveSpeedKey, _defaultMoveSpeed));
        SetTurnSpeed(PlayerPrefs.GetFloat(TurnSpeedKey, _defaultTurnSpeed));
        SetVignetteEnabled(PlayerPrefs.GetInt(VignetteKey, _defaultVignetteEnabled ? 1 : 0) == 1);
        SyncControlsFromCurrentValues();
    }

    private void SyncControlsFromCurrentValues()
    {
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);

        if (_moveSpeedSlider != null)
            _moveSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MoveSpeedKey, _defaultMoveSpeed));

        if (_turnSpeedSlider != null)
            _turnSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(TurnSpeedKey, _defaultTurnSpeed));

        if (_vignetteToggle != null)
            _vignetteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(VignetteKey, _defaultVignetteEnabled ? 1 : 0) == 1);
    }

    private void ConfigureSliderRanges()
    {
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.minValue = 0f;
            _masterVolumeSlider.maxValue = 1f;
        }

        if (_moveSpeedSlider != null)
        {
            _moveSpeedSlider.minValue = 0.5f;
            _moveSpeedSlider.maxValue = 3f;
        }

        if (_turnSpeedSlider != null)
        {
            _turnSpeedSlider.minValue = 15f;
            _turnSpeedSlider.maxValue = 120f;
        }
    }

    private void BindControlEvents()
    {
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (_moveSpeedSlider != null)
        {
            _moveSpeedSlider.onValueChanged.RemoveListener(SetMoveSpeed);
            _moveSpeedSlider.onValueChanged.AddListener(SetMoveSpeed);
        }

        if (_turnSpeedSlider != null)
        {
            _turnSpeedSlider.onValueChanged.RemoveListener(SetTurnSpeed);
            _turnSpeedSlider.onValueChanged.AddListener(SetTurnSpeed);
        }

        if (_vignetteToggle != null)
        {
            _vignetteToggle.onValueChanged.RemoveListener(SetVignetteEnabled);
            _vignetteToggle.onValueChanged.AddListener(SetVignetteEnabled);
        }
    }

    private void BuildFallbackUiIfNeeded()
    {
        if (!_buildFallbackUiWhenMissing) return;
        if (_masterVolumeSlider != null || _moveSpeedSlider != null || _turnSpeedSlider != null || _vignetteToggle != null) return;
        if (transform.Find("SettingsContent") != null) return;

        var panelRect = transform as RectTransform;
        if (panelRect == null) return;

        var panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.02f, 0.02f, 0.025f, 0.72f);
            panelImage.raycastTarget = false;
        }

        var content = CreateRect("SettingsContent", panelRect);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(760f, 620f);

        CreatePanelImage(content, new Color(0.06f, 0.055f, 0.05f, 0.92f), false);

        var title = CreateText("Title", content, "설정", 54f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(640f, 80f));

        CreateSliderRow(content, "MasterVolume", "마스터 볼륨", 0f, -170f, 0f, 1f, out _masterVolumeSlider, out _masterVolumeValueLabel);
        CreateSliderRow(content, "MoveSpeed", "이동 속도", 0f, -270f, 0.5f, 3f, out _moveSpeedSlider, out _moveSpeedValueLabel);
        CreateSliderRow(content, "TurnSpeed", "회전 속도", 0f, -370f, 15f, 120f, out _turnSpeedSlider, out _turnSpeedValueLabel);

        CreateToggleRow(content, "Vignette", "멀미 방지 비네팅", 0f, -470f, out _vignetteToggle, out _vignetteValueLabel);

        var resetButton = CreateButton("ResetButton", content, "기본값", new Vector2(-145f, -550f), new Vector2(220f, 56f));
        resetButton.onClick.AddListener(ResetToDefaults);

        var backButton = CreateButton("BackButton", content, "뒤로", new Vector2(145f, -550f), new Vector2(220f, 56f));
        backButton.onClick.AddListener(CloseSettings);
    }

    private void CreateSliderRow(RectTransform parent, string name, string label, float x, float y, float minValue, float maxValue, out Slider slider, out TMP_Text valueLabel)
    {
        var row = CreateRect(name + "Row", parent);
        SetRect(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(620f, 72f));

        var labelText = CreateText(name + "Label", row, label, 30f, FontStyles.Normal, TextAlignmentOptions.Left);
        SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 14f), new Vector2(260f, 44f));

        valueLabel = CreateText(name + "Value", row, "", 26f, FontStyles.Normal, TextAlignmentOptions.Right);
        SetRect(valueLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-64f, 14f), new Vector2(150f, 44f));

        slider = CreateSlider(name + "Slider", row, minValue, maxValue);
        SetRect((RectTransform)slider.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(620f, 24f));
    }

    private void CreateToggleRow(RectTransform parent, string name, string label, float x, float y, out Toggle toggle, out TMP_Text valueLabel)
    {
        var row = CreateRect(name + "Row", parent);
        SetRect(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(620f, 64f));

        var labelText = CreateText(name + "Label", row, label, 30f, FontStyles.Normal, TextAlignmentOptions.Left);
        SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(380f, 48f));

        valueLabel = CreateText(name + "Value", row, "", 26f, FontStyles.Normal, TextAlignmentOptions.Right);
        SetRect(valueLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-64f, 0f), new Vector2(150f, 44f));

        toggle = CreateToggle(name + "Toggle", row);
        SetRect((RectTransform)toggle.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(54f, 54f));
    }

    private static Slider CreateSlider(string name, RectTransform parent, float minValue, float maxValue)
    {
        var root = CreateRect(name, parent);
        var slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        var background = CreateRect("Background", root);
        SetRect(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreatePanelImage(background, new Color(0.17f, 0.16f, 0.15f, 1f), false);

        var fillArea = CreateRect("Fill Area", root);
        SetRect(fillArea, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f));

        var fill = CreateRect("Fill", fillArea);
        SetRect(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fillImage = CreatePanelImage(fill, new Color(0.76f, 0.56f, 0.25f, 1f), false);

        var handleArea = CreateRect("Handle Slide Area", root);
        SetRect(handleArea, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f));

        var handle = CreateRect("Handle", handleArea);
        SetRect(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
        var handleImage = CreatePanelImage(handle, new Color(0.92f, 0.82f, 0.62f, 1f));

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Toggle CreateToggle(string name, RectTransform parent)
    {
        var root = CreateRect(name, parent);
        var toggle = root.gameObject.AddComponent<Toggle>();

        var background = CreateRect("Background", root);
        SetRect(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backgroundImage = CreatePanelImage(background, new Color(0.17f, 0.16f, 0.15f, 1f), true);

        var checkmark = CreateRect("Checkmark", background);
        SetRect(checkmark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
        var checkmarkImage = CreatePanelImage(checkmark, new Color(0.78f, 0.58f, 0.28f, 1f), false);

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        return toggle;
    }

    private Button CreateButton(string name, RectTransform parent, string label, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent);
        SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
        var image = CreatePanelImage(rect, new Color(0.22f, 0.18f, 0.13f, 0.95f), true);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = new Color(0.22f, 0.18f, 0.13f, 0.95f);
        colors.highlightedColor = new Color(0.54f, 0.42f, 0.24f, 1f);
        colors.pressedColor = new Color(0.74f, 0.58f, 0.32f, 1f);
        button.colors = colors;

        var text = CreateText("Label", rect, label, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreatePanelImage(RectTransform rect, Color color, bool raycastTarget = true)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private TMP_Text CreateText(string name, RectTransform parent, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var rect = CreateRect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        if (_fallbackFontAsset != null)
            label.font = _fallbackFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = new Color(0.95f, 0.89f, 0.76f, 1f);
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void AutoAssignTargets()
    {
        if (_vignetteSetting == null)
            _vignetteSetting = FindObjectOfType<VignetteToggle>(true);

        if (_moveProvider == null)
            _moveProvider = FindComponentByTypeName("SmoothDynamicMoveProvider", "ContinuousMoveProvider");

        if (_turnProvider == null)
            _turnProvider = FindComponentByTypeName("ContinuousTurnProvider");
    }

    private static Component FindComponentByTypeName(params string[] typeNames)
    {
        var components = FindObjectsOfType<Component>(true);
        foreach (var component in components)
        {
            if (component == null) continue;

            string componentTypeName = component.GetType().Name;
            foreach (string typeName in typeNames)
            {
                if (componentTypeName == typeName)
                    return component;
            }
        }

        return null;
    }

    private void UpdateMasterVolumeLabel(float value)
    {
        if (_masterVolumeValueLabel != null)
            _masterVolumeValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void UpdateMoveSpeedLabel(float value)
    {
        if (_moveSpeedValueLabel != null)
            _moveSpeedValueLabel.text = $"{value:0.0} m/s";
    }

    private void UpdateTurnSpeedLabel(float value)
    {
        if (_turnSpeedValueLabel != null)
            _turnSpeedValueLabel.text = $"{Mathf.RoundToInt(value)} deg/s";
    }

    private void UpdateVignetteLabel(bool enabled)
    {
        if (_vignetteValueLabel != null)
            _vignetteValueLabel.text = enabled ? "ON" : "OFF";
    }

    private static void TrySetFloat(Component target, float value, params string[] memberNames)
    {
        if (target == null) return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = target.GetType();

        foreach (string memberName in memberNames)
        {
            var property = FindProperty(type, memberName, flags);
            if (property != null && property.CanWrite && property.PropertyType == typeof(float))
            {
                property.SetValue(target, value);
                return;
            }

            var field = FindField(type, memberName, flags);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(target, value);
                return;
            }
        }

        Debug.LogWarning($"[{nameof(MainMenuSettingsController)}] Could not set float setting on {target.GetType().Name}.", target);
    }

    private static PropertyInfo FindProperty(System.Type type, string propertyName, BindingFlags flags)
    {
        while (type != null)
        {
            var property = type.GetProperty(propertyName, flags);
            if (property != null)
                return property;

            type = type.BaseType;
        }

        return null;
    }

    private static FieldInfo FindField(System.Type type, string fieldName, BindingFlags flags)
    {
        while (type != null)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}
