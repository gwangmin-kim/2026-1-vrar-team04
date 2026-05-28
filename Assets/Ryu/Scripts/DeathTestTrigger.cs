using UnityEngine;

namespace VRARTeam04.Player
{
    [AddComponentMenu("XR/Locomotion/Death Test Trigger (VR&AR Team 04)")]
    [RequireComponent(typeof(Collider))]
    public sealed class DeathTestTrigger : MonoBehaviour
    {
        [SerializeField] private PlayerControlLock _targetPlayerControlLock;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private LayerMask _triggerLayers = ~0;

        private bool _hasTriggered;

        private void Reset()
        {
            var triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void Awake()
        {
            var triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggerOnce && _hasTriggered)
                return;

            if (!IsValidTarget(other, out var playerControlLock))
                return;

            _hasTriggered = true;
            playerControlLock.Lock();
        }

        [ContextMenu("Trigger Death")]
        public void TriggerDeath()
        {
            var playerControlLock = _targetPlayerControlLock != null
                ? _targetPlayerControlLock
                : FindObjectOfType<PlayerControlLock>();

            if (playerControlLock == null)
            {
                Debug.LogWarning("No PlayerControlLock found for death trigger.", this);
                return;
            }

            _hasTriggered = true;
            playerControlLock.Lock();
        }

        private bool IsValidTarget(Collider other, out PlayerControlLock playerControlLock)
        {
            playerControlLock = null;

            if ((_triggerLayers.value & (1 << other.gameObject.layer)) == 0)
                return false;

            playerControlLock = _targetPlayerControlLock;
            if (playerControlLock == null)
                playerControlLock = other.GetComponentInParent<PlayerControlLock>();
            if (playerControlLock == null)
                playerControlLock = FindObjectOfType<PlayerControlLock>();

            return playerControlLock != null;
        }
    }
}
