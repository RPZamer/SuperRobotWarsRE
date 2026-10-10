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
    BattleUnit target = null,
    PilotBase spiritPilot = null)
    {
        // WEEK 4: A defeated or missing unit cannot use a Spirit Command.
        if (caster == null || caster.IsDefeated || spirit == null)
        {
            return false;
        }

        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER sub-pilots can cast Spirits while a different pilot controls combat.
        // The optional caster pilot defaults to the main pilot and must belong to this unit's crew; sub-pilots may only use their own assigned commands.
        // Affordability and successful SP charging now use that pilot's independent pool without changing the mech's main pilot or existing Spirit effects.
        spiritPilot ??= caster.Pilot;
        if (!caster.HasSpiritPilot(spiritPilot)) return false;
        if (spiritPilot != caster.Pilot && System.Array.IndexOf(spiritPilot.GetSpiritCommands(), spirit) < 0) return false;

        // WEEK 4: Do not allow the Spirit to activate when the pilot
        // cannot afford its configured Spirit Point cost.
        if (spirit.SpiritPointCost > caster.GetSpiritPoints(spiritPilot))
        {
            Debug.Log(
                $"{spiritPilot.PilotName} does not have enough SP to use {spirit.CommandName}.");

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

            // WEEK 6: Strike guarantees the caster's next attack will hit.
            case SpiritCommandEffect.Strike:
                caster.ActivateStrike();
                activated = true;
                break;

            // WEEK 6: Alert guarantees evasion against the next incoming attack.
            case SpiritCommandEffect.Alert:
                caster.ActivateAlert();
                activated = true;
                break;

            // WEEK 6: Persist prevents the caster from being defeated by one attack.
            case SpiritCommandEffect.Persist:
                caster.ActivatePersist();
                activated = true;
                break;

            // WEEK 6: Focus improves the caster's accuracy and evasion.
            case SpiritCommandEffect.Focus:
                caster.ActivateFocus();
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

            // WEEK 6: Vigor restores 30% of the caster's maximum HP.
            case SpiritCommandEffect.Vigor:
                if (caster.Mech == null)
                {
                    break;
                }

                int vigorHealing =
                    Mathf.RoundToInt(caster.Mech.Health * 0.30f);

                caster.RestoreHealth(vigorHealing);
                activated = true;
                break;

            // WEEK 6: Guts completely restores the caster's HP.
            case SpiritCommandEffect.Guts:
                if (caster.Mech == null)
                {
                    break;
                }

                caster.RestoreHealth(caster.Mech.Health);
                activated = true;
                break;

            // WEEK 6: Spirit raises the caster's Morale by 10.
            case SpiritCommandEffect.Spirit:
                caster.AddMorale(10);
                activated = true;
                break;

            // WEEK 6: Drive raises the caster's Morale by 30.
            case SpiritCommandEffect.Drive:
                caster.AddMorale(30);
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

            // WEEK 6: Faith completely restores one living allied unit's HP.
            case SpiritCommandEffect.Faith:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team != caster.Team ||
                    target.Mech == null)
                {
                    Debug.LogWarning("Faith requires a living allied target.");
                    break;
                }

                target.RestoreHealth(target.Mech.Health);
                activated = true;
                break;

            // WEEK 6: Attune raises one living allied unit's Morale by 10.
            case SpiritCommandEffect.Attune:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team != caster.Team)
                {
                    Debug.LogWarning("Attune requires a living allied target.");
                    break;
                }

                target.AddMorale(10);
                activated = true;
                break;

            // WEEK 6: Hope restores 50 Spirit Points to one living allied unit.
            case SpiritCommandEffect.Hope:
                if (target == null ||
                    target.IsDefeated ||
                    target.Team != caster.Team)
                {
                    Debug.LogWarning("Hope requires a living allied target.");
                    break;
                }

                target.RestoreSpiritPoints(50);
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

            // WEEK 5 CHANGES PLEASE READ: Spirit execution previously had no effect that restored EN/ammo/shield together.
            // Resupply now delegates that restoration to MechSkillEffect after validating a living allied target.
            // SP is still charged by the existing successful-activation path, and the device's action/morale penalty is not applied to this Spirit.
            case SpiritCommandEffect.Resupply:
                if (target == null || target.IsDefeated || target.Team != caster.Team || target.Mech == null || target.IsCombinedComponent)
                    break;
                MechSkillEffect.RestoreSupplies(target);
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
        if (!caster.TrySpendSpiritPoints(spirit.SpiritPointCost, spiritPilot))
        {
            return false;
        }

        Debug.Log(
            $"{spiritPilot.PilotName} used {spirit.CommandName}. " +
            $"SP remaining: {caster.GetSpiritPoints(spiritPilot)}");

        return true;
    }
}