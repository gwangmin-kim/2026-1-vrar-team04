using UnityEngine;

public class GhostChaseSfx : MonoBehaviour
{
    [SerializeField] private OneShotPlayer _breath;
    [SerializeField] private OneShotPlayer _scream;
    [SerializeField] private OneShotPlayer _shortScream;
    [SerializeField] private OneShotPlayer _footstep;
    [SerializeField] private OneShotPlayer _attack;

    private void OnEnable()
    {
        PlayBreathLoop();
    }

    public void PlayBreathLoop()
    {
        _breath.PlayLoop();
    }

    public void PlayScream()
    {
        _breath.StopLoop();
        _scream.Play();
    }

    public void PlayShortScream()
    {
        _breath.StopLoop();
        _shortScream.Play();
    }

    public void PlayFootstep()
    {
        _footstep.Play();
    }

    public void PlayAttack()
    {
        _breath.StopLoop();
        _attack.Play();
    }
}
