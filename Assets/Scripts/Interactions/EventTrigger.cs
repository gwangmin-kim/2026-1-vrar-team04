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
        onTriggerEnterEvent.Invoke(collider);
    }

    private void OnTriggerStay(Collider collider)
    {
        onTriggerStayEvent.Invoke(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        onTriggerExitEvent.Invoke(collider);
    }
}
