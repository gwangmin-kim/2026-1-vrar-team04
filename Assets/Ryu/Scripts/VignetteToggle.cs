using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace VRARTeam04.Player
{
    /// <summary>
    /// Tunneling Vignette 의 ON/OFF 를 런타임에 토글한다.
    /// - 기본값 OFF (멀미에 민감한 사용자만 켜는 옵트인 옵션).
    /// - 컨트롤러 버튼(InputAction) 으로도, UI 버튼 OnClick 으로도, 코드에서도 호출 가능.
    /// - 마지막 선택은 PlayerPrefs 에 저장돼 다음 실행에도 유지된다.
    ///
    /// 사용법:
    /// 1) 빈 GameObject (예: "LocomotionSettings") 에 이 컴포넌트를 추가.
    /// 2) _vignetteRoot 에 씬에 부착된 TunnelingVignette 프리팹 인스턴스를 드래그.
    /// 3) (선택) _toggleAction 에 InputActionReference 를 연결 — 예: 컨트롤러 Menu 버튼.
    /// 4) (선택) UI 토글이나 버튼의 OnClick 에 Toggle() / SetEnabled(bool) 을 연결.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Vignette Toggle (VR&AR Team 04)")]
    public class VignetteToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("켜고 끌 TunnelingVignette 게임오브젝트 (보통 Main Camera 아래에 부착된 프리팹 인스턴스).")]
        private GameObject _vignetteRoot;

        [Header("Input (Optional)")]
        [SerializeField, Tooltip("이 InputAction 이 performed 될 때마다 토글된다. 비워두면 버튼 입력은 무시. InputActionReference 를 드래그.")]
        private InputActionReference _toggleAction;

        [Header("Default")]
        [SerializeField, Tooltip("저장된 값이 없을 때 사용할 초기 상태. 기본 OFF 권장.")]
        private bool _defaultOn = false;

        [SerializeField, Tooltip("PlayerPrefs 저장 키. 다른 설정과 충돌하지 않게 고유하게 유지.")]
        private string _playerPrefsKey = "vrar.team04.vignette.enabled";

        [Header("Events")]
        [Tooltip("상태가 바뀌면 호출 (UI 텍스트/아이콘 갱신용).")]
        public UnityEvent<bool> OnStateChanged;

        private bool _isOn;

        /// <summary>현재 비네팅이 켜져있는지.</summary>
        public bool IsEnabled => _isOn;

        private void OnEnable()
        {
            // 저장된 값 로드 (없으면 _defaultOn)
            int saved = PlayerPrefs.GetInt(_playerPrefsKey, _defaultOn ? 1 : 0);
            _isOn = saved == 1;
            ApplyState(invokeEvent: true);

            // 입력 액션 구독
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.performed += OnTogglePerformed;
                _toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.performed -= OnTogglePerformed;
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            Toggle();
        }

        /// <summary>UI Button.OnClick 또는 외부 코드에서 호출.</summary>
        public void Toggle()
        {
            SetEnabled(!_isOn);
        }

        /// <summary>UI Toggle.OnValueChanged(bool) 또는 외부 코드에서 호출.</summary>
        public void SetEnabled(bool on)
        {
            if (_isOn == on) return;
            _isOn = on;
            PlayerPrefs.SetInt(_playerPrefsKey, on ? 1 : 0);
            PlayerPrefs.Save();
            ApplyState(invokeEvent: true);
        }

        private void ApplyState(bool invokeEvent)
        {
            if (_vignetteRoot != null)
                _vignetteRoot.SetActive(_isOn);

            if (invokeEvent)
                OnStateChanged?.Invoke(_isOn);
        }
    }
}
