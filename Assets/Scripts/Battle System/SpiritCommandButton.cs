using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// WEEK 4: Controls one generated button in the Spirit Command menu.
// The ActionsMenu supplies the Spirit Command and what should happen when clicked.
public class SpiritCommandButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text commandText;

    // WEEK 4: Configure this reusable button for one Spirit Command.
    public void Setup(
        SpiritCommandBase spirit,
        bool canAfford,
        Action<SpiritCommandBase> onClicked)
    {
        if (spirit == null)
        {
            return;
        }

        // WEEK 4: Display both the command name and its SP cost.
        if (commandText != null)
        {
            commandText.text =
                $"{spirit.CommandName} ({spirit.SpiritPointCost} SP)";
        }

        if (button != null)
        {
            button.interactable = canAfford;

            // WEEK 4: Clear old runtime listeners before this
            // reusable button receives its new Spirit Command.
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => onClicked?.Invoke(spirit));
        }
    }
}