using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Flashlight 의 완성형 — 두 버튼으로 분리 제어.
///
/// 입력 매핑 (Meta Quest 오른손 컨트롤러):
///   • Primary (A) 버튼   → flashlight prefab 활성화/비활성화 (visibility 토글)
///   • Secondary (B) 버튼 → light 켜기/끄기 (활성 상태일 때만)
///
/// 비활성 상태에서는 B 입력 무시. 비활성화하면 light 상태는 *기억*되어
/// 다시 활성화했을 때 직전 light 상태로 복귀함.
///
/// 사운드: 상태가 바뀔 때마다 OneShotPlayer.Play() 호출.
/// 더 정교한 사운드 분리 (켜는 클릭 vs 끄는 클릭) 가 필요하면 UnityEvent 4개에
/// 각각 다른 OneShotPlayer 를 연결하면 됨.
///
/// 셋업:
/// 1) 이 컴포넌트는 *항상 활성인 GameObject* 에 붙일 것 (예: Right Hand Controller).
/// 2) FlashlightBody: 보일/숨길 flashlight prefab GameObject 드래그.
/// 3) Light: 그 안의 Light 컴포넌트.
/// 4) ToggleSound: OneShotPlayer. 같은 GameObject 에 두면 자동 검색.
///
/// 기존 Flashlight.cs 의 SphereCast 기반 ILightable 감지 로직은 그대로 유지.
/// </summary>
[AddComponentMenu("Horror/Flashlight Completed (VR&AR Team 04)")]
public class Flashlight_completed : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("토글할 flashlight prefab GameObject. SetActive 로 보임/숨김. 비워두면 light 만 on/off.")]
    private GameObject _flashlightBody;

    [SerializeField, Tooltip("실제 빛을 내는 Light 컴포넌트.")]
    private Light _light;

    [Header("Audio")]
    [SerializeField, Tooltip("상태 변화 시 재생할 OneShotPlayer. 비워두면 같은 GameObject 에서 자동 검색.")]
    private OneShotPlayer _toggleSound;

    [Header("Light Detection (기존 Flashlight.cs 로직 유지)")]
    [SerializeField, Tooltip("빛이 닿으면 반응할 레이어 (ILightable 검사 대상).")]
    private LayerMask _targetLayer;

    [SerializeField] private float _detectDistance = 5f;
    [SerializeField] private float _detectRadius = 0.3f;
    [SerializeField] private float _checkInterval = 0.05f;

    [Header("Initial State")]
    [SerializeField, Tooltip("ON 이면 시작 시 flashlight 가 활성화된 상태.")]
    private bool _startsActive = false;

    [SerializeField, Tooltip("ON 이면 시작 시 light 가 켜진 상태 (활성화돼있을 때만 의미).")]
    private bool _startsLightOn = false;

    [Header("Events")]
    [Tooltip("flashlight 가 활성화 (A 버튼) 되었을 때.")]
    public UnityEvent OnActivated;

    [Tooltip("flashlight 가 비활성화되었을 때.")]
    public UnityEvent OnDeactivated;

    [Tooltip("light 가 켜졌을 때 (B 버튼).")]
    public UnityEvent OnLightOn;

    [Tooltip("light 가 꺼졌을 때.")]
    public UnityEvent OnLightOff;

    /// <summary>현재 flashlight 가 활성 (보임) 상태인지.</summary>
    public bool IsActive { get; private set; }

    /// <summary>현재 light 가 켜진 상태인지. 비활성 상태일 땐 false 가 보장됨.</summary>
    public bool IsLightOn { get; private set; }

    // 코드로 만든 InputAction (인스펙터 슬롯 불필요)
    private InputAction _activateAction;
    private InputAction _lightToggleAction;

    private float _checkTimer = 0f;

    private void Awake()
    {
        if (_toggleSound == null) _toggleSound = GetComponent<OneShotPlayer>();

        // 셋업 실수 경고: 토글 대상이 자기 자신이면 OFF 시 스크립트가 같이 꺼짐
        if (_flashlightBody == gameObject)
        {
            Debug.LogWarning(
                "[Flashlight_completed] FlashlightBody 가 이 스크립트가 붙은 GameObject 와 동일합니다. " +
                "비활성화 시 입력 핸들러도 같이 꺼져 다시 켤 수 없게 됩니다. " +
                "스크립트를 부모 또는 별도 GameObject 로 옮기세요.",
                this);
        }

        // ─── InputAction 코드로 생성 ───────────────────────
        // Meta Quest 오른손 컨트롤러:
        //   primaryButton   = A 버튼 (활성화/비활성화)
        //   secondaryButton = B 버튼 (light on/off)
        _activateAction = new InputAction(
            name: "Flashlight/Activate",
            type: InputActionType.Button,
            binding: "<XRController>{RightHand}/primaryButton");

        _lightToggleAction = new InputAction(
            name: "Flashlight/ToggleLight",
            type: InputActionType.Button,
            binding: "<XRController>{RightHand}/secondaryButton");

        // ─── 초기 상태 적용 (이벤트·사운드 없이) ──────────
        IsActive  = _startsActive;
        IsLightOn = _startsActive && _startsLightOn;
        ApplyVisibility();
        ApplyLight();
    }

    private void OnEnable()
    {
        _activateAction.performed += OnActivatePressed;
        _activateAction.Enable();

        _lightToggleAction.performed += OnLightTogglePressed;
        _lightToggleAction.Enable();
    }

    private void OnDisable()
    {
        if (_activateAction != null)
        {
            _activateAction.performed -= OnActivatePressed;
            _activateAction.Disable();
        }
        if (_lightToggleAction != null)
        {
            _lightToggleAction.performed -= OnLightTogglePressed;
            _lightToggleAction.Disable();
        }
    }

    private void OnDestroy()
    {
        _activateAction?.Dispose();
        _lightToggleAction?.Dispose();
    }

    // ─── 입력 핸들러 ───────────────────────────────────

    private void OnActivatePressed(InputAction.CallbackContext ctx)
    {
        ToggleActive();
    }

    private void OnLightTogglePressed(InputAction.CallbackContext ctx)
    {
        // 비활성 상태면 light 토글 무시 (손에 없는 손전등은 못 켬)
        if (!IsActive) return;
        ToggleLight();
    }

    // ─── 공개 API ─────────────────────────────────────

    /// <summary>현재 visibility 상태 반전. UnityEvent 또는 외부 코드에서 호출 가능.</summary>
    public void ToggleActive()
    {
        if (IsActive) Deactivate();
        else Activate();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        _toggleSound?.Play();
        ApplyVisibility();
        ApplyLight();   // 직전 light 상태 복원
        OnActivated?.Invoke();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        _toggleSound?.Play();
        ApplyVisibility();
        ApplyLight();   // 비활성 → light 도 같이 시각적으로 꺼지지만 IsLightOn 값은 유지
        OnDeactivated?.Invoke();
    }

    /// <summary>light 의 켜짐/꺼짐 반전. 비활성 상태에선 무시됨.</summary>
    public void ToggleLight()
    {
        if (!IsActive) return;
        if (IsLightOn) TurnLightOff();
        else TurnLightOn();
    }

    public void TurnLightOn()
    {
        if (IsLightOn || !IsActive) return;
        IsLightOn = true;
        _toggleSound?.Play();
        ApplyLight();
        OnLightOn?.Invoke();
    }

    public void TurnLightOff()
    {
        if (!IsLightOn) return;
        IsLightOn = false;
        _toggleSound?.Play();
        ApplyLight();
        OnLightOff?.Invoke();
        _checkTimer = 0f;
    }

    // ─── 빛 감지 (기존 Flashlight.cs 로직) ─────────────

    private void Update()
    {
        if (!IsActive || !IsLightOn || _light == null) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer >= _checkInterval)
        {
            CheckLightTarget(_checkInterval);
            _checkTimer = 0f;
        }
    }

    private void CheckLightTarget(float deltaTime)
    {
        Transform lightTransform = _light.transform;
        if (Physics.SphereCast(lightTransform.position, _detectRadius, lightTransform.forward, out var hit, _detectDistance, _targetLayer))
        {
            if (hit.transform.TryGetComponent<ILightable>(out var lightable))
            {
                lightable.Light(deltaTime);
            }
        }
    }

    // ─── 상태 → 실제 GameObject/Light 반영 ─────────────

    private void ApplyVisibility()
    {
        if (_flashlightBody != null) _flashlightBody.SetActive(IsActive);
    }

    private void ApplyLight()
    {
        // 실제 Light 컴포넌트는 *활성 + light on* 일 때만 켜짐
        if (_light != null) _light.enabled = IsActive && IsLightOn;
    }
}
