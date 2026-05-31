using UnityEngine;

public class MazeFloorManager : MonoBehaviour
{
    [SerializeField] private int _countNeedToClear; // 클리어까지 코너를 돌아야 하는 횟수
    [SerializeField] private int _currentCount = 0;

    [Header("On Complete")]
    [SerializeField] private GameObject _teleportTriggers; // 완료 시 플레이어 순간이동 비활성화
    [SerializeField] private Elevator[] _exitElevatorList; // 완료 시 출구 엘리베이터 활성화

    public void EncountCorner()
    {
        _currentCount++;

        if (_currentCount >= _countNeedToClear)
        {
            OnComplete();
        }
    }

    private void OnComplete()
    {
        // 필요는 없지만 일단 매니저도 상태를 알 수 있게 마킹
        GameManager.Instance.ClearStage();

        // 순간이동 트리거를 사용 해제하고 엘리베이터 활성화
        _teleportTriggers.SetActive(false);
        foreach (var ev in _exitElevatorList)
        {
            ev.gameObject.SetActive(true);
            ev.Initialize();
            ev.openTrigger.enabled = true;
            // ev.closeTrigger.enabled = true;
        }
    }
}
