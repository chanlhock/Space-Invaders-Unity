using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource shootSource;
    public AudioSource explosionSource;
    public AudioSource invaderDeathSource;
    public AudioSource musicBackgroundSource;

    [Header("Audio Clips")]
    public AudioClip shootClip;
    public AudioClip explosionClip;
    public AudioClip invaderDeathClip;
    public AudioClip musicBackgroundClip;

    [Header("Music Settings")]
    public float musicVolume = 0.5f;
    public bool playMusicOnStart = true;

    private bool soundOn = true;
    private bool isMusicPlaying = false;

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
        if (musicBackgroundSource == null)
            musicBackgroundSource = gameObject.AddComponent<AudioSource>();

        musicBackgroundSource.loop = true;
        musicBackgroundSource.volume = musicVolume;

        // Auto-load clips from Resources
        if (shootClip == null)
            shootClip = Resources.Load<AudioClip>("bullet");
        if (explosionClip == null)
            explosionClip = Resources.Load<AudioClip>("explosion");
        if (invaderDeathClip == null)
            invaderDeathClip = Resources.Load<AudioClip>("invader_dies");
        if (musicBackgroundClip == null)
            musicBackgroundClip = Resources.Load<AudioClip>("music");

        // Set volume levels
        shootSource.volume = 0.5f;
        explosionSource.volume = 0.7f;
        invaderDeathSource.volume = 0.5f;
        
        // Start playing music if enabled
        if (playMusicOnStart && musicBackgroundClip != null)
        {
            PlayMusic();
        }
        else if (musicBackgroundClip == null)
        {
            Debug.LogWarning("⚠️ No music clip found! Make sure music.mp3 is in the Sounds folder.");
        }
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
    // NEW: Play background music
    public void PlayMusic()
    {
        if (musicBackgroundSource != null && musicBackgroundClip != null && soundOn)
        {
            if (!musicBackgroundSource.isPlaying)
            {
                musicBackgroundSource.clip = musicBackgroundClip;
                musicBackgroundSource.Play();
                isMusicPlaying = true;
                Debug.Log("🎵 Background music started");
            }
        }
        else if (musicBackgroundClip == null)
        {
            Debug.LogWarning("⚠️ Cannot play music - music clip is null!");
        }
    }

    // NEW: Stop background music
    public void StopMusic()
    {
        if (musicBackgroundSource != null && musicBackgroundSource.isPlaying)
        {
            musicBackgroundSource.Stop();
            isMusicPlaying = false;
            Debug.Log("🎵 Background music stopped");
        }
    }

    // NEW: Pause background music
    public void PauseMusic()
    {
        if (musicBackgroundSource != null && musicBackgroundSource.isPlaying)
        {
            musicBackgroundSource.Pause();
            isMusicPlaying = false;
        }
    }

    // NEW: Resume background music
    public void ResumeMusic()
    {
        if (musicBackgroundSource != null && soundOn && musicBackgroundClip != null)
        {
            musicBackgroundSource.UnPause();
            isMusicPlaying = true;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicBackgroundSource != null)
        {
            musicBackgroundSource.volume = musicVolume;
        }
    }

    public void ToggleMusic()
    {
        if (musicBackgroundSource != null)
        {
            if (musicBackgroundSource.isPlaying)
            {
                PauseMusic();
            }
            else
            {
                ResumeMusic();
            }
        }
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        if (!soundOn && musicBackgroundSource != null)
        {
            musicBackgroundSource.Pause();
        }
        else if (soundOn && musicBackgroundSource != null && musicBackgroundClip != null)
        {
            musicBackgroundSource.UnPause();
        }
    }

    public bool IsSoundOn() => soundOn;
    public bool IsMusicPlaying() => isMusicPlaying;
}