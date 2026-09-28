using UnityEngine;

// WEEK 4: PILOT SKILLS - Shared SRW Y skill rules. Never change the pilot/mech/weapon asset at runtime.

public static class PilotSkillEffects
{
    private static readonly int[] AmmoPercent = { 100, 120, 150 };
    private static readonly int[] EnergyPercent = { 100, 90, 80 };
    private static readonly int[] DamageReduction = { 0, 10, 10, 20, 20, 30, 40, 50, 60, 70 };

    public static int AmmoCapacity(PilotBase pilot, Weapon weapon)
    {
        if (weapon == null || weapon.MaxAmmo <= 0) return 0;
        int percent = AmmoPercent[pilot != null ? pilot.SaveBLevel : 0];
        return (int)System.Math.Min(int.MaxValue, ((long)weapon.MaxAmmo * percent + 99) / 100);
    }

    // WEEK 4: PILOT SKILLS - Round fractional EN costs up; a positive-cost weapon never becomes free.
    public static int EnergyCost(PilotBase pilot, Weapon weapon)
    {
        if (weapon == null || weapon.EnergyCost <= 0) return 0;
        int percent = EnergyPercent[pilot != null ? pilot.SaveELevel : 0];
        return (int)(((long)weapon.EnergyCost * percent + 99) / 100);
    }

    // WEEK 4: PILOT SKILLS - Strict HP boundaries: L9 begins BELOW 90%, L1 BELOW 10%.
    public static int PotentialStage(int level, int currentHP, int maxHP)
    {
        if (level <= 0 || currentHP <= 0 || maxHP <= 0) return 0;
        int hpBand = (int)System.Math.Min(10L, (long)currentHP * 10 / maxHP);
        return Mathf.Clamp(Mathf.Clamp(level, 0, 9) - hpBand, 0, 9);
    }

    public static int HitDodgeBonus(int stage) => Mathf.Clamp(stage, 0, 9) * 5;
    public static int CriticalBonus(int stage) => Mathf.Clamp(stage, 0, 9) * 6;
    public static int DamageReductionPercent(int stage) => DamageReduction[Mathf.Clamp(stage, 0, 9)];

    public static void Log(BattleUnit unit, string message)
    {
        if (unit != null)
            Debug.Log($"[Pilot Skill] {unit.name}: {message}", unit);
    }
}