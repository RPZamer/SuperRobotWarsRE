using System.Collections;
using UnityEngine;

public class BattleEffects : MonoBehaviour
{
    [Header("Glow Effect")]
    [SerializeField] private SpriteRenderer UnitSprite;
    [SerializeField] private Color GlowColor = Color.yellow;
    [SerializeField] private float EffectDuration = 0.5f;

    [Header("Sparkle Effect")]
    [SerializeField] private ParticleSystem SparkleEffect;

    [Header("Death Effect")]
    [SerializeField] private GameObject DeathEffectPrefab;

    [Header("Audio Effects")]
    [SerializeField] private AudioClip SpiritSound;
    [SerializeField][Range(0f, 1f)] float SpiritVolume = 0.5f;
    [SerializeField] private AudioClip DeathSound;
    [SerializeField][Range(0f, 1f)] float DeathVolume = 0.5f;
    [SerializeField] private AudioClip DamageSound;
    [SerializeField][Range(0f, 1f)] float DamageVolume = 0.5f;

    private AudioSource audioSource;

    private Coroutine effectCoroutine;
    private Color originalColor;


    private void Awake()
    {
        if (UnitSprite == null)
        {
            UnitSprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (UnitSprite != null)
        {
            originalColor = UnitSprite.color;
        }

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlaySkillEffect()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine); 
        }

        if (UnitSprite != null)
        { 
            UnitSprite.color = originalColor;
        }

        if (SparkleEffect != null)
        {
            SparkleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SparkleEffect.Play();
        }

        // play the sound effect with the spirit command/skill
        if (audioSource != null && SpiritSound != null)
        {
            audioSource.PlayOneShot(SpiritSound);
        }

        effectCoroutine = StartCoroutine(GlowEffect());
    }

    private IEnumerator GlowEffect()
    {
        if (UnitSprite == null)
        { 
            yield break; 
        }

        float timer = 0f;

        while (timer < EffectDuration)
        {
            timer += Time.deltaTime;
            float glow = Mathf.PingPong(timer / EffectDuration * 2f, 1f);

            UnitSprite.color = Color.Lerp(originalColor, GlowColor, glow);

            yield return null;
        }

        UnitSprite.color = originalColor;
        effectCoroutine = null;
    }

    public void PlayDamageEffect()
    {
        if (effectCoroutine != null)
        { 
            StopCoroutine(effectCoroutine); 
        }

        if (audioSource != null && DamageSound != null)
        {
            audioSource.PlayOneShot(DamageSound);
        }

        effectCoroutine = StartCoroutine(DamageFlash());
    }


    private IEnumerator DamageFlash()
    {
        if (UnitSprite == null)
        { 
            yield break; 
        }

        Color previousColor = UnitSprite.color;

        // making it a for loop will allow the flash to play mutliple times, like the damage effect in pokemon.
        for (int i = 0; i < 3; i++)
        {
            UnitSprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            UnitSprite.color = previousColor;
            yield return new WaitForSeconds(0.1f);
        }

        effectCoroutine = null;
    }


    public void PlayDeathEffect()
    {
        if (DeathEffectPrefab == null)
        {
            return;
        }

        // Play the explosion sound.
        if (audioSource != null && DeathSound != null)
        {
            audioSource.PlayOneShot(DeathSound);
        }

        GameObject effect = Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);
        effect.SetActive(true);

        Animator animator = effect.GetComponent<Animator>();

        if (animator != null)
        {
            animator.Play(0, 0, 0f);
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

            if (clips.Length > 0)
            {
                Destroy(effect, clips[0].length);
            }
        }
    }
}
