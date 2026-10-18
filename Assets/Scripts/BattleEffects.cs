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
}
