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
        [SerializeField] private bool _disableTrackedPoseDrivers = true;
        [SerializeField] private bool _disableHeadBob = true;
        [SerializeField] private Behaviour[] _extraComponentsToDisable;

        private readonly List<Behaviour> _controlledComponents = new List<Behaviour>();
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

        public void SetLocked(bool locked)
        {
            if (_isLocked == locked)
                return;

            _isLocked = locked;

            foreach (var component in _controlledComponents)
            {
                if (component == null)
                    continue;

                if (locked)
                {
                    if (!_originalEnabledStates.ContainsKey(component))
                        _originalEnabledStates.Add(component, component.enabled);

                    component.enabled = false;
                }
                else if (_originalEnabledStates.TryGetValue(component, out var wasEnabled))
                {
                    component.enabled = wasEnabled;
                }
            }
        }

        [ContextMenu("Collect Controlled Components")]
        public void CollectControlledComponents()
        {
            _controlledComponents.Clear();

            if (_disableLocomotion)
            {
                foreach (var provider in GetComponentsInChildren<LocomotionProvider>(true))
                    AddControlledComponent(provider);
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
                foreach (var trackedPoseDriver in GetComponentsInChildren<TrackedPoseDriver>(true))
                    AddControlledComponent(trackedPoseDriver);
            }

            if (_disableHeadBob)
            {
                foreach (var headBob in GetComponentsInChildren<VRHeadBob>(true))
                    AddControlledComponent(headBob);
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

        private void AddControlledComponent(Behaviour component)
        {
            if (component == null || component == this || _controlledComponents.Contains(component))
                return;

            _controlledComponents.Add(component);
        }
    }
}
