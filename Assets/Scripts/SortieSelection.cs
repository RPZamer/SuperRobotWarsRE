using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SortieSelection : MonoBehaviour
{
    [Header("Sortie Toggles")]
    public Toggle kojiToggle;
    public Toggle tetsuyaToggle;
    public Toggle amuroToggle;

    [Header("Sortie Information")]
    [SerializeField] private TMP_Text sortieInfoText;
    
    [Header("Sortie Limits")]
    [SerializeField] private int maxSortieUnits = 5;

    private void Start()
    {
        if(SortieData.HasCustomSelection)
        {
            kojiToggle.isOn = SortieData.KojiSelected;
            tetsuyaToggle.isOn = SortieData.TetsuyaSelected;
            amuroToggle.isOn = SortieData.AmuroSelected;
        }
        else
        {
            // Default selection if no custom selection has been made
            kojiToggle.isOn = true;
            tetsuyaToggle.isOn = true;
            amuroToggle.isOn = true;
        }

        kojiToggle.onValueChanged.AddListener(OnSelectionChanged);
        tetsuyaToggle.onValueChanged.AddListener(OnSelectionChanged);
        amuroToggle.onValueChanged.AddListener(OnSelectionChanged);

        UpdateSortieInfo();
    }

    private void OnSelectionChanged(bool isOn)
    {
        UpdateSortieInfo();
    }

    private void UpdateSortieInfo()
    {
        int selectedCount = 0;

        if (kojiToggle.isOn) selectedCount++;
        if (tetsuyaToggle.isOn) selectedCount++;
        if (amuroToggle.isOn) selectedCount++;

        if (sortieInfoText != null)
        {
            sortieInfoText.text = "Selected Units: " + selectedCount + " / " + maxSortieUnits;
        }
        Debug.Log("Sortie Units selected: " + selectedCount);
    }

    public void ConfirmSortie()
    {
        int selectedCount = 0;
        if (kojiToggle.isOn) selectedCount++;
        if (tetsuyaToggle.isOn) selectedCount++;
        if (amuroToggle.isOn) selectedCount++;
        if (selectedCount == 0)
        {
            Debug.LogWarning("Select at least one unit before confirming.");
            return;
        }

        if(selectedCount > maxSortieUnits)
        {
            Debug.LogWarning("You can only select up to " + maxSortieUnits + " units.");
            return;
        }

        SortieData.KojiSelected = kojiToggle.isOn;
        SortieData.TetsuyaSelected = tetsuyaToggle.isOn;
        SortieData.AmuroSelected = amuroToggle.isOn;
        SortieData.HasCustomSelection = true;

        Debug.Log("Sortie confirmed with " + selectedCount + " units.");      
    }

    private void OnDestroy()
    {
        if (kojiToggle != null)
            kojiToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (tetsuyaToggle != null)
            tetsuyaToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (amuroToggle != null)
            amuroToggle.onValueChanged.RemoveListener(OnSelectionChanged);
    }

}
