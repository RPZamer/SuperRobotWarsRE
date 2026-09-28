using System.Collections.Generic;
using UnityEngine;

public enum PilotEmotion
{
    Default,
    Angry,
    Mad,
    Sad,
    Motivated,
    Defeated
}

[CreateAssetMenu(fileName = "New Pilot", menuName = "SRW/Pilot")]
public class PilotBase : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string pilotName;

    // WEEK 3: Keep all pilot stats here, together with the pilot name and dialogue.
    [Header("Pilot Stats")]
    // WEEK 3: Melee and Ranged increase damage for their matching weapon type.
    [Min(0)][SerializeField] private int melee = 100;
    [Min(0)][SerializeField] private int ranged = 100;
    // WEEK 3: Defense reduces damage; Evade helps the mech dodge.
    [Min(0)][SerializeField] private int defense = 100;
    [Min(0)][SerializeField] private int evade = 100;
    // WEEK 3: Accuracy helps attacks hit; Skill affects critical chance.
    [Min(0)][SerializeField] private int accuracy = 100;
    [Min(0)][SerializeField] private int skill = 100;
    // WEEK 3: Morale helps attack and defense. This is the renamed Will stat.
    [Min(0)][SerializeField] private int morale = 100;
    // WEEK 3: Store how well this pilot handles each terrain.

    // WEEK 4: Spirit Points are spent when the pilot uses Spirit Commands.
    // The value is configurable because the design document does not
    // specify one universal SP amount for every pilot.
    [Min(0)][SerializeField] private int maxSpiritPoints = 100;
    [SerializeField] private TerrainRatings terrainRatings = new();

    // WEEK 4: Each pilot can have up to six Spirit Commands.
    // These slots correspond to the six Spirit positions in the pilot database.
    [Header("Spirit Commands")]
    [SerializeField] private SpiritCommandBase spirit1;
    [SerializeField] private SpiritCommandBase spirit2;
    [SerializeField] private SpiritCommandBase spirit3;
    [SerializeField] private SpiritCommandBase spirit4;
    [SerializeField] private SpiritCommandBase spirit5;
    [SerializeField] private SpiritCommandBase spirit6;

    // WEEK 4: Return all six Spirit Command slots so the Spirit UI
    // can automatically build itself from the selected pilot's commands.
    public SpiritCommandBase[] GetSpiritCommands()
    {
        return new SpiritCommandBase[]
        {
        spirit1,
        spirit2,
        spirit3,
        spirit4,
        spirit5,
        spirit6
        };
    }

    [Header("Battle Dialogue")]
    [SerializeField] private List<string> onSuccessLines = new();

    [Header("Dialogue Portraits")]
    [SerializeField] private Sprite defaultPortrait;
    [SerializeField] private Sprite angryPortrait;
    [SerializeField] private Sprite madPortrait;
    [SerializeField] private Sprite sadPortrait;
    [SerializeField] private Sprite motivatedPortrait;
    [SerializeField] private Sprite defeatedPortrait;

    public string PilotName => pilotName;
    // WEEK 3: Let battle code read the pilot stats directly from PilotBase.
    public int Melee => melee;
    public int Ranged => ranged;
    public int Defense => defense;
    public int Evade => evade;
    public int Accuracy => accuracy;
    public int Skill => skill;
    public int Morale => morale;

    // WEEK 4: Allow BattleUnit to initialize this pilot's current SP.
    public int MaxSpiritPoints => maxSpiritPoints;

    public TerrainRatings TerrainRatings => terrainRatings;

    // WEEK 4: Allow the battle and Spirit UI systems to read
    // the six Spirit Commands assigned to this pilot.
    public SpiritCommandBase Spirit1 => spirit1;
    public SpiritCommandBase Spirit2 => spirit2;
    public SpiritCommandBase Spirit3 => spirit3;
    public SpiritCommandBase Spirit4 => spirit4;
    public SpiritCommandBase Spirit5 => spirit5;
    public SpiritCommandBase Spirit6 => spirit6;
    public IReadOnlyList<string> OnSuccessLines => onSuccessLines;

    public Sprite GetPortrait(PilotEmotion emotion)
    {
        Sprite portrait = emotion switch
        {
            PilotEmotion.Angry => angryPortrait,
            PilotEmotion.Mad => madPortrait,
            PilotEmotion.Sad => sadPortrait,
            PilotEmotion.Motivated => motivatedPortrait,
            PilotEmotion.Defeated => defeatedPortrait,
            _ => defaultPortrait
        };

        // An emotion can be left empty while a pilot is still being set up.
        return portrait != null ? portrait : defaultPortrait;
    }

    // WEEK 4: Find a Spirit Command assigned to this pilot
    // by its gameplay effect.
    public SpiritCommandBase GetSpiritCommand(SpiritCommandEffect effect)
    {
        SpiritCommandBase[] spirits =
        {
        spirit1,
        spirit2,
        spirit3,
        spirit4,
        spirit5,
        spirit6
    };

        foreach (SpiritCommandBase spirit in spirits)
        {
            if (spirit != null && spirit.Effect == effect)
            {
                return spirit;
            }
        }

        return null;
    }
}