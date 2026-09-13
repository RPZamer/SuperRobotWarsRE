using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleSystem : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private Camera battleCamera;
    [SerializeField] private DialogSystem dialogSystem;

    [Header("Turns")]
    [SerializeField] private BattleTeam playerTeam = BattleTeam.Player;
    [Min(0f)][SerializeField] private float enemyMoveDelay = 0.5f;

    [Header("Selection")]
    [SerializeField] private float enemyPulseSeconds = 1f;

    private BattleUnit selectedUnit;
    private BattleUnit pendingAttackTarget;
    private SpriteRenderer attackTargetHighlight;
    private Vector2Int controllerCursor;
    private bool isPlayerTurn;

    // Battle messages remain readable briefly after their typewriter animation finishes.
    private const float MessageHoldSeconds = 0.75f;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        if (battlefield == null)
        {
            battlefield = FindFirstObjectByType<Battlefield>();
        }

        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        if (dialogSystem == null)
        {
            dialogSystem = FindFirstObjectByType<DialogSystem>();
        }


        if (battlefield == null)
        {
            Debug.LogError("BattleSystem needs a Battlefield in the scene.", this);
            enabled = false;
            return;
        }

        battlefield.RegisterSceneUnits();
        StartCoroutine(BeginPlayerTurn());
    }

    private void Update()
    {
        if (isPlayerTurn)
        {
            HandleMouse();
        }

        PulseAttackTarget();
    }

    private void HandleMouse()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || battleCamera == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceToGrid = Mathf.Abs(battleCamera.transform.position.z - battlefield.transform.position.z);
        Vector3 screenPosition = new(mousePosition.x, mousePosition.y, distanceToGrid);
        Vector3 worldPosition = battleCamera.ScreenToWorldPoint(screenPosition);
        ConfirmCell(battlefield.WorldToGrid(worldPosition));
    }

    private void MoveCursor(Vector2Int direction)
    {
        Vector2Int next = controllerCursor + direction;

        if (battlefield.IsInside(next))
        {
            controllerCursor = next;
        }
    }

    private void ConfirmCell(Vector2Int position)
    {
        if (!battlefield.IsInside(position))
        {
            return;
        }

        BattleUnit occupant = battlefield.GetUnit(position);

        if (occupant != null && occupant.Team == playerTeam)
        {
            SelectUnit(occupant);
            return;
        }

        if (selectedUnit == null || ManhattanDistance(selectedUnit.GridPosition, position) != 1)
        {
            return;
        }

        if (occupant == null)
        {
            if (battlefield.TryMove(selectedUnit, position))
            {
                controllerCursor = position;
                PrepareForTurnChange();
                StartCoroutine(RunEnemyTurn());
            }

            return;
        }

        if (occupant.Team != selectedUnit.Team)
        {
            if (pendingAttackTarget == occupant)
            {
                PrepareForTurnChange();
                StartCoroutine(ResolvePlayerAttack(occupant));
            }
            else
            {
                ChooseAttackTarget(occupant);
            }
        }
    }

    private IEnumerator BeginPlayerTurn()
    {
        isPlayerTurn = false;

        BattleUnit player = FindFirstUnit(playerTeam);
        if (player == null)
        {
            yield break;
        }

        yield return ShowBattleMessage(player.Pilot, PilotEmotion.Motivated, $"{GetPilotName(player)}'s turn.");
        SelectUnit(player);
        isPlayerTurn = true;
    }

    private IEnumerator ResolvePlayerAttack(BattleUnit target)
    {
        yield return ResolveAttack(selectedUnit, target);

        if (FindFirstUnit(OpposingTeam(playerTeam)) != null)
        {
            yield return RunEnemyTurn();
        }
    }

    private IEnumerator RunEnemyTurn()
    {
        isPlayerTurn = false;

        BattleTeam enemyTeam = OpposingTeam(playerTeam);
        BattleUnit enemy = FindFirstUnit(enemyTeam);
        BattleUnit player = FindClosestUnit(enemy, playerTeam);

        if (enemy == null || player == null)
        {
            yield break;
        }

        yield return ShowBattleMessage(enemy.Pilot, PilotEmotion.Angry, $"{GetPilotName(enemy)}'s turn.");

        if (enemyMoveDelay > 0f)
        {
            yield return new WaitForSeconds(enemyMoveDelay);
        }

        if (ManhattanDistance(enemy.GridPosition, player.GridPosition) == 1)
        {
            yield return ResolveAttack(enemy, player);
        }
        else
        {
            MoveEnemyCloser(enemy, player);
        }

        if (FindFirstUnit(playerTeam) != null && FindFirstUnit(enemyTeam) != null)
        {
            yield return BeginPlayerTurn();
        }
    }

    private IEnumerator ResolveAttack(BattleUnit attacker, BattleUnit target)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        PilotBase attackerPilot = attacker.Pilot;
        PilotBase targetPilot = target.Pilot;
        string attackerName = GetPilotName(attacker);
        string targetName = GetPilotName(target);
        int attack = attackerPilot != null ? attackerPilot.Attack : 0;
        int damage = target.TakeDamage(attack);
        bool defeated = target.IsDefeated;

        // Damage and defeat messages use text only, so their portrait flag remains false.
        yield return ShowBattleMessage(
            targetPilot,
            defeated ? PilotEmotion.Defeated : PilotEmotion.Sad,
            $"{targetName} took {damage} damage.");

        if (!defeated)
        {
            yield break;
        }

        yield return ShowBattleMessage(targetPilot, PilotEmotion.Defeated, $"{targetName} was defeated.");

        if (attackerPilot == null)
        {
            yield break;
        }

        // Success lines play in list order and are the only battle messages that show a portrait.
        foreach (string successLine in attackerPilot.OnSuccessLines)
        {
            if (!string.IsNullOrWhiteSpace(successLine))
            {
                yield return ShowBattleMessage(attackerPilot, PilotEmotion.Motivated, successLine, true);
            }
        }
    }

    private IEnumerator ShowBattleMessage(
        PilotBase pilot,
        PilotEmotion emotion,
        string message,
        bool showPortrait = false)
    {
        // Dialogue is optional; skipping it must never stop the battle coroutine.
        if (dialogSystem == null)
        {
            yield break;
        }

        dialogSystem.SetVisibleImmediate(true);
        dialogSystem.DisplayLine(pilot, emotion, message, showPortrait);

        while (dialogSystem.IsTyping)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(MessageHoldSeconds);

        yield return dialogSystem.FadeOut();
    }

private void MoveEnemyCloser(BattleUnit enemy, BattleUnit target)
    {
        List<Vector2Int> openMoves = new();
        List<Vector2Int> closerMoves = new();
        int currentDistance = ManhattanDistance(enemy.GridPosition, target.GridPosition);

        foreach (Vector2Int direction in Directions)
        {
            Vector2Int position = enemy.GridPosition + direction;

            if (!battlefield.IsInside(position) || battlefield.GetUnit(position) != null)
            {
                continue;
            }

            openMoves.Add(position);

            if (ManhattanDistance(position, target.GridPosition) < currentDistance)
            {
                closerMoves.Add(position);
            }
        }

        List<Vector2Int> choices = closerMoves.Count > 0 ? closerMoves : openMoves;

        if (choices.Count > 0)
        {
            battlefield.TryMove(enemy, choices[Random.Range(0, choices.Count)]);
        }
    }

    private BattleUnit FindFirstUnit(BattleTeam team)
    {
        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit != null && !unit.IsDefeated && unit.Team == team)
            {
                return unit;
            }
        }

        return null;
    }

    private BattleUnit FindClosestUnit(BattleUnit source, BattleTeam team)
    {
        if (source == null)
        {
            return null;
        }

        BattleUnit closest = null;
        int closestDistance = int.MaxValue;

        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit == null || unit.IsDefeated || unit.Team != team)
            {
                continue;
            }

            int distance = ManhattanDistance(source.GridPosition, unit.GridPosition);
            if (distance < closestDistance)
            {
                closest = unit;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void SelectUnit(BattleUnit unit)
    {
        selectedUnit = unit;
        controllerCursor = unit.GridPosition;
        ClearAttackTarget();
        battlefield.ShowMovement(selectedUnit);
    }

    private void ChooseAttackTarget(BattleUnit target)
    {
        ClearAttackTarget();
        pendingAttackTarget = target;
        battlefield.ShowMovement(selectedUnit);
        attackTargetHighlight = battlefield.ShowAttackTarget(target.GridPosition);
    }

    private void PrepareForTurnChange()
    {
        isPlayerTurn = false;
        ClearAttackTarget();
        battlefield.ClearHighlights();
    }

    private void PulseAttackTarget()
    {
        if (attackTargetHighlight == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, enemyPulseSeconds);
        float alpha = Mathf.Lerp(0.3f, 0.8f, Mathf.PingPong(Time.time * 2f / duration, 1f));
        Color color = attackTargetHighlight.color;
        color.a = alpha;
        attackTargetHighlight.color = color;
    }

    private void ClearAttackTarget()
    {
        pendingAttackTarget = null;
        attackTargetHighlight = null;
    }

    private static BattleTeam OpposingTeam(BattleTeam team)
    {
        return team == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
    }

    private static string GetPilotName(BattleUnit unit)
    {
        return unit != null && unit.Pilot != null ? unit.Pilot.PilotName : "Pilot";
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}