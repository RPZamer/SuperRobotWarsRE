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

    // WEEK 3: Let other scripts read these settings without changing the asset.
    public string MechName => mechName;
    public Sprite BattleSprite => battleSprite;
    public int Health => health;
    public int Energy => energy;
    public int Movement => movement;
    public int Mobility => mobility;
    public int Armor => armor;
    public MechSize Size => size;
    public TerrainRatings TerrainRatings => terrainRatings;
    public IReadOnlyList<Weapon> Weapons => weapons;
}
