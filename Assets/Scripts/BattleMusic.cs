using System.Collections;
using UnityEngine;

public class BattleMusic : MonoBehaviour
{
    // WEEK 5: MUSIC SYSTEM - AudioSource used for all battlefield music.
    [SerializeField] private AudioSource musicSource;
    [SerializeField][Range(0f, 1f)] float musicVolume = 0.5f;

    // WEEK 6: MUSIC SYSTEM - Controls the transition back to overworld music.
    [SerializeField, Min(0f)] private float musicFadeDuration = 1.5f;

    private Coroutine musicFadeCoroutine;

    // WEEK 5: MUSIC SYSTEM - Default music that plays while units are on the overworld.
    [SerializeField] private AudioClip overworldMusic;

    // WEEK 5: MUSIC SYSTEM - Optional themes for allied reinforcements.
    // Each stage can choose either the main or alternate reinforcement theme.
    [SerializeField] private AudioClip allyReinforcementMusic;
    [SerializeField] private AudioClip allyReinforcementMusicAlt;

    // WEEK 6: MUSIC SYSTEM - Music played when enemy reinforcements arrive.
    [SerializeField] private AudioClip enemyReinforcementMusic;

    // WEEK 5: MUSIC SYSTEM - Optional music prepared for future scenes.
    [SerializeField] private AudioClip splashScreenMusic;
    [SerializeField] private AudioClip intermissionMusic;

    private void Awake()
    {
        // WEEK 5: MUSIC SYSTEM - Automatically find the AudioSource
        // on this object if one was not assigned manually.
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }
    }

    // WEEK 6: MUSIC SYSTEM - Choose which music starts automatically
    // based on the scene's purpose.
    public enum StartingMusic
    {
        Overworld,
        Intermission,
        None
    }

    // WEEK 6: MUSIC SYSTEM - Each scene can select its starting music
    // directly from the Unity Inspector.
    [SerializeField] private StartingMusic startingMusic = StartingMusic.Overworld;

    private void Start()
    {
        // WEEK 6: MUSIC SYSTEM - Start the music assigned to this scene.
        switch (startingMusic)
        {
            case StartingMusic.Overworld:
                PlayOverworldMusic();
                break;

            case StartingMusic.Intermission:
                PlayIntermissionMusic();
                break;

            case StartingMusic.None:
                break;
        }
    }

    public void PlayOverworldMusic()
    {
        if (musicSource == null || overworldMusic == null)
        {
            return;
        }

        // Do not restart the song if it is already playing.
        if (musicSource.clip == overworldMusic && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = overworldMusic;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // WEEK 6: MUSIC SYSTEM - Smoothly return to overworld music
    // after the opening cutscene finishes.
    public void FadeToOverworldMusic()
    {
        if (musicSource == null || overworldMusic == null)
        {
            return;
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeOverworldCoroutine());
    }

    // WEEK 6: MUSIC SYSTEM - Fade out the current track,
    // switch to overworld music, then fade back in.
    private IEnumerator FadeOverworldCoroutine()
    {
        float halfDuration = musicFadeDuration / 2f;
        float startingVolume = musicSource.volume;

        // WEEK 6: Fade out the reinforcement music.
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            musicSource.volume = Mathf.Lerp(
                startingVolume,
                0f,
                elapsed / halfDuration);

            yield return null;
        }

        musicSource.volume = 0f;

        // WEEK 6: Switch tracks while the volume is silent.
        musicSource.clip = overworldMusic;
        musicSource.loop = true;
        musicSource.Play();

        // WEEK 6: Fade in the overworld music.
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            musicSource.volume = Mathf.Lerp(
                0f,
                musicVolume,
                elapsed / halfDuration);

            yield return null;
        }

        musicSource.volume = musicVolume;
        musicFadeCoroutine = null;
    }

    public void PlayMechTheme(AudioClip mechTheme)
    {
        if (musicSource == null || mechTheme == null)
        {
            return;
        }

        // WEEK 5: MUSIC SYSTEM - Keep the current theme playing
        // instead of restarting it every time the same mech attacks.
        if (musicSource.clip == mechTheme && musicSource.isPlaying)
        {
            return;
        }

        // WEEK 5: MUSIC SYSTEM - Replace the overworld music with
        // the attacking mech's assigned theme.
        musicSource.clip = mechTheme;
        musicSource.loop = true;
        musicSource.Play();
    }

    // WEEK 5: MUSIC SYSTEM - Play the reinforcement theme selected by the stage.
    public void PlayAllyReinforcementMusic(bool useAlternate = false)
    {
        if (musicSource == null)
        {
            return;
        }

        AudioClip reinforcementTheme = useAlternate
            ? allyReinforcementMusicAlt
            : allyReinforcementMusic;

        if (reinforcementTheme == null)
        {
            return;
        }

        musicSource.clip = reinforcementTheme;
        musicSource.loop = true;
        musicSource.Play();
    }

    // WEEK 6: MUSIC SYSTEM - Switch to enemy reinforcement music
    // when enemy units are revealed during a cutscene.
    public void PlayEnemyReinforcementMusic()
    {
        if (musicSource == null || enemyReinforcementMusic == null)
        {
            Debug.LogWarning(
                "WEEK 6: Enemy reinforcement music is not configured.",
                this);

            return;
        }

        // WEEK 6: Avoid restarting the same reinforcement track.
        if (musicSource.clip == enemyReinforcementMusic &&
            musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = enemyReinforcementMusic;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // WEEK 5: MUSIC SYSTEM - Play the splash screen theme when that scene is implemented.
    public void PlaySplashScreenMusic()
    {
        if (musicSource == null || splashScreenMusic == null)
        {
            return;
        }

        musicSource.clip = splashScreenMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    // WEEK 6: MUSIC SYSTEM - Play the Intermission theme
    // using the volume configured in the Unity Inspector.
    public void PlayIntermissionMusic()
    {
        if (musicSource == null || intermissionMusic == null)
        {
            return;
        }

        musicSource.clip = intermissionMusic;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }
}