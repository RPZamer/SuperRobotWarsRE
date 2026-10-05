using UnityEngine;

public class BattleMusic : MonoBehaviour
{
    // WEEK 5: MUSIC SYSTEM - AudioSource used for all battlefield music.
    [SerializeField] private AudioSource musicSource;
    [SerializeField][Range(0f, 1f)] float musicVolume = 0.5f;

    // WEEK 5: MUSIC SYSTEM - Default music that plays while units are on the overworld.
    [SerializeField] private AudioClip overworldMusic;

    // WEEK 5: MUSIC SYSTEM - Optional themes for allied reinforcements.
    // Each stage can choose either the main or alternate reinforcement theme.
    [SerializeField] private AudioClip allyReinforcementMusic;
    [SerializeField] private AudioClip allyReinforcementMusicAlt;

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

    private void Start()
    {
        // WEEK 5: MUSIC SYSTEM - Begin the battle with the passive overworld theme.

        PlayOverworldMusic();
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

    // WEEK 5: MUSIC SYSTEM - Play the intermission theme when that scene is implemented.
    public void PlayIntermissionMusic()
    {
        if (musicSource == null || intermissionMusic == null)
        {
            return;
        }

        musicSource.clip = intermissionMusic;
        musicSource.loop = true;
        musicSource.Play();
    }
}