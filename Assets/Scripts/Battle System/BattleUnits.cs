using UnityEngine;

public enum BattleTeam
{
    Player,
    Enemy
}

[RequireComponent(typeof(SpriteRenderer))]
public class BattleUnit : MonoBehaviour
{
    [SerializeField] private PilotBase pilot;
    [SerializeField] private BattleTeam team;
    [SerializeField] private Vector2Int startingPosition;

    private Battlefield battlefield;

    public PilotBase Pilot => pilot;
    public BattleTeam Team => team;
    public Vector2Int GridPosition { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDefeated => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = pilot != null ? pilot.Health : 1;

        if (pilot != null)
        {
            GetComponent<SpriteRenderer>().sprite = pilot.BattleSprite;
        }
    }

    public bool PlaceOn(Battlefield targetBattlefield)
    {
        battlefield = targetBattlefield;
        return battlefield != null && battlefield.TryPlace(this, startingPosition);
    }

    public void SetGridPosition(Vector2Int position)
    {
        GridPosition = position;
        transform.position = battlefield.GridToWorld(position);
    }

    public int TakeDamage(int damage)
    {
        int appliedDamage = Mathf.Min(CurrentHealth, Mathf.Max(0, damage));
        CurrentHealth -= appliedDamage;

        if (IsDefeated)
        {
            battlefield.Remove(this);
            Destroy(gameObject);
        }

        return appliedDamage;
    }
}