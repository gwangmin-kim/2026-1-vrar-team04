using UnityEngine;

[AddComponentMenu("Horror/Stain Spawn SFX (VR&AR Team 04)")]
public class StainSpawnSfx : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip[] _spawnClips;

    [SerializeField]
    private BloodStain _bloodStain;

    [SerializeField]
    private bool _autoSubscribeToBloodStain = true;

    [Header("Variation")]
    [SerializeField]
    private Vector2 _volumeRange = new Vector2(0.85f, 1f);

    [SerializeField]
    private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField]
    private float _cooldown = 0.03f;

    [Header("3D Sound")]
    [SerializeField, Range(0f, 1f)]
    private float _spatialBlend = 1f;

    [SerializeField]
    private float _minDistance = 0.2f;

    [SerializeField]
    private float _maxDistance = 8f;

    private float _lastPlayTime = -999f;
    private bool _subscribed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Play()
    {
        if (_audioSource == null) return;
        if (!TryPickClip(out AudioClip clip, out float volume, out float pitch)) return;

        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip, volume);
        _lastPlayTime = Time.time;
        Debug.Log($"[StainSpawnSfx] Played spawn SFX: {clip.name}", this);
    }

    public void PlayAt(Vector3 worldPosition)
    {
        if (!TryPickClip(out AudioClip clip, out float volume, out float pitch)) return;

        var sourceObject = new GameObject("Stain Spawn SFX");
        sourceObject.transform.position = worldPosition;

        var source = sourceObject.AddComponent<AudioSource>();
        ConfigureAudioSource(source);
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);

        float lifetime = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f;
        Destroy(sourceObject, lifetime);

        _lastPlayTime = Time.time;
        Debug.Log($"[StainSpawnSfx] Played spawn SFX at {worldPosition}: {clip.name}", this);
    }

    private void ResolveReferences()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_bloodStain == null)
            _bloodStain = GetComponent<BloodStain>();

        if (_audioSource != null)
            ConfigureAudioSource(_audioSource);
    }

    private void Subscribe()
    {
        if (!_autoSubscribeToBloodStain || _bloodStain == null || _subscribed)
            return;

        _bloodStain.OnSpawned.AddListener(Play);
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (_bloodStain == null || !_subscribed)
            return;

        _bloodStain.OnSpawned.RemoveListener(Play);
        _subscribed = false;
    }

    private bool TryPickClip(out AudioClip clip, out float volume, out float pitch)
    {
        clip = null;
        volume = 0f;
        pitch = 1f;

        if (_spawnClips == null || _spawnClips.Length == 0) return false;
        if (Time.time - _lastPlayTime < _cooldown) return false;

        clip = _spawnClips[Random.Range(0, _spawnClips.Length)];
        if (clip == null) return false;

        volume = Random.Range(_volumeRange.x, _volumeRange.y);
        pitch = Random.Range(_pitchRange.x, _pitchRange.y);
        return true;
    }

    private void ConfigureAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = _spatialBlend;
        audioSource.minDistance = _minDistance;
        audioSource.maxDistance = _maxDistance;
    }
}
