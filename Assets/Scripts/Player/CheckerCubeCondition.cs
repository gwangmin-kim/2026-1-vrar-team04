using UnityEngine;

public class CheckerCubeCondition : MonoBehaviour
{
    [SerializeField] private GameObject _checkerCube;
    [SerializeField] private MonoBehaviour _triggerBehaviour;

    private IConditionReceiver _trigger;

    void Awake()
    {
        _trigger = _triggerBehaviour as IConditionReceiver;
        if (_trigger == null)
        {
            Debug.LogError("[CheckerCubeCondition] _triggerBehaviour가 IConditionReceiver를 구현하지 않음");
        }
    }

    void Update()
    {
        if (_checkerCube == null || _trigger == null) return;

        bool destroyed = !_checkerCube.activeInHierarchy;
        _trigger.SetCondition(destroyed);
    }
}
