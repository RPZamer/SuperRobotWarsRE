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

    public string PilotName => pilotName;
    public Sprite BattleSprite => battleSprite;
    public int Health => health;
    public int Attack => attack;
}