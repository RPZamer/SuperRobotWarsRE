using UnityEngine;

// WEEK 4: Handles activation of Spirit Commands.
// Keeping Spirit execution here prevents BattleSystem and BattleUnit
// from needing separate public methods for every Spirit Command.
public static class SpiritSystem
{
    public static bool UseSpirit(
    BattleUnit caster,
    SpiritCommandBase spirit,
    Battlefield battlefield = null,
    BattleUnit target = null)
    {
        // WEEK 4: A defeated or missing unit cannot use a Spirit Command.
        if (caster == null || caster.IsDefeated || spirit == null)
        {
            return false;
        }

        // WEEK 4: Do not allow the Spirit to activate when the pilot
        // cannot afford its configured Spirit Point cost.
        if (spirit.SpiritPointCost > caster.CurrentSpiritPoints)
        {
            Debug.Log(
                $"{caster.Pilot?.PilotName} does not have enough SP to use {spirit.CommandName}.");

            return false;
        }

        // WEEK 4: Apply the Spirit effect first. SP is only charged
        // after an effect successfully activates.
        bool activated = false;

        switch (spirit.Effect)
        {
            case SpiritCommandEffect.Valor:
                caster.ActivateValor();
                activated = true;
                break;

            // WEEK 4: Soul increases the caster's next attack damage by 2.5X.
            case SpiritCommandEffect.Soul:
                caster.ActivateSoul();
                activated = true;
                break;

            // WEEK 4: Smash guarantees a critical hit on the caster's next attack.
            case SpiritCommandEffect.Smash:
                caster.ActivateSmash();
                activated = true;
                break;

            // WEEK 4: Accel adds 3 spaces to the caster's next movement.
            case SpiritCommandEffect.Accel:
                caster.ActivateAccel();
                activated = true;
                break;

            // WEEK 4: Bonds restores 50% of maximum HP to every living ally.
            case SpiritCommandEffect.Bonds:
                if (battlefield == null)
                {
                    Debug.LogWarning("Bonds requires a Battlefield.");
                    break;
                }

                foreach (BattleUnit ally in battlefield.Units)
                {
                    if (ally != null &&
                        !ally.IsDefeated &&
                        ally.Team == caster.Team &&
                        ally.Mech != null)
                    {
                        int healing = Mathf.RoundToInt(ally.Mech.Health * 0.5f);
                        ally.RestoreHealth(healing);
                    }
                }

                activated = true;
                break;

            // WEEK 4: Rally gives +5 Morale to every living ally.
            case SpiritCommandEffect.Rally:
                if (battlefield == null)
                {
                    Debug.LogWarning("Rally requires a Battlefield.");
                    break;
                }

                foreach (BattleUnit ally in battlefield.Units)
                {
                    if (ally != null &&
                        !ally.IsDefeated &&
                        ally.Team == caster.Team)
                    {
                        ally.AddMorale(5);
                    }
                }

                activated = true;
                break;

            // WEEK 4: Daunt reduces one enemy's Morale by 10.
            case SpiritCommandEffect.Daunt:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team == caster.Team)
                {
                    Debug.LogWarning("Daunt requires a living enemy target.");
                    break;
                }

                target.ReduceMorale(10);
                activated = true;
                break;

            // WEEK 4: Dread reduces the Morale of every living enemy by 5.
            case SpiritCommandEffect.Dread:
                if (battlefield == null)
                {
                    Debug.LogWarning("Dread requires a Battlefield.");
                    break;
                }

                foreach (BattleUnit enemy in battlefield.Units)
                {
                    if (enemy != null &&
                        !enemy.IsDefeated &&
                        enemy.Team != caster.Team)
                    {
                        enemy.ReduceMorale(5);
                    }
                }

                activated = true;
                break;

            // WEEK 4: Confuse removes 30 Spirit Points from one enemy.
            case SpiritCommandEffect.Confuse:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team == caster.Team)
                {
                    Debug.LogWarning("Confuse requires a living enemy target.");
                    break;
                }

                target.ReduceSpiritPoints(30);
                activated = true;
                break;

            // WEEK 4: Trust restores 30% of maximum HP to one living ally.
            case SpiritCommandEffect.Trust:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team != caster.Team ||
                    target.Mech == null)
                {
                    Debug.LogWarning("Trust requires a living allied target.");
                    break;
                }

                int trustHealing =
                    Mathf.RoundToInt(target.Mech.Health * 0.30f);

                target.RestoreHealth(trustHealing);
                activated = true;
                break;

            // WEEK 4: Prospect restores 30 Spirit Points to one living ally.
            case SpiritCommandEffect.Prospect:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team != caster.Team)
                {
                    Debug.LogWarning("Prospect requires a living allied target.");
                    break;
                }

                target.RestoreSpiritPoints(30);
                activated = true;
                break;

            default:
                Debug.LogWarning(
                    $"Spirit Command '{spirit.CommandName}' does not have an implemented effect yet.");
                break;
        }

        if (!activated)
        {
            return false;
        }

        // WEEK 4: Spend the Spirit Command's configured cost only
        // after the effect has successfully activated.
        if (!caster.TrySpendSpiritPoints(spirit.SpiritPointCost))
        {
            return false;
        }

        Debug.Log(
            $"{caster.Pilot?.PilotName} used {spirit.CommandName}. " +
            $"SP remaining: {caster.CurrentSpiritPoints}");

        return true;
    }
}
