using UnityEngine;

public class CubePlacementCondition : MonoBehaviour
{
    [SerializeField] private GameObject _targetCube;
    [SerializeField] private MonoBehaviour _triggerBehaviour;

    private IConditionReceiver _trigger;

    void Awake()
    {
        _trigger = _triggerBehaviour as IConditionReceiver;
        if (_trigger == null)
        {
            Debug.LogError("[CubePlacementCondition] _triggerBehaviour가 IConditionReceiver를 구현하지 않음");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_targetCube == null || _trigger == null) return;

        if (other.gameObject == _targetCube)
        {
            Debug.Log("[CubePlacement] 큐브가 영역에 올라감 조건 충족");
            _trigger.SetCondition(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_targetCube == null || _trigger == null) return;

        if (other.gameObject == _targetCube)
        {
            Debug.Log("[CubePlacement] 큐브가 영역에서 벗어남 조건 해제");
            _trigger.SetCondition(false);
        }
    }
}
