using UnityEngine;

public class Cleaner : MonoBehaviour
{
    public float linearSpeed = 0f;
    [SerializeField] private float _updateInterval = 0.1f;

    private Vector3 _prevPosition;
    private float _updateTimer = 0f;

    private void OnEnable()
    {
        _prevPosition = transform.position;
        linearSpeed = 0f;
    }

    private void Update()
    {
        _updateTimer -= Time.deltaTime;
        if (_updateTimer < 0f)
        {
            UpdateSpeed(_updateInterval);
            _updateTimer = _updateInterval;
        }
    }

    private void UpdateSpeed(float deltaTime)
    {
        Vector3 deltaPosition = transform.position - _prevPosition;
        linearSpeed = deltaPosition.magnitude / deltaTime;

        _prevPosition = transform.position;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent<ICleanable>(out var cleanable))
        {
            Debug.Log($"touched with {collider.name}");
            cleanable.Touch(this);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.TryGetComponent<ICleanable>(out var cleanable))
        {
            cleanable.Untouch();
        }
    }
}
