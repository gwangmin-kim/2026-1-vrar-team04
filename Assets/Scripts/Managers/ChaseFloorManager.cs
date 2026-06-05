using UnityEngine;

public class ChaseFloorManager : MonoBehaviour
{
    [SerializeField] private Collider[] _enableTriggerList;
    [SerializeField] private GameObject[] _disableMapList;
    [SerializeField] private Door _door;

    [SerializeField] private GhostChase _ghost;
    [SerializeField] private Transform _teleportPoint;
    [SerializeField] private int _targetIndexAfterTeleport = 3;

    private void OnEnable()
    {
        foreach (var trigger in _enableTriggerList)
        {
            trigger.enabled = true;
        }

        foreach (var mapPart in _disableMapList)
        {
            mapPart.SetActive(false);
        }

        _door.Initialize();
    }

    public void TeleportGhost()
    {
        _ghost.transform.SetPositionAndRotation(_teleportPoint.position, _teleportPoint.rotation);
        _ghost.SetTargetIndex(_targetIndexAfterTeleport);
        _ghost.StartRun();
    }
}
