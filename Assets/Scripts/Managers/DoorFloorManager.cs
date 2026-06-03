using UnityEngine;

public class DoorFloorManager : MonoBehaviour
{
    [SerializeField] private int _doorCount;
    [SerializeField] private int _currentCount = 0;

    public void OnDoorClosed()
    {
        _currentCount++;

        if (_currentCount >= _doorCount && GameManager.Instance != null)
        {
            GameManager.Instance.ClearStage();
        }
    }
}
