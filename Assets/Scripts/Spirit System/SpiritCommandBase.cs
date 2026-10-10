using UnityEngine;

// WEEK 4: Defines the different categories of Spirit Commands
// listed in the team's Spirit Command design document.
public enum SpiritCommandType
{
    SelfBuff,
    GroupBuff,
    Debuff,
    Other,
    Support,
    Omnicast
}

// WEEK 4: Identifies the actual gameplay effect a Spirit Command performs.
// More Spirit effects can be added here as they are implemented.
public enum SpiritCommandEffect
{
    None,
    Valor,

    // WEEK 4: Soul makes the pilot's next attack deal 2.5X damage.
    Soul,

    // WEEK 4: Smash guarantees a critical hit on the pilot's next attack.
    Smash,

    // WEEK 4: Accel adds 3 spaces to the pilot's next movement.
    Accel,

    // WEEK 4: Bonds restores 50% HP to all allied units.
    Bonds,

    // WEEK 4: Rally gives +5 Morale to all allied units.
    Rally,

    // WEEK 4: Daunt reduces one enemy's Morale by 10.
    Daunt,

    // WEEK 4: Dread reduces every enemy's Morale by 5.
    Dread,

    // WEEK 4: Confuse removes 30 Spirit Points from one enemy.
    Confuse,

    // WEEK 4: Trust restores 30% HP to one allied unit.
    Trust,

    // WEEK 4: Prospect restores 30 SP to one allied unit.
    Prospect,

    // WEEK 5 CHANGES PLEASE READ: Shields must be restorable by a resupply Spirit, but the effect enum has no resupply entry.
    // This new entry is appended so the numeric values of all existing saved Spirit effects remain unchanged.
    // A configured Spirit asset can now select Resupply and use the existing target and SP system.
    Resupply,

    // WEEK 6: Vigor restores 30% of the caster's maximum HP.
    Vigor,

    // WEEK 6: Guts completely restores the caster's HP.
    Guts,

    // WEEK 6: Spirit raises the caster's Morale by 10.
    Spirit,

    // WEEK 6: Drive raises the caster's Morale by 30.
    Drive,

    // WEEK 6: Strike guarantees the caster's next attack hits.
    Strike,

    // WEEK 6: Alert guarantees evasion of the next incoming attack.
    Alert,

    // WEEK 6: Persist reduces damage from the next successful enemy attack.
    Persist,

    // WEEK 6: Focus temporarily increases accuracy and evasion.
    Focus,

    // WEEK 6: Faith completely restores one living ally's HP.
    Faith,

    // WEEK 6: Attune raises one allied unit's Morale by 10.
    Attune,

    // WEEK 6: Hope restores 50 Spirit Points to one living ally.
    Hope,


}


// WEEK 4: Store Spirit Commands as reusable assets so the same command
// can be assigned to multiple pilots without duplicating its data.
[CreateAssetMenu(
    fileName = "New Spirit Command",
    menuName = "SRW/Spirit Command")]
public class SpiritCommandBase : ScriptableObject
{
    [Header("Spirit Command")]
    [SerializeField] private string commandName;

    [TextArea(2, 5)]
    [SerializeField] private string description;

    [SerializeField] private SpiritCommandType commandType;

    // WEEK 4: Connect this Spirit Command asset to its runtime gameplay effect.
    [SerializeField] private SpiritCommandEffect effect;

    [Header("Spirit Cost")]
    [Min(0)]
    [SerializeField] private int spiritPointCost;

    public string CommandName => commandName;
    public string Description => description;
    public SpiritCommandType CommandType => commandType;

    public SpiritCommandEffect Effect => effect;
    public int SpiritPointCost => spiritPointCost;
}