using UnityEngine;

public class PlayerBodyFollower : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    [SerializeField] private bool _followY = false;
    [SerializeField] private float _yOffset = 0f;

    void LateUpdate()
    {
        if (_camera == null) return;

        Vector3 camPos = _camera.position;
        Vector3 newPos = transform.position;

        newPos.x = camPos.x;
        newPos.z = camPos.z;
        if (_followY)
        {
            newPos.y = camPos.y + _yOffset;
        }

        transform.position = newPos;
    }
}
