using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class InfoPaper : MonoBehaviour
{
    [SerializeField] private GameObject _paper;
    [SerializeField] private float _handOffsetY; // 손으로부터 종이를 위치시킬 수직 오프셋
    [SerializeField] private float _handOffsetAngle; // 손으로부터 종이를 위치시킬 각도 오프셋

    [Header("Animation")]
    [SerializeField] private float _animMoveDistance; // 아래에서 위로 올라오는 애니메이션. 올라오는 거리
    [SerializeField] private float _animMoveAngle; // 아래에서 위로 올라오는 애니메이션. 올라오는 각도 (최종값은 0)
    [SerializeField] private float _animDuration; // 올라오는 시간

    [Header("SFX")]
    [SerializeField] private OneShotPlayer _soundPlayer;

    private void OnEnable()
    {
        AttachToHand();
    }

    public void AttachToHand()
    {
        Vector3 startPosition = (_handOffsetY - _animMoveDistance) * Vector3.up;
        Quaternion startRotation = Quaternion.Euler(_handOffsetAngle - _animMoveAngle, 0f, 0f);
        transform.SetLocalPositionAndRotation(startPosition, startRotation);

        transform.DOLocalMoveY(_handOffsetY, _animDuration).SetEase(Ease.OutQuad);
        transform.DOLocalRotate(_handOffsetAngle * Vector3.right, _animDuration).SetEase(Ease.OutQuad);

        _soundPlayer.Play();
    }

    public void Release()
    {
        _soundPlayer.Play();

        transform.DOLocalMoveY(_handOffsetY - _animMoveDistance, _animDuration).SetEase(Ease.OutQuad);
        transform.DOLocalRotate((_handOffsetAngle - _animMoveAngle) * Vector3.right, _animDuration).SetEase(Ease.OutQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
