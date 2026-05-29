using UnityEngine;
using DG.Tweening;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Trashbin : MonoBehaviour
{
    [Header("Goal Setting")]
    [SerializeField] private int _bagCount; // 처리해야 할 쓰레기봉투 개수
    private int _currentCount = 0; // 현재 처리한 쓰레기봉투 개수

    [Header("Collision Setting")]
    [SerializeField] private string _trashTag = "Trash";

    [Header("Animation Setting")]
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _animTime;

    private void OnComplete()
    {
        GameManager.Instance.ClearStage();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(_trashTag))
        {
            GameObject trashBag = collision.gameObject;

            // 잡고 있다면 강제로 놓게 만듦
            if (trashBag.TryGetComponent<XRGrabInteractable>(out var grabInteractable) && grabInteractable.isSelected)
            {
                grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
            }

            // 물리 연산 중지
            collision.collider.enabled = false;
            if (trashBag.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = true;
            }

            Vector3 targetPosition = transform.position + _offset;

            Sequence trashSequence = DOTween.Sequence();

            trashSequence.Join(trashBag.transform.DOMove(targetPosition, _animTime).SetEase(Ease.InQuad));
            trashSequence.Join(trashBag.transform.DOScale(Vector3.zero, _animTime).SetEase(Ease.InQuad));

            trashSequence.OnComplete(() =>
            {
                trashBag.SetActive(false);

                Debug.Log($"{trashBag.name} complete!");
                _currentCount++;
                if (_currentCount >= _bagCount)
                {
                    OnComplete();
                }
            });
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + _offset, 0.1f);
    }
#endif
}
