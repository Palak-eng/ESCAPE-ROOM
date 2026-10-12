using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Playlist")]
    [SerializeField] private AudioClip[] songs;
    [SerializeField] private bool shuffle = true;
    [SerializeField] private bool playOnStart = true;

    [Header("Audio")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
    [SerializeField] private float crossfadeTime = 2f;

    private AudioSource[] sources = new AudioSource[2]; // two sources for crossfading
    private int activeSource = 0;
    private int currentIndex = -1;
    private Coroutine playlistRoutine;

    void Awake()
    {
        // Singleton: keep one BGM manager across scenes
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < 2; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].loop = false;
            sources[i].playOnAwake = false;
            sources[i].volume = 0f;
        }
    }

    void Start()
    {
        if (playOnStart && songs.Length > 0) StartPlaylist();
    }

    public void StartPlaylist()
    {
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        playlistRoutine = StartCoroutine(PlaylistLoop());
    }

    public void StopPlaylist(float fadeOut = 1.5f)
    {
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        StartCoroutine(FadeOut(sources[activeSource], fadeOut));
    }

    public void SkipTrack()
    {
        StartPlaylist(); // restarts loop, which picks the next song
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        sources[activeSource].volume = volume;
    }

    // Play one specific song (e.g. boss fight), then resume playlist if you want
    public void PlaySpecific(AudioClip clip)
    {
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        StartCoroutine(Crossfade(clip));
    }

    private IEnumerator PlaylistLoop()
    {
        while (true)
        {
            AudioClip next = PickNext();
            if (next == null) yield break;

            yield return StartCoroutine(Crossfade(next));

            // Wait until the song is almost over, then loop to next
            float waitTime = Mathf.Max(0.1f, next.length - crossfadeTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private AudioClip PickNext()
    {
        if (songs == null || songs.Length == 0) return null;

        if (shuffle && songs.Length > 1)
        {
            int idx;
            do { idx = Random.Range(0, songs.Length); } while (idx == currentIndex);
            currentIndex = idx;
        }
        else
        {
            currentIndex = (currentIndex + 1) % songs.Length;
        }
        return songs[currentIndex];
    }

    private IEnumerator Crossfade(AudioClip newClip)
    {
        AudioSource from = sources[activeSource];
        activeSource = 1 - activeSource;
        AudioSource to = sources[activeSource];

        to.clip = newClip;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        float startVol = from.volume;
        while (t < crossfadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / crossfadeTime;
            to.volume = Mathf.Lerp(0f, volume, k);
            from.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }
        from.Stop();
        to.volume = volume;
    }

    private IEnumerator FadeOut(AudioSource src, float time)
    {
        float start = src.volume, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }
        src.Stop();
    }
}