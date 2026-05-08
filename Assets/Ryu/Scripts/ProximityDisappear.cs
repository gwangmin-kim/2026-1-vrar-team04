using UnityEngine;

public class ProximityDisappear : MonoBehaviour
{
    [SerializeField] private HandItemTest _handItemTest;
    [SerializeField] private float _triggerDistance = 1.5f;

    void Update()
    {
        if (_handItemTest == null)
        {
            return;
        }

        if (IsItemClose(_handItemTest.TestCube) || IsItemClose(_handItemTest.TestSphere))
        {
            gameObject.SetActive(false);
            Debug.Log("[ProximityDisappear] 가까이 와서 사라짐");
        }
    }

    private bool IsItemClose(GameObject item)
    {
        if (item == null || !item.activeInHierarchy)
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, item.transform.position);
        return distance <= _triggerDistance;
    }
}
