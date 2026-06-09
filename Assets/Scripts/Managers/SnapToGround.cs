using UnityEngine;

public class SnapToGround : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hit))
        {
            transform.position = hit.point;
        }
    }
#endif
}
