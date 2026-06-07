using System.Collections;
using UnityEngine;

public class MazeFloorManager : MonoBehaviour
{
    [SerializeField] private MapSwitcher _mapSwitcher;
    [SerializeField] private int _countNeedToClear; // 클리어까지 코너를 돌아야 하는 횟수
    [SerializeField] private int _currentCount = 0;

    [Header("On Complete")]
    [SerializeField] private GameObject _teleportTriggers; // 완료 시 플레이어 순간이동 비활성화
    [SerializeField] private Elevator[] _exitElevatorList; // 완료 시 출구 엘리베이터 활성화

    [Header("SFXs")]
    [SerializeField] private AudioSource[] _oneshotSfxList;
    [SerializeField] private Vector2 _sfxIntervalRange;
    private Coroutine _soundRoutine;
    private int _soundIndex = 0;

    private void OnEnable()
    {
        if (!_mapSwitcher.gameObject.activeSelf)
        {
            _mapSwitcher.gameObject.SetActive(true);
        }
    }

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

        // 주기적 sfx 재생 해제
        StopSoundRoutine();
    }

    public void StartSoundRoutine()
    {
        StopSoundRoutine();
        _soundRoutine = StartCoroutine(SoundRoutine());
    }

    public void StopSoundRoutine()
    {
        if (_soundRoutine == null) return;
        StopCoroutine(_soundRoutine);
        _soundRoutine = null;
    }

    private IEnumerator SoundRoutine()
    {
        while (true)
        {
            float randomWaitTime = Random.Range(_sfxIntervalRange.x, _sfxIntervalRange.y);
            yield return new WaitForSeconds(randomWaitTime);

            if (_oneshotSfxList.Length == 0) continue;

            // int randomIndex = Random.Range(0, _oneshotSfxList.Length - 1);
            _soundIndex++;
            if (_soundIndex >= _oneshotSfxList.Length) _soundIndex = 0;

            _oneshotSfxList[_soundIndex].Play();
        }
    }
}
