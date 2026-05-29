using UnityEngine;

public class MapSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _baseMap; // 기본 복도 구조
    [SerializeField] private GameObject _switchMap; // 바꿀 복도 구조

    public void SwitchMap()
    {
        _baseMap.SetActive(false);
        _switchMap.SetActive(true);
    }
}
