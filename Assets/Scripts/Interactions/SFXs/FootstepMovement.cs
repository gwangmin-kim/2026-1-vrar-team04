using UnityEngine;
using DG.Tweening;

public class FootstepMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _endPoint;
    [SerializeField] private float _moveDuration = 5f; // 한쪽 지점까지 가는 데 걸리는 시간

    private void Start()
    {
        // 1. 시작 위치로 강제 초기화
        transform.position = _startPoint.position;

        // 2. DOTween을 이용해 endPoint까지 이동 후 왕복(Yoyo)하도록 무한 루프 설정
        transform.DOMove(_endPoint.position, _moveDuration)
            .SetEase(Ease.Linear) // 일정한 속도로 걷기
            .SetLoops(-1, LoopType.Yoyo); // -1은 무한 루프, Yoyo는 핑퐁 왕복
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴되거나 씬이 바뀔 때 트윈을 안전하게 종료
        transform.DOKill();
    }
}
