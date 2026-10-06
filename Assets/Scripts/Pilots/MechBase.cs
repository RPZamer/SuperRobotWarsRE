using System.Collections.Generic;
using UnityEngine;

// WEEK 3: List the mech sizes used by the hit chance formula.
public enum MechSize { SS, S, M, L, LL }

// WEEK 3: Create a reusable mech asset with its own stats, sprite and weapons.
[CreateAssetMenu(fileName = "New Mech", menuName = "SRW/Mech")]
public class MechBase : ScriptableObject
{
    // WEEK 3: Give the mech a name and the sprite shown in battle.
    [SerializeField] private string mechName;
    [SerializeField] private Sprite battleSprite;

    [SerializeField] private Sprite menuSprite;


    // WEEK 5: MUSIC SYSTEM - Store the theme that plays when this mech attacks.
    [SerializeField] private AudioClip battleTheme;


    // WEEK 3: Set starting health and the energy available for moving and attacking.
    [Min(1)][SerializeField] private int health = 10;
    [Min(0)][SerializeField] private int energy = 100;

    // WEEK 3: Movement limits grid steps; Mobility helps dodge; Armor reduces damage.
    [Min(0)][SerializeField] private int movement = 3;
    [Min(0)][SerializeField] private int mobility = 100;
    [Min(0)][SerializeField] private int armor;

    // WEEK 3: Set the mech size, terrain ratings and its own list of weapons.
    [SerializeField] private MechSize size = MechSize.M;
    [SerializeField] private TerrainRatings terrainRatings = new();
    [SerializeField] private List<Weapon> weapons = new();

    // WEEK 5 CHANGES PLEASE READ: The current mech asset exposes no ability settings, so the effect script has nothing to read.
    // These fields restore the saved ability names and add an optional component recipe for compatible combinations.
    // Defaults leave ordinary mechs unchanged, while the actual rules and runtime state stay in MechSkillEffect.
    [Header("WEEK 5 Mech Skills")]
    [Min(0)][SerializeField] private int shieldHealth;
    [SerializeField] private MechBarrier barrier;
    [SerializeField] private SpecialEvasionAbility specialEvasionAbility;
    [HideInInspector][Range(0, 100)][SerializeField] private int specialEvasionChance;
    [HideInInspector][Min(0)][SerializeField] private int specialEvasionMorale = 130;
    [SerializeField] private RegenerationLevel hpRegeneration;
    [SerializeField] private RegenerationLevel enRegeneration;
    [SerializeField] private bool mazinPower;
    [SerializeField] private MechMode mode;
    [SerializeField] private bool gundFormat;
    [Min(0)][SerializeField] private int sight = 140;
    [SerializeField] private bool repairDevice;
    [SerializeField] private bool resupplyDevice;
    [Header("Forms and combination")]
    [SerializeField] private MechBase transformInto;
    [SerializeField] private MechBase combineInto;
    [SerializeField] private List<MechBase> requiredComponents = new();
    [SerializeField] private string getterCompatibilityId;
    [Min(1)][SerializeField] private int requiredGetterPilots = 3;
    [SerializeField] private List<MechBase> getterForms = new();
    [SerializeField] private PilotBase mainGetterPilot;
    [SerializeField] private List<PilotBase> requiredGetterCrew = new();

    public int ShieldHealth => Mathf.Max(0, shieldHealth);
    public MechBarrier Barrier => barrier;
    public SpecialEvasionAbility SpecialEvasionAbility => specialEvasionAbility;
    internal int LegacyEvasionChance => specialEvasionChance;
    internal int LegacyEvasionMorale => specialEvasionMorale;
    public RegenerationLevel HPRegeneration => hpRegeneration;
    public RegenerationLevel ENRegeneration => enRegeneration;
    public bool MazinPower => mazinPower;
    public MechMode Mode => mode;
    public bool GUNDFormat => gundFormat;
    public int Sight => sight;
    public bool RepairDevice => repairDevice;
    public bool ResupplyDevice => resupplyDevice;
    public MechBase TransformInto => transformInto;
    public MechBase CombineInto => combineInto;
    public IReadOnlyList<MechBase> RequiredComponents => requiredComponents;
    public string GetterCompatibilityId => getterCompatibilityId;
    public int RequiredGetterPilots => Mathf.Max(1, requiredGetterPilots);
    public IReadOnlyList<MechBase> GetterForms => getterForms;
    public PilotBase MainGetterPilot => mainGetterPilot;
    public IReadOnlyList<PilotBase> RequiredGetterCrew => requiredGetterCrew;

    // WEEK 3: Let other scripts read these settings without changing the asset.
    public string MechName => mechName;
    public Sprite BattleSprite => battleSprite;

    public Sprite MenuSprite => menuSprite;


    // WEEK 5: MUSIC SYSTEM - Let the music system read this mech's assigned theme.
    public AudioClip BattleTheme => battleTheme;


    public int Health => health;
    public int Energy => energy;
    public int Movement => movement;
    public int Mobility => mobility;
    public int Armor => armor;
    public MechSize Size => size;
    public TerrainRatings TerrainRatings => terrainRatings;
    public IReadOnlyList<Weapon> Weapons => weapons;
}