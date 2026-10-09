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
        // WEEK 5 FIXES: Originally, an acted passenger's row said Ready next phase because it could not deploy after boarding.
        // Since passengers can now leave with their action already spent, the label must distinguish deployment from readiness to act.
        // Deploy (action spent) explains that leaving does not restore the passenger's action, while Exit blocked still identifies unavailable exits.
        label.text = $"{passenger.Mech.MechName}  HP {passenger.CurrentHealth}/{passenger.Mech.Health}  " +
            $"EN {passenger.CurrentEnergy}/{passenger.Mech.Energy}" +
            (canDeploy ? passenger.HasActed ? "  Deploy (action spent)" : "  Deploy" : "  (Exit blocked)");
        button.interactable = canDeploy;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => deploy?.Invoke());
    }
}