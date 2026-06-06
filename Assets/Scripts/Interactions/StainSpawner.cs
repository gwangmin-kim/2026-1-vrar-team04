using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRARTeam04.Player;

public class StainSpawner : MonoBehaviour
{
    [Header("Object Pool")]
    [SerializeField] private Transform _stainPoolRoot;
    [SerializeField] private List<BloodStain> _stainPool;

    [Header("Raycast Setting")]
    [SerializeField] private Vector2 _spreadAngles; // 비산 각도 범위 (각각 수평, 수직 방향, +- 대칭 범위로 사용)
    [SerializeField] private float _rayDistance; // 레이 사거리
    [SerializeField] private LayerMask _wallLayer; // 벽 레이어

    [Header("Spawn Setting")]
    [SerializeField] private float _hitOffset; // 레이 충돌 지점에서 떨어질 오프셋 거리(오프셋 위치로 decal projector 설정)
    [SerializeField] private float _spawnInterval; // 핏자국 생성 간격
    [SerializeField] private float _minScale; // 최소 크기 (배율)
    [SerializeField] private float _maxScale; // 최대 크기 (배율)

    [Header("Clear Condition")]
    [SerializeField] private int _totalCount = 0;
    [SerializeField] private int _cleanCount = 0;

    private void OnValidate()
    {
        // 자동 할당 로직
        _stainPool = new List<BloodStain>();

        foreach (var stain in _stainPoolRoot.GetComponentsInChildren<BloodStain>(true))
        {
            _stainPool.Add(stain);
        }
    }

    private void Awake()
    {
        foreach (var stain in _stainPool)
        {
            stain.gameObject.SetActive(false);
        }
    }

    public void StartBloodSplatterEvent()
    {
        StartCoroutine(SpawnSequence());
    }

    public void OnStainCleaned()
    {
        _cleanCount++;
        if (_cleanCount >= _totalCount)
        {
            GameManager.Instance.ClearStage();
        }
    }

    private IEnumerator SpawnSequence()
    {
        Vector3 origin = transform.position;
        Vector3 baseDirection = transform.forward;

        _totalCount = 0;
        _cleanCount = 0;
        foreach (var stain in _stainPool)
        {
            Quaternion spreadRotation = Quaternion.Euler(
                Random.Range(-_spreadAngles.y, _spreadAngles.y),
                Random.Range(-_spreadAngles.x, _spreadAngles.x),
                0f
            );

            Vector3 direction = spreadRotation * baseDirection;

            if (Physics.Raycast(origin, direction, out var hit, _rayDistance, _wallLayer))
            {
                Vector3 position = hit.point - direction * _hitOffset;

                float randomZ = Random.Range(0f, 360f);
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                Quaternion rotation = lookRotation * Quaternion.Euler(0f, 0f, randomZ);

                stain.transform.SetPositionAndRotation(position, rotation);

                float randomScale = Random.Range(_minScale, _maxScale);
                stain.transform.localScale = randomScale * Vector3.one;
                stain.decalProjector.size = new Vector3(randomScale, randomScale, 2f);
                stain.cleanTrigger.center = new Vector3(0f, 0f, 1.5f / randomScale); // 센터 위치가 스케일에 영향을 받지 않도록 역수를 곱해줌

                stain.spawner = this;
                _totalCount++;

                stain.gameObject.SetActive(true);
                stain.MarkSpawned();
                stain.audioPoint.position = hit.point;

                yield return new WaitForSeconds(_spawnInterval);
            }
        }
    }
}
