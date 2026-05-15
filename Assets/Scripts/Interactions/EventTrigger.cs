using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EventTrigger : MonoBehaviour
{
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }

    [Header("Trigger Callbacks")]
    public TriggerEvent onTriggerEnterEvent = new TriggerEvent();
    public TriggerEvent onTriggerStayEvent = new TriggerEvent();
    public TriggerEvent onTriggerExitEvent = new TriggerEvent();

    private void OnTriggerEnter(Collider collider)
    {
#if UNITY_EDITOR
        Debug.Log($"[{name}]: trigger enter detected with {collider.name}");
#endif
        onTriggerEnterEvent.Invoke(collider);
    }

    private void OnTriggerStay(Collider collider)
    {
        onTriggerStayEvent.Invoke(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
#if UNITY_EDITOR
        Debug.Log($"[{name}]: trigger exit detected with {collider.name}");
#endif
        onTriggerExitEvent.Invoke(collider);
    }
}
