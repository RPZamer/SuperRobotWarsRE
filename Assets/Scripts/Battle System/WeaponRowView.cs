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