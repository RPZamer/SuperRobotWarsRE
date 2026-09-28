using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// WEEK 4: MOTHERSHIP - Assign a Button and TMP label on the hangar's passenger-row template.
public class PassengerRowView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    public bool IsConfigured => button != null && label != null;

    public void SetPassenger(BattleUnit passenger, bool canDeploy, Action deploy)
    {
        label.text = $"{passenger.Mech.MechName}  HP {passenger.CurrentHealth}/{passenger.Mech.Health}  " +
            $"EN {passenger.CurrentEnergy}/{passenger.Mech.Energy}" +
            (passenger.HasActed ? "  (Ready next phase)" : canDeploy ? "  Deploy" : "  (Exit blocked)");
        button.interactable = canDeploy;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => deploy?.Invoke());
    }
}
