using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;

public class BloodStain : MonoBehaviour, ICleanable
{
    public StainSpawner spawner;

    [Header("Decal Projector")]
    public DecalProjector decalProjector;

    [Header("Collider")]
    [SerializeField] private Collider _collider;

    [Header("Clean Setting")]
    [SerializeField] private float _initialFade; // 최초 투명도
    [SerializeField] private float _cleanAmount; // 청소 판정 시 감소할 투명도
    [SerializeField] private float _rubDistance; // 청소 판정을 위해 걸레가 움직여야 할 거리
    [SerializeField] private bool _isCleaned = false;

    [SerializeField] private Cleaner _cleaner = null;
    [SerializeField] private float _accumulatedDistance = 0f;

    [Header("Events")]
    public UnityEvent OnSpawned;

    private bool IsBeingCleaned => _cleaner != null;

    private void Update()
    {
        if (IsBeingCleaned)
        {
            _accumulatedDistance += _cleaner.linearSpeed * Time.deltaTime;

            if (_accumulatedDistance >= _rubDistance)
            {
                Clean();
                _accumulatedDistance = 0f;
            }
        }
    }

    public void Touch(Cleaner cleaner)
    {
        _cleaner = cleaner;
    }

    public void Untouch()
    {
        _cleaner = null;
    }

    public void MarkSpawned()
    {
        _isCleaned = false;
        _cleaner = null;
        _accumulatedDistance = 0f;

        if (_collider != null)
            _collider.enabled = true;

        if (decalProjector != null)
            decalProjector.fadeFactor = _initialFade;

        OnSpawned?.Invoke();
    }

    public void Clean()
    {
        if (_isCleaned) return;

        decalProjector.fadeFactor -= _cleanAmount;

        if (decalProjector.fadeFactor <= 0f)
        {
            _isCleaned = true;
            _collider.enabled = false;
            spawner.OnStainCleaned();
        }
    }
}
