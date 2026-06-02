using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRARTeam04.Player
{
    [AddComponentMenu("XR/Interaction/XR UI Interaction Toggle (VR&AR Team 04)")]
    public sealed class XRUIInteractionToggle : MonoBehaviour
    {
        [SerializeField] private bool _autoCollectOnAwake = true;
        [SerializeField] private bool _useSelectInputAsUIPress = true;
        [SerializeField] private XRRayInteractor[] _rayInteractors;

        private void Awake()
        {
            if (_autoCollectOnAwake)
                CollectRayInteractors();
        }

        [ContextMenu("Collect Ray Interactors")]
        public void CollectRayInteractors()
        {
            _rayInteractors = GetComponentsInChildren<XRRayInteractor>(true);
        }

        public void EnableUIInteraction()
        {
            SetUIInteraction(true);
        }

        public void DisableUIInteraction()
        {
            SetUIInteraction(false);
        }

        public void SetUIInteraction(bool enabled)
        {
            if (_rayInteractors == null)
                return;

            foreach (var rayInteractor in _rayInteractors)
            {
                if (rayInteractor == null)
                    continue;

                if (enabled && _useSelectInputAsUIPress)
                    rayInteractor.uiPressInput = rayInteractor.selectInput;

                rayInteractor.enableUIInteraction = enabled;
            }
        }
    }
}
