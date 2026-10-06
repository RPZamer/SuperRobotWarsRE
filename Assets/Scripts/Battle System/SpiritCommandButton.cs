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
        Action<SpiritCommandBase> onClicked,
        string pilotLabel = null)
    {
        if (spirit == null)
        {
            return;
        }

        // WEEK 4: Display both the command name and its SP cost.
        if (commandText != null)
        {
            // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER presents Spirits from three pilots in the existing command list.
            // An optional label identifies the command owner and their remaining SP, and labelled rows grow enough to fit that extra text.
            // Ordinary units omit the label and retain their original button text and size while Getter's crew list can scroll.
            commandText.text =
                (string.IsNullOrEmpty(pilotLabel) ? "" : pilotLabel + "\n") + $"{spirit.CommandName} ({spirit.SpiritPointCost} SP)";
            if (!string.IsNullOrEmpty(pilotLabel) && button != null)
            {
                RectTransform row = button.transform as RectTransform;
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(row.rect.width, 280f));
                commandText.textWrappingMode = TextWrappingModes.Normal;
                float width = Mathf.Max(1f, row.rect.width - commandText.margin.x - commandText.margin.z);
                float height = commandText.GetPreferredValues(commandText.text, width, 0f).y + 8f;
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(row.rect.height, height));
            }
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