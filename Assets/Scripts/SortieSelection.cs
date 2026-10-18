using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SortieSelection : MonoBehaviour
{
    [Header("Sortie Toggles")]
    public Toggle kojiToggle;
    public Toggle tetsuyaToggle;
    public Toggle amuroToggle;
    public Toggle ryomaToggle; 
    public Toggle sayakaToggle; 
    public Toggle junToggle; 
    public Toggle bossToggle; 
    public Toggle michiruToggle;

    [Header("Sortie Information")]
    [SerializeField] private TMP_Text sortieInfoText;
    
    [Header("Sortie Limits")]
    [SerializeField] private int maxSortieUnits = 5;

    [Header("Campaign Sortie Limits")]
    [SerializeField] private int[] campaignSortieLimits = { 3, 3, 5, 6, 7 };

    [SerializeField]
    private string[] campaignSceneNames =
    {
        "Level 1 - Strange Robot Gathering 1",
        "Level 2 - Launch to space, Super Robot Army 1",
        "Level 3 - Space Battle, Keep strong",
        "Level 4 - Moon battle before decisive conflict",
        "Final Stage - Char Vs Super Robot Army"
    };

    private void UpdateMissionSortieLimit()
    {
        string nextScene = PlayerPrefs.GetString("NextScene", campaignSceneNames[0]);

        for (int i = 0; i < campaignSceneNames.Length; i++)
        {
            if (campaignSceneNames[i] == nextScene)
            {
                if (i < campaignSortieLimits.Length)
                {
                    maxSortieUnits = campaignSortieLimits[i];

                    Debug.Log("Mission: " + nextScene + " | Max sortie units: " + maxSortieUnits);
                }
                return;
            }
        }
        Debug.LogWarning("No sortie limit configured for upcoming mission: " + nextScene);
    }

    private void Start()
    {
        UpdateMissionSortieLimit();
        if (SortieData.HasCustomSelection)
        {
            kojiToggle.isOn = SortieData.KojiSelected;
            tetsuyaToggle.isOn = SortieData.TetsuyaSelected;
            amuroToggle.isOn = SortieData.AmuroSelected;

            ryomaToggle.isOn = SortieData.RyomaSelected;
            sayakaToggle.isOn = SortieData.SayakaSelected; 
            junToggle.isOn = SortieData.JunSelected; 
            bossToggle.isOn = SortieData.BossSelected; 
            michiruToggle.isOn = SortieData.MichiruSelected;

            
        }
        else
        {
            // Default selection if no custom selection has been made
            kojiToggle.isOn = true;
            tetsuyaToggle.isOn = true;
            amuroToggle.isOn = true;
            ryomaToggle.isOn = false;
            sayakaToggle.isOn = false;
            junToggle.isOn = false;
            bossToggle.isOn = false;
            michiruToggle.isOn = false;
        }

        kojiToggle.onValueChanged.AddListener(OnSelectionChanged);
        tetsuyaToggle.onValueChanged.AddListener(OnSelectionChanged);
        amuroToggle.onValueChanged.AddListener(OnSelectionChanged);
        ryomaToggle.onValueChanged.AddListener(OnSelectionChanged);
        sayakaToggle.onValueChanged.AddListener(OnSelectionChanged);
        junToggle.onValueChanged.AddListener(OnSelectionChanged);
        bossToggle.onValueChanged.AddListener(OnSelectionChanged);
        michiruToggle.onValueChanged.AddListener(OnSelectionChanged);

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
        if (ryomaToggle.isOn) selectedCount++;
        if (sayakaToggle.isOn) selectedCount++;
        if (junToggle.isOn) selectedCount++;
        if (bossToggle.isOn) selectedCount++;
        if (michiruToggle.isOn) selectedCount++;

        if (sortieInfoText != null)
        {
            if (selectedCount > maxSortieUnits)
            {
                sortieInfoText.text =
                    "Selected Units: " + selectedCount + " / " + maxSortieUnits +
                    "\nToo many mechs! Maximum allowed: " + maxSortieUnits;

                sortieInfoText.color = Color.red;
            }
            else
            {
                sortieInfoText.text =
                    "Selected Units: " + selectedCount + " / " + maxSortieUnits;

                sortieInfoText.color = Color.white;
            }
        }

        Debug.Log("Sortie Units selected: " + selectedCount);
    }

    public void ConfirmSortie()
    {
        int selectedCount = 0;
        if (kojiToggle.isOn) selectedCount++;
        if (tetsuyaToggle.isOn) selectedCount++;
        if (amuroToggle.isOn) selectedCount++;
        if (ryomaToggle.isOn) selectedCount++;
        if (sayakaToggle.isOn) selectedCount++;
        if (junToggle.isOn) selectedCount++;
        if (bossToggle.isOn) selectedCount++;
        if (michiruToggle.isOn) selectedCount++;
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
        SortieData.RyomaSelected = ryomaToggle.isOn;
        SortieData.SayakaSelected = sayakaToggle.isOn;
        SortieData.JunSelected = junToggle.isOn;
        SortieData.BossSelected = bossToggle.isOn;
        SortieData.MichiruSelected = michiruToggle.isOn;
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

        if (ryomaToggle != null)
            ryomaToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (sayakaToggle != null)
            sayakaToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (junToggle != null)
            junToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (bossToggle != null)
            bossToggle.onValueChanged.RemoveListener(OnSelectionChanged);

        if (michiruToggle != null)
            michiruToggle.onValueChanged.RemoveListener(OnSelectionChanged);
    }

}
