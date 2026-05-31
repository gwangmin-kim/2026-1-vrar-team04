using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    [SerializeField] private Transform _from;
    [SerializeField] private Transform _to;

    public void TeleportPlayer()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            Debug.LogWarning($"GameManager or Player not found");
            return;
        }

        Transform player = GameManager.Instance.player;

        Vector3 positionOffset = _from.InverseTransformPoint(player.position);
        Vector3 newPosition = _to.TransformPoint(positionOffset);
        Quaternion newRotation = _to.rotation
                                * Quaternion.Inverse(_from.rotation)
                                * player.rotation;

        player.SetPositionAndRotation(newPosition, newRotation);
    }
}
