using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource shootSource;
    public AudioSource explosionSource;
    public AudioSource invaderDeathSource;

    [Header("Audio Clips")]
    public AudioClip shootClip;
    public AudioClip explosionClip;
    public AudioClip invaderDeathClip;

    private bool soundOn = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Auto-setup audio sources if not assigned
        if (shootSource == null)
            shootSource = gameObject.AddComponent<AudioSource>();
        if (explosionSource == null)
            explosionSource = gameObject.AddComponent<AudioSource>();
        if (invaderDeathSource == null)
            invaderDeathSource = gameObject.AddComponent<AudioSource>();

        // Auto-load clips from Resources
        if (shootClip == null)
            shootClip = Resources.Load<AudioClip>("laser");
        if (explosionClip == null)
            explosionClip = Resources.Load<AudioClip>("explosion");
        if (invaderDeathClip == null)
            invaderDeathClip = Resources.Load<AudioClip>("invader_death");

        // Set volume levels
        shootSource.volume = 0.5f;
        explosionSource.volume = 0.7f;
        invaderDeathSource.volume = 0.5f;
    }

    public void PlayShoot()
    {
        if (soundOn && shootSource != null && shootClip != null)
        {
            shootSource.PlayOneShot(shootClip);
        }
    }

    public void PlayExplosion()
    {
        if (soundOn && explosionSource != null && explosionClip != null)
        {
            explosionSource.PlayOneShot(explosionClip);
        }
    }

    public void PlayInvaderDeath()
    {
        if (soundOn && invaderDeathSource != null && invaderDeathClip != null)
        {
            invaderDeathSource.PlayOneShot(invaderDeathClip);
        }
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
    }

    public bool IsSoundOn() => soundOn;
}