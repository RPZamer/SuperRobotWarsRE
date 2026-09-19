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
    [SerializeField] private TerrainRatings terrainRatings = new();

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
    public TerrainRatings TerrainRatings => terrainRatings;
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
}