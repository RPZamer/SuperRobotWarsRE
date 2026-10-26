
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using System.Runtime.CompilerServices;

public class IntermissionMenu : MonoBehaviour
{
    [SerializeField] private SceneTransition sceneTransition;

    [Header("Campaign Levels")]
    [SerializeField] private List<string> campaignLevels = new();
    private const string NextSceneKey = "NextScene";

    private string nextScene;

    [Header("Intermission Panels")]
    [SerializeField] private GameObject mechsPanel;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pilotsPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [Header("Sortie Menu Availability")]
    [SerializeField] private GameObject sortieButton;

    [Header("Debug Level Select")]
    [SerializeField] private GameObject levelSelectButton;
    private bool debugLevelSelectUnlocked= false; // Set this to true to enable the level select button

    [Header("Mechs")]
    [SerializeField] private List<MechBase> mechs = new();
    [Header("Mech Availability")]
    [SerializeField] private List<GameObject> mechButtons = new();
    [Header("Getter Forms")]
    
    [SerializeField] private GameObject getterFormsPanel;

    [SerializeField] private TMP_Text mechInfoText;
    [SerializeField] private GameObject mechInfoBox;
    [SerializeField] private GameObject weaponsBox;
    [SerializeField] private TMP_Text weaponNamesText;
    [SerializeField] private TMP_Text weaponPowerText;
    [SerializeField] private TMP_Text weaponRangeText;
    [SerializeField] private Image mechImage;

    [Header("Pilots")]
    [SerializeField] private List<PilotBase> pilots = new();
    [SerializeField] private TMP_Text pilotInfoText;
    [Header("Pilot Availability")]
    [SerializeField] private List<GameObject> pilotButtons = new();

    [Header("Pilot Portrait")]
    [SerializeField] private Image pilotImage;
    [SerializeField] private GameObject pilotPortraitBox;
    [SerializeField] private GameObject sortiePanel;

    private bool IsSortieAvailable()
    {
        int levelIndex = campaignLevels.IndexOf(nextScene);
        return levelIndex >= 2 && levelIndex < campaignLevels.Count;
    }

    private void UpdateSortieAvailability()
    {
        bool available = IsSortieAvailable();
        
            if (sortieButton != null)
            {
                sortieButton.SetActive(available);
            }
            Debug.Log("Sortie available for " + nextScene + ": " + available);

    }
    private void Start()
    {
        
        if (campaignLevels == null || campaignLevels.Count == 0)
        {
            Debug.LogError("Campaign levels list is empty. Please assign the campaign levels in the inspector.");
            return;
        }

        nextScene = PlayerPrefs.GetString(NextSceneKey, campaignLevels[0]);
        Debug.Log("Saved NextScene: " + nextScene);
        Debug.Log("Level 2 in campaign list: " + campaignLevels[1]);
        Debug.Log("Scene name matches: "  + campaignLevels.Contains(nextScene));
        
        if (!campaignLevels.Contains(nextScene))
        {
            Debug.LogError("Saved mission does not match campaign list: [" + nextScene + "]");
            return;

        }
        UpdateSortieAvailability();

        if (levelSelectButton != null)
        {
            levelSelectButton.SetActive(debugLevelSelectUnlocked);
        }

       if(levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }

    }
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool ctrlHeld = keyboard.leftCtrlKey.isPressed ||
            keyboard.rightCtrlKey.isPressed;

        bool shiftHeld = keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        if (ctrlHeld && shiftHeld && keyboard.lKey.wasPressedThisFrame)
        {
            debugLevelSelectUnlocked = !debugLevelSelectUnlocked;
            if (levelSelectButton != null)
            {
                levelSelectButton.SetActive(debugLevelSelectUnlocked);
            }


            if (!debugLevelSelectUnlocked && levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(false);
                mainMenu.SetActive(true);
            }
            Debug.Log("Debug Level Select unlocked: " + debugLevelSelectUnlocked);

        }
    }

    public void OpenLevelSelect()
    {
        if (!debugLevelSelectUnlocked)
        {
            return;
        }

        mainMenu.SetActive(false);
        levelSelectPanel.SetActive(true);
    }
    public void CloseLevelSelect()
    {
        levelSelectPanel.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void SelectLevel(string sceneName)
    {
        if (!campaignLevels.Contains(sceneName))
        {
            Debug.LogWarning("Scene not found in campaign: " + sceneName);
            return;
        }

        nextScene = sceneName;
        PlayerPrefs.SetString(NextSceneKey, nextScene);
        PlayerPrefs.Save();

        Debug.Log("Debug selected mission: " + nextScene);

        if (sceneTransition != null)
        {
            sceneTransition.FadeToScene(sceneName);
        }
    }
    private void DisplayMech(MechBase mech)
    {
        
        if (mech == null || mechInfoText == null || weaponNamesText == null || weaponPowerText == null || weaponRangeText == null)
        {
            return;
        }
        mechInfoBox.SetActive(true);
        weaponsBox.SetActive(true);
        if(mechImage != null)
        {
            mechImage.sprite = mech.MenuSprite;
            mechImage.gameObject.SetActive(mech.MenuSprite != null);
        }

        mechInfoText.text = $"Name: {mech.MechName}\n" +
                                $"Health: {mech.Health}\n" +
                                $"Energy: {mech.Energy}\n" +
                                $"Movement: {mech.Movement}\n" +
                                $"Mobility: {mech.Mobility}\n" +
                                $"Armor: {mech.Armor}\n" +
                                $"Size: {mech.Size}\n\n"+
                                $"TERRAIN\n" +
                                $"Air: {mech.TerrainRatings.Get(TerrainType.Air)}\n" +
                                $"Ground: {mech.TerrainRatings.Get(TerrainType.Ground)}\n" +
                                $"Water: {mech.TerrainRatings.Get(TerrainType.Water)}\n" +
                                $"Space: {mech.TerrainRatings.Get(TerrainType.Space)}\n"
                                ;

        string names = "Name\n";
        string power = "Power\n";
        string range = "Range\n";

        foreach (Weapon weapon in mech.Weapons)
        {
            if (weapon != null)
            {
                names += $"{weapon.WeaponName}\n";
                power += $"{weapon.Power}\n";
                range += $"{weapon.MinRange}-{weapon.MaxRange}\n";
            }
        }

        weaponNamesText.text = names;
        weaponPowerText.text = power;
        weaponRangeText.text = range;

    }

    private void DisplayPilot(PilotBase pilot)
    {
        if (pilot == null || pilotInfoText == null)
        {
            return;
        }
        if (pilotImage != null)
       
        {
            Sprite portrait = pilot.GetPortrait(PilotEmotion.Default);

            pilotImage.sprite = portrait;
            pilotImage.gameObject.SetActive(portrait != null);
        }
       
        if (pilotPortraitBox != null)
        {
            pilotPortraitBox.SetActive(
                pilotImage != null && pilotImage.sprite != null
            );
        }
        string spritsText = "\nSPIRIT COMMANDS\n";
        bool hasSpirits = false;

        SpiritCommandBase[] spirits = pilot.GetSpiritCommands();

        foreach (SpiritCommandBase spirit in spirits)
        {
            if (spirit != null)
            {
                spritsText += $"{spirit.CommandName} SP: {spirit.SpiritPointCost}\n";
                hasSpirits = true;
            }
        }

        if (!hasSpirits)
        {
            spritsText += "None.\n";
        }

        string skillsText = "PILOT SKILLS\n";

        if (pilot.SaveBLevel > 0)
        {
            skillsText += $"Save B: {pilot.SaveBLevel}\n";
        }

        if (pilot.SaveELevel > 0)
        {
            skillsText += $"Save E: {pilot.SaveELevel}\n";
        }


        if (pilot.PotentialLevel > 0)
        {
            skillsText += $"Potential: {pilot.PotentialLevel}\n";
        }

        pilotInfoText.text = $"Name: {pilot.PilotName}\n" +
                             $"Melee: {pilot.Melee}\n" +
                             $"Ranged: {pilot.Ranged}\n" +
                             $"Defense: {pilot.Defense}\n" +
                             $"Evade: {pilot.Evade}\n" +
                             $"Accuracy: {pilot.Accuracy}\n" +
                             $"Skill: {pilot.Skill}\n" +
                             $"Morale: {pilot.Morale}\n" +
                             $"Max Spirit Points: {pilot.MaxSpiritPoints}\n\n" +
                             skillsText +
                             spritsText;


    }

    public void OpenPilots()
    {
        mainMenu.SetActive(false);
        pilotsPanel.SetActive(true);
        UpdatePilotAvailability();

        if (pilotInfoText != null)
        {
            pilotInfoText.text = "Select a pilot to view their stats.";
        }
        if (pilotImage != null)
        {
            pilotImage.sprite = null;
            pilotImage.gameObject.SetActive(false);
        }

        if (pilotPortraitBox != null)
        {
            pilotPortraitBox.SetActive(false);
        }
    }

    public void ClosePilots()
    {
        pilotsPanel.SetActive(false);
        mainMenu.SetActive(true);

    }

    public void OpenSortie()
    {
        if(!IsSortieAvailable())
        {
            Debug.LogWarning("Sortie selection is unavailable for this mission.");
            return;
        }

        mainMenu.SetActive(false);
        sortiePanel.SetActive(true);
    }

    public void CloseSortie()
    {
        sortiePanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void SaveGame()
    {
        PlayerPrefs.SetString("NextScene", nextScene);
        PlayerPrefs.Save();
        // Implement your save game logic here
        Debug.Log("Game saved! Next scene: " + nextScene);
    }
    public void NextStage()
    {
        Debug.Log("NEXT STAGE loading: " + nextScene);

        if (sceneTransition != null)
        {
            sceneTransition.FadeToScene(nextScene);
        }
        
    }

    public void OpenMechs()
    {
        mainMenu.SetActive(false);
        mechsPanel.SetActive(true);

        UpdateMechAvailability();
        if(getterFormsPanel != null)
        {
            getterFormsPanel.SetActive(false);
        }
        mechInfoBox.SetActive(false);
        weaponsBox.SetActive(false);

        if (mechInfoText != null)
        {
            mechInfoText.text = "Select a mech to view its stats.";
        }

        if (weaponNamesText != null)
        {
            weaponNamesText.text = "";
        }

        if (weaponPowerText != null)
        {
            weaponPowerText.text = "";
        }

        if (weaponRangeText != null)
        {
            weaponRangeText.text = "";
        }
        if(mechImage != null)
        {
            mechImage.sprite = null;
            mechImage.gameObject.SetActive(false);
        }   

    }
    private void UpdateMechAvailability()
    {
        int levelIndex = campaignLevels.IndexOf(nextScene);
        int visibleIndex = 0;

        for (int i = 0; i < mechButtons.Count; i++)
        {
            if (mechButtons[i] == null)
                continue;

            bool available = levelIndex >= 2 || i == 0 || i == 2 || i == 3;

            mechButtons[i].SetActive(available);

            if (available)
            {
                RectTransform buttonRect =
                    mechButtons[i].GetComponent<RectTransform>();

                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = new Vector2(
                        0,
                        -35 - (visibleIndex * 40)
                    );
                }

                visibleIndex++;
            }
        }
    }
    private void UpdatePilotAvailability()
    {
        int levelIndex = campaignLevels.IndexOf(nextScene);
        int visibleIndex = 0;

        for (int i = 0; i < pilotButtons.Count; i++)
        {
            if (pilotButtons[i] == null)
                continue;


            bool isStartingPilot = i == 0 || i == 2 || i == 3 ||i == 4 || i == 5;

            bool available = isStartingPilot || levelIndex >= 1;

            pilotButtons[i].SetActive(available);

            if (available)
            {
                RectTransform buttonRect =
                    pilotButtons[i].GetComponent<RectTransform>();

                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = new Vector2(
                        0, -35 - (visibleIndex * 40));
                }

                visibleIndex++;
            }
        }
    }
    public void ShowMech(int index)
    {
        if (index < 0 || index >= mechs.Count)
            return;

        if (getterFormsPanel != null)
        {
            getterFormsPanel.SetActive(index == 3);
        }

        DisplayMech(mechs[index]);
    }
    public void ShowGetterForm(int formIndex)
    {
        if (mechs.Count <= 3 || mechs[3] == null)
            return;

        MechBase getter1 = mechs[3];

        if (formIndex == 0)
        {
            DisplayMech(getter1);
            return;
        }



        int getterFormIndex = formIndex - 1;

        if (getterFormIndex < 0 ||
            getterFormIndex >= getter1.GetterForms.Count)
            return;

        DisplayMech(getter1.GetterForms[getterFormIndex]);
    }
    public void ShowMazingerZ()
    {
        if (mechs.Count > 0)
        {
            DisplayMech(mechs[0]);
        }
    }   

    public void ShowGreatMazinger()
    {
        if (mechs.Count > 1)
        {
            DisplayMech(mechs[1]);
        }
    }

    public void ShowRX78Gundam()
    {
        if (mechs.Count > 2)
        {
            DisplayMech(mechs[2]);
        }
    }

    public void CloseMechs()
    {
        mechsPanel.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void ShowPilot(int index)
    {
        if (index < 0 || index >= pilots.Count)
            return;

        DisplayPilot(pilots[index]);
    }
    public void ShowKojiKauto()
    {
        if (pilots.Count > 0)
        {
            DisplayPilot(pilots[0]);
        }
    }
    public void ShowTetsuyaTsurugi()
    {
        if (pilots.Count > 1)
        {
            DisplayPilot(pilots[1]);
        }
    }
    public void ShowAmuroRay()
    {
        if (pilots.Count > 2)
        {
            DisplayPilot(pilots[2]);
        }
    }   
}
