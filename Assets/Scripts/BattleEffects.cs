using System.Collections;
using UnityEngine;

public class BattleEffects : MonoBehaviour
{
    [Header("Glow Effect")]
    [SerializeField] private SpriteRenderer unitSprite;
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float effectDuration = 0.5f;

    [Header("Sparkle Effect")]
    [SerializeField] private ParticleSystem sparkleEffect;

    [Header("Death Effect")]
    [SerializeField] private GameObject DeathEffectPrefab;

    private Coroutine effectCoroutine;
    private Color originalColor;

    private void Awake()
    {
        if (unitSprite == null)
            unitSprite = GetComponentInChildren<SpriteRenderer>();

        if (unitSprite != null)
            originalColor = unitSprite.color;
    }

    public void PlaySkillEffect()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        if (unitSprite != null)
            unitSprite.color = originalColor;

        if (sparkleEffect != null)
        {
            sparkleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkleEffect.Play();
        }

        effectCoroutine = StartCoroutine(GlowEffect());
    }

    private IEnumerator GlowEffect()
    {
        if (unitSprite == null)
            yield break;

        float timer = 0f;

        while (timer < effectDuration)
        {
            timer += Time.deltaTime;

            float glow = Mathf.PingPong(timer / effectDuration * 2f, 1f);

            unitSprite.color = Color.Lerp(
                originalColor,
                glowColor,
                glow
            );

            yield return null;
        }

        unitSprite.color = originalColor;
        effectCoroutine = null;
    }

    public void PlayDamageEffect()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(DamageFlash());
    }


    private IEnumerator DamageFlash()
    {
        if (unitSprite == null)
            yield break;

        Color previousColor = unitSprite.color;

        // making it a for loop will allow the flash to play mutliple times, like the damage effect in pokemon.
        for (int i = 0; i < 3; i++)
        {
            unitSprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            unitSprite.color = previousColor;
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
