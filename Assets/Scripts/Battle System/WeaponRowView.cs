using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// WEEK 3: Put this on your weapon-row template and assign each TMP field yourself.
[DisallowMultipleComponent]
public class WeaponRowView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text damageTypeText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text energyCostText;
    [SerializeField] private TMP_Text availabilityText;
    [SerializeField] private Image selectionGraphic;

    public Weapon Weapon { get; private set; }

    // WEEK 3: Fill separate fields without changing their positions, fonts, or layout.
    public void SetWeapon(Weapon weapon, bool affordable, bool canTarget, Action clicked)
    {
        Weapon = weapon;
        weaponNameText.text = weapon.WeaponName;
        damageTypeText.text = weapon.DamageType.ToString();
        powerText.text = weapon.Power.ToString();
        rangeText.text = $"{weapon.MinRange}-{weapon.MaxRange}";
        energyCostText.text = weapon.EnergyCost.ToString();
        availabilityText.text = affordable ? (canTarget ? string.Empty : "No target") : "Not enough EN";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clicked?.Invoke());
    }

    // WEEK 4: MOTHERSHIP - Show per-unit ammo and distinguish empty magazines from insufficient EN.
    // WEEK 6 CHANGES PLEASE READ: Counter weapons ignore previous movement, but the row previously showed the normal movement warning.
    // This optional flag suppresses that warning only for the defensive picker and identifies weapons that cannot counter.
    // Normal callers retain the existing ammo, EN, morale and mode messages through the default false value.
    public void ShowAmmo(BattleUnit unit, Weapon weapon, bool counterattack = false)
    {
        // WEEK 4: PILOT SKILLS - Replace the base cost with the same effective cost used when firing.
        energyCostText.text = unit.GetWeaponEnergyCost(weapon).ToString();
        // WEEK 5 CHANGES PLEASE READ: Each weapon row previously showed only base power and resource availability.
        // GUND changes effective weapon power, and Hyper/Super-only moves need a clear locked-mode explanation.
        // Updating the existing fields keeps the list consistent with combat without adding controls or changing its layout.
        powerText.text = unit.GetWeaponPower(weapon).ToString();
        if (!MechSkillEffect.IsWeaponUnlocked(unit, weapon)) availabilityText.text = $"Requires {weapon.RequiredMode} Mode";
        // WEEK 5 CHANGES PLEASE READ: A weapon blocked by morale would otherwise be labelled as lacking EN in this row.
        // Showing its configured minimum explains why the attack is unavailable even when the unit has enough resources.
        // This uses the existing availability text so the weapon menu needs no new controls or layout changes.
        if (unit.CurrentMorale < weapon.RequiredMorale) availabilityText.text = $"Requires {weapon.RequiredMorale} morale";
        // WEEK 5 FIXES: Originally, weapon rows reported targets and resources without explaining a movement restriction.
        // Restoring the PostMovement requirement means an affordable weapon can now be unavailable simply because its unit moved.
        // This message explains that case in the existing row while retaining the separate EN and ammo messages.
        if (!counterattack && unit.CanAffordWeapon(weapon) && unit.HasMoved && !weapon.CanUseAfterMoving)
            availabilityText.text = "Cannot use after moving";
        if (counterattack && weapon.Classification != WeaponClassification.SingleTarget)
            availabilityText.text = "Cannot counter";
        if (weapon.MaxAmmo > 0)
        {
            energyCostText.text += $" | Ammo {unit.GetAmmo(weapon)}/{unit.GetAmmoCapacity(weapon)}";
            if (unit.GetAmmo(weapon) <= 0) availabilityText.text = "No ammo";
        }
    }

    // WEEK 6 CHANGES PLEASE READ: Unusable counter rows must not allow selection through their existing click listener.
    // The menu passes the same exact-target eligibility check that combat uses for the pending retaliation.
    // This method changes only defensive rows, preserving the ordinary weapon list's current selection behavior.
    public void SetCounterAvailability(bool available) => button.interactable = available;

    // WEEK 3: Change only the assigned selection graphic when this weapon is highlighted.
    public void SetSelected(bool isSelected, Color normalColor, Color selectedColor, float blinkSeconds)
    {
        if (selectionGraphic == null) return;
        selectionGraphic.color = isSelected
            ? Color.Lerp(normalColor, selectedColor, Mathf.PingPong(Time.unscaledTime / blinkSeconds, 1f))
            : normalColor;
    }
    // WEEK 3: Assign the row template fields once so each field stays movable in the Hierarchy.
    public void Configure(
        Button assignedButton,
        TMP_Text assignedWeaponName,
        TMP_Text assignedDamageType,
        TMP_Text assignedPower,
        TMP_Text assignedRange,
        TMP_Text assignedEnergyCost,
        TMP_Text assignedAvailability,
        Image assignedSelectionGraphic)
    {
        button = assignedButton;
        weaponNameText = assignedWeaponName;
        damageTypeText = assignedDamageType;
        powerText = assignedPower;
        rangeText = assignedRange;
        energyCostText = assignedEnergyCost;
        availabilityText = assignedAvailability;
        selectionGraphic = assignedSelectionGraphic;
    }
}