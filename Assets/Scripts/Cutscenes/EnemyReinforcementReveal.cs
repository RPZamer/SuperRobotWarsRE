using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WEEK 6: Cinematically reveals existing battlefield units.
// Supports both allied and enemy reinforcements.
public class EnemyReinforcementReveal : MonoBehaviour
{
    [Header("WEEK 6: Enemy Reinforcements")]
    [SerializeField] private List<BattleUnit> enemyUnits = new();

    // WEEK 6: Level 2 allied reinforcement units.
    [Header("WEEK 6: Allied Reinforcements")]
    [SerializeField] private List<BattleUnit> alliedUnits = new();

    [Header("WEEK 6: Reveal Settings")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    [SerializeField, Min(0f)] private float delayBetweenEnemies = 0.3f;

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new();

    private bool enemiesHidden;
    private bool alliesHidden;

    private bool enemyRevealStarted;
    private bool allyRevealStarted;

    // WEEK 7: FINAL STAGE REINFORCEMENTS
    // Identify enemies that have not been revealed yet.
    public bool IsWaitingEnemy(BattleUnit unit)
    {
        return unit != null &&
               enemyUnits.Contains(unit) &&
               !enemyRevealStarted;
    }

    // WEEK 7: FINAL STAGE REINFORCEMENTS
    // Check whether the first enemy wave has been defeated.
    // Only enemies assigned to this reinforcement controller
    // are excluded from the first-wave count.
    public bool IsFirstWaveDefeated(IEnumerable<BattleUnit> battlefieldUnits)
    {
        if (enemyRevealStarted || enemyUnits.Count == 0)
        {
            return false;
        }

        foreach (BattleUnit unit in battlefieldUnits)
        {
            if (unit == null ||
                unit.Team != BattleTeam.Enemy ||
                unit.IsDefeated)
            {
                continue;
            }

            // WEEK 7: Ignore Char and the four unrevealed Zakus.
            if (IsWaitingEnemy(unit))
            {
                continue;
            }

            // At least one first-wave enemy is still alive.
            return false;
        }

        // Every first-wave enemy has been defeated.
        return true;
    }

    // WEEK 6: Hide reinforcement sprites before opening dialogue.
    private void Awake()
    {
        originalColors.Clear();

        enemiesHidden = HideUnits(enemyUnits, BattleTeam.Enemy);
        alliesHidden = HideUnits(alliedUnits, BattleTeam.Player);
    }

    // WEEK 6: Hide sprites without disabling battlefield units.
    private bool HideUnits(List<BattleUnit> units, BattleTeam team)
    {
        bool hiddenAny = false;

        foreach (BattleUnit unit in units)
        {
            if (unit == null || unit.Team != team)
            {
                continue;
            }

            SpriteRenderer sprite = unit.GetComponent<SpriteRenderer>();

            if (sprite == null)
            {
                continue;
            }

            originalColors[sprite] = sprite.color;

            Color hiddenColor = sprite.color;
            hiddenColor.a = 0f;

            sprite.color = hiddenColor;

            hiddenAny = true;
        }

        return hiddenAny;
    }

    // WEEK 6: Preserve Level 1's existing enemy reveal method.
    public IEnumerator RevealEnemies()
    {
        if (enemyRevealStarted || !enemiesHidden)
        {
            yield break;
        }

        enemyRevealStarted = true;

        yield return RevealUnits(enemyUnits, BattleTeam.Enemy);

        enemiesHidden = false;
    }

    // WEEK 6: Reveal Level 2 allied reinforcements.
    public IEnumerator RevealAllies()
    {
        if (allyRevealStarted || !alliesHidden)
        {
            yield break;
        }

        allyRevealStarted = true;

        yield return RevealUnits(alliedUnits, BattleTeam.Player);

        alliesHidden = false;
    }

    // WEEK 6: Shared animation for either reinforcement team.
    private IEnumerator RevealUnits(
        List<BattleUnit> units,
        BattleTeam team)
    {
        foreach (BattleUnit unit in units)
        {
            if (unit == null || unit.Team != team)
            {
                continue;
            }

            SpriteRenderer sprite = unit.GetComponent<SpriteRenderer>();

            if (sprite == null ||
                !originalColors.TryGetValue(
                    sprite,
                    out Color originalColor))
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
    }

    // WEEK 6: Restore visibility if the component is disabled.
    private void OnDisable()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry
                 in originalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }
    }
}