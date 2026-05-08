using UnityEngine;

public class StainCleaner : MonoBehaviour
{
    [SerializeField] private float _requiredScrubDistance = 3f;
    [SerializeField] private float _minMovementThreshold = 0.001f;
    [SerializeField] private GameObject _stainVisual;

    private float _accumulatedDistance = 0f;
    private bool _isCleaned = false;

    private Transform _activeMopPart = null;
    private Vector3 _lastMopPosition;

    private void OnTriggerEnter(Collider other)
    {
        if (_isCleaned) return;
        if (!other.CompareTag("MopHead")) return;

        _activeMopPart = other.transform;
        _lastMopPosition = _activeMopPart.position;
        Debug.Log("[StainCleaner] 대걸레 접촉 시작");
    }

    private void OnTriggerStay(Collider other)
    {
        if (_isCleaned || _activeMopPart == null) return;
        if (other.transform != _activeMopPart) return;

        Vector3 currentPos = _activeMopPart.position;
        float frameDistance = Vector3.Distance(currentPos, _lastMopPosition);

        if (frameDistance > _minMovementThreshold)
        {
            _accumulatedDistance += frameDistance;
            Debug.Log($"[StainCleaner] 닦은 거리: {_accumulatedDistance:F2} / {_requiredScrubDistance}");

            if (_accumulatedDistance >= _requiredScrubDistance)
            {
                Clean();
            }
        }

        _lastMopPosition = currentPos;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == _activeMopPart)
        {
            _activeMopPart = null;
            Debug.Log("[StainCleaner] 대걸레 떨어짐");
        }
    }

    private void Clean()
    {
        _isCleaned = true;
        Debug.Log("[StainCleaner] 얼룩 제거 완료");

        if (_stainVisual != null)
        {
            _stainVisual.SetActive(false);
        }
    }

    public bool IsCleaned => _isCleaned;
}
