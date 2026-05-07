using UnityEngine;

public class FloorTransitionTrigger : MonoBehaviour, IConditionReceiver
{
    [SerializeField] private Transform _xrOrigin;
    [SerializeField] private Transform _camera;
    [SerializeField] private Transform _successSpawn;
    [SerializeField] private Transform _failSpawn;

    private bool _conditionMet = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Transform target = _conditionMet ? _successSpawn : _failSpawn;
        if (target == null)
        {
            Debug.LogWarning("[FloorTransition] Spawn point가 비어있음");
            return;
        }

        Debug.Log($"[FloorTransition] 조건 {(_conditionMet ? "만족" : "불만족")} → {target.name}");
        TeleportTo(target.position);
    }

    private void TeleportTo(Vector3 worldPos)
    {
        if (_xrOrigin == null || _camera == null)
        {
            Debug.LogWarning("[FloorTransition] XR Origin 또는 Camera 참조 없음");
            return;
        }

        Vector3 cameraOffset = _camera.position - _xrOrigin.position;
        Vector3 newOriginPos = worldPos;
        newOriginPos.x -= cameraOffset.x;
        newOriginPos.z -= cameraOffset.z;

        _xrOrigin.position = newOriginPos;
    }

    public void SetCondition(bool met)
    {
        _conditionMet = met;
        Debug.Log($"[FloorTransition] 조건 변경: {_conditionMet}");
    }
}
