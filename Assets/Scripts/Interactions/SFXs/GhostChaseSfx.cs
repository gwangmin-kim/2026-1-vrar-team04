using UnityEngine;

public class GhostChaseSfx : MonoBehaviour
{
    [SerializeField] private OneShotPlayer _scream;
    [SerializeField] private OneShotPlayer _footstep;
    [SerializeField] private OneShotPlayer _attack;

    public void PlayScream()
    {
        _scream.Play();
    }

    public void PlayFootstep()
    {
        _footstep.Play();
    }

    public void PlayAttack()
    {
        _attack.Play();
    }
}
