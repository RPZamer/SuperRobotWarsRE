using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WEEK 6: Cinematically reveals existing enemy units during cutscenes.
// Enemy objects remain active and registered with the battlefield.
public class EnemyReinforcementReveal : MonoBehaviour
{
    [Header("WEEK 6: Enemy Reinforcements")]
    [SerializeField] private List<BattleUnit> enemyUnits = new();

    [Header("WEEK 6: Reveal Settings")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    [SerializeField, Min(0f)] private float delayBetweenEnemies = 0.3f;

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new();

    private bool enemiesHidden;
    private bool revealStarted;

    // WEEK 6: Hide enemy sprites before the opening dialogue begins.
    private void Awake()
    {
        HideEnemies();
    }

    private void HideEnemies()
    {
        originalColors.Clear();

        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.Team != BattleTeam.Enemy)
            {
                continue;
            }

            SpriteRenderer sprite = enemy.GetComponent<SpriteRenderer>();

            if (sprite == null)
            {
                continue;
            }

            originalColors[sprite] = sprite.color;

            Color hiddenColor = sprite.color;
            hiddenColor.a = 0f;

            sprite.color = hiddenColor;
        }

        enemiesHidden = originalColors.Count > 0;
    }

    // WEEK 6: Reveal enemies one by one without spawning new objects.
    public IEnumerator RevealEnemies()
    {
        if (revealStarted || !enemiesHidden)
        {
            yield break;
        }

        revealStarted = true;

        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.Team != BattleTeam.Enemy)
            {
                continue;
            }

            SpriteRenderer sprite = enemy.GetComponent<SpriteRenderer>();

            if (sprite == null ||
                !originalColors.TryGetValue(sprite, out Color originalColor))
            {
                continue;
            }

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                if (sprite == null)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;

                Color currentColor = originalColor;

                currentColor.a = Mathf.Lerp(
                    0f,
                    originalColor.a,
                    fadeDuration <= 0f
                        ? 1f
                        : elapsed / fadeDuration);

                sprite.color = currentColor;

                yield return null;
            }

            if (sprite != null)
            {
                sprite.color = originalColor;
            }

            if (delayBetweenEnemies > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    delayBetweenEnemies);
            }
        }

        enemiesHidden = false;
    }

    // WEEK 6: Restore enemy visibility if the reveal is interrupted.
    private void OnDisable()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in originalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }
    }
}