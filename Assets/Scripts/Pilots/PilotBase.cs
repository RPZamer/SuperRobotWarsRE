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

    [Header("Battle")]
    [SerializeField] private Sprite battleSprite;
    [Min(1)][SerializeField] private int health = 10;
    [Min(0)][SerializeField] private int attack = 2;

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
    public Sprite BattleSprite => battleSprite;
    public int Health => health;
    public int Attack => attack;
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