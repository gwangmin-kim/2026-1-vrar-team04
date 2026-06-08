using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace VRARTeam04.Player
{
    [AddComponentMenu("XR/Locomotion/Player Control Lock (VR&AR Team 04)")]
    public sealed class PlayerControlLock : MonoBehaviour
    {
        [SerializeField] private bool _lockedOnStart;
        [SerializeField] private bool _autoCollectOnAwake = true;
        [SerializeField] private bool _disableLocomotion = true;
        [SerializeField] private bool _disableInteractors = true;
        [SerializeField] private bool _disableInteractorVisuals = true;
        [SerializeField] private bool _disableInputActionManagers = true;

        [Header("Tracked Pose Driver Settings")]
        [SerializeField] private bool _disableTrackedPoseDrivers = true;
        [Tooltip("true일 경우, 메인 카메라(HMD)에 붙은 포즈 드라이버는 수집에서 제외하여 고개 돌리기를 허용(멀미 방지)")]
        [SerializeField] private bool _excludeMainCameraHMD = true;

        [SerializeField] private bool _disableHeadBob = true;
        [SerializeField] private Behaviour[] _extraComponentsToDisable;

        private readonly List<Behaviour> _controlledComponents = new List<Behaviour>();
        private readonly List<Behaviour> _movementComponents = new List<Behaviour>();
        private readonly Dictionary<Behaviour, bool> _originalEnabledStates = new Dictionary<Behaviour, bool>();
        private bool _isLocked;

        public bool IsLocked => _isLocked;

        private void Awake()
        {
            if (_autoCollectOnAwake)
                CollectControlledComponents();

            CaptureOriginalStates();
        }

        private void Start()
        {
            SetLocked(_lockedOnStart);
        }

        public void Lock()
        {
            SetLocked(true);
        }

        public void Unlock()
        {
            SetLocked(false);
        }

        public void UnlockKeepingMovementLocked()
        {
            Unlock();

            foreach (var component in _movementComponents)
            {
                if (component != null)
                    component.enabled = false;
            }
        }

        public void SetLocked(bool locked)
        {
            if (_isLocked == locked)
                return;

            _isLocked = locked;

            if (locked)
                CaptureOriginalStates();

            foreach (var component in _controlledComponents)
            {
                if (component == null)
                    continue;

                if (locked)
                {
                    component.enabled = false;
                }
                else if (_originalEnabledStates.TryGetValue(component, out var wasEnabled))
                {
                    component.enabled = wasEnabled;
                }
            }

            if (!locked)
                _originalEnabledStates.Clear();
        }

        [ContextMenu("Collect Controlled Components")]
        public void CollectControlledComponents()
        {
            _controlledComponents.Clear();
            _movementComponents.Clear();

            if (_disableLocomotion)
            {
                foreach (var provider in GetComponentsInChildren<LocomotionProvider>(true))
                    AddControlledComponent(provider, isMovement: true);
            }

            if (_disableInteractors)
            {
                foreach (var interactor in GetComponentsInChildren<XRBaseInteractor>(true))
                    AddControlledComponent(interactor);
            }

            if (_disableInteractorVisuals)
            {
                foreach (var lineVisual in GetComponentsInChildren<XRInteractorLineVisual>(true))
                    AddControlledComponent(lineVisual);
            }

            if (_disableInputActionManagers)
            {
                foreach (var inputActionManager in GetComponentsInChildren<InputActionManager>(true))
                    AddControlledComponent(inputActionManager);
            }

            if (_disableTrackedPoseDrivers)
            {
                // 메인 카메라 태그를 가진 오브젝트 미리 확보
                Camera mainCam = Camera.main;

                foreach (var trackedPoseDriver in GetComponentsInChildren<TrackedPoseDriver>(true))
                {
                    // _excludeMainCameraHMD가 켜져 있고, 현재 검사 중인 포즈 드라이버가 메인 카메라에 붙어 있다면 수집 패스
                    if (_excludeMainCameraHMD && mainCam != null && trackedPoseDriver.gameObject == mainCam.gameObject)
                    {
                        continue;
                    }

                    AddControlledComponent(trackedPoseDriver);
                }
            }

            if (_disableHeadBob)
            {
                foreach (var headBob in GetComponentsInChildren<VRHeadBob>(true))
                    AddControlledComponent(headBob, isMovement: true);
            }

            if (_extraComponentsToDisable == null)
                return;

            foreach (var component in _extraComponentsToDisable)
                AddControlledComponent(component);
        }

        private void CaptureOriginalStates()
        {
            _originalEnabledStates.Clear();

            foreach (var component in _controlledComponents)
            {
                if (component != null && !_originalEnabledStates.ContainsKey(component))
                    _originalEnabledStates.Add(component, component.enabled);
            }
        }

        private void AddControlledComponent(Behaviour component, bool isMovement = false)
        {
            if (component == null || component == this || _controlledComponents.Contains(component))
                return;

            _controlledComponents.Add(component);

            if (isMovement)
                _movementComponents.Add(component);
        }
    }
}
