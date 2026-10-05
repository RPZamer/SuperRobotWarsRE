
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class IntermissionMenu : MonoBehaviour
{
    [SerializeField] private SceneTransition sceneTransition;

    [Header("Intermission Panels")]
    [SerializeField] private GameObject mechsPanel;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pilotsPanel;

    [Header("Mechs")]
    [SerializeField] private List<MechBase> mechs = new();
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

        if (pilotInfoText != null)
        {
            pilotInfoText.text = "Select a pilot to view their stats.";
        }
    }

    public void ClosePilots()
    {
        pilotsPanel.SetActive(false);
        mainMenu.SetActive(true);

    }

    public void SaveGame()
    {
        PlayerPrefs.SetString("NextScene", "SampleScene");
        PlayerPrefs.Save();
        // Implement your save game logic here
        Debug.Log("Game saved!");
    }
    public void NextStage()
    {
        if (sceneTransition != null)
        {
            sceneTransition.FadeToScene("SampleScene");
        }
        
    }

    public void OpenMechs()
    {
        mainMenu.SetActive(false);
        mechsPanel.SetActive(true);

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
        }   

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
