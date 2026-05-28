using UnityEngine;

[AddComponentMenu("XR/Audio/Mop Movement Loop SFX (VR&AR Team 04)")]
public class MopMovementLoopSfx : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Cleaner _cleaner;

    [SerializeField]
    private OneShotPlayer _oneShotPlayer;

    [Header("Speed Threshold")]
    [SerializeField, Min(0f)]
    private float _startSpeed = 0.12f;

    [SerializeField, Min(0f)]
    private float _stopSpeed = 0.06f;

    private bool _isPlaying;

    private void Awake()
    {
        if (_cleaner == null)
            _cleaner = GetComponent<Cleaner>() ?? GetComponentInChildren<Cleaner>();

        if (_oneShotPlayer == null)
            _oneShotPlayer = GetComponent<OneShotPlayer>() ?? GetComponentInChildren<OneShotPlayer>();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void Update()
    {
        if (_cleaner == null || _oneShotPlayer == null)
            return;

        float speed = _cleaner.linearSpeed;

        if (!_isPlaying && speed >= _startSpeed)
        {
            _oneShotPlayer.PlayLoop();
            _isPlaying = true;
        }
        else if (_isPlaying && speed <= _stopSpeed)
        {
            StopLoop();
        }
    }

    private void StopLoop()
    {
        if (!_isPlaying || _oneShotPlayer == null)
            return;

        _oneShotPlayer.StopLoop();
        _isPlaying = false;
    }

    private void OnValidate()
    {
        if (_stopSpeed > _startSpeed)
            _stopSpeed = _startSpeed;
    }
}
