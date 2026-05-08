using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _detectDistance;
    [SerializeField] private float _detectRadius;
    [SerializeField] private float _checkInterval = 0.05f;

    private bool _isTurnedOn = false;
    private float _checkTimer = 0f;

    private void OnEnable()
    {
        TurnOff();
    }

    private void Update()
    {
        if (_isTurnedOn)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= _checkInterval)
            {
                CheckLightTarget(_checkInterval);
                _checkTimer = 0f;
            }
        }
    }

    private void CheckLightTarget(float deltaTime)
    {
        Transform lightTransform = _light.transform;

        if (Physics.SphereCast(lightTransform.position, _detectRadius, lightTransform.forward, out var hit, _detectDistance, _targetLayer))
        {
            if (hit.transform.TryGetComponent<ILightable>(out var lightable))
            {
                lightable.Light(deltaTime);
            }
        }
    }

    public void TurnOn()
    {
        _light.enabled = true;
        _isTurnedOn = true;
    }

    public void TurnOff()
    {
        _light.enabled = false;
        _isTurnedOn = false;

        _checkTimer = 0f;
    }
}
