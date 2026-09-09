using System.Collections.Generic;
using UnityEngine;

public class Battlefield : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)][SerializeField] private int width = 10;
    [Min(1)][SerializeField] private int height = 10;
    [Min(0.1f)][SerializeField] private float cellSize = 1f;

    [Header("Runtime Grid Lines")]
    [SerializeField] private bool showGridLines = true;
    [SerializeField] private Color gridLineColor = new(1f, 1f, 1f, 0.35f);
    [Range(0.005f, 0.1f)][SerializeField] private float gridLineWidth = 0.025f;
    [SerializeField] private int gridLineSortingOrder = 5;

    [Header("Highlights")]
    [SerializeField] private Color movableColor = new(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color blockedColor = new(1f, 0f, 0f, 0.3f);
    [SerializeField] private int highlightSortingOrder = 10;

    private readonly Dictionary<Vector2Int, BattleUnit> occupants = new();
    private readonly List<SpriteRenderer> highlights = new();
    private Sprite highlightSprite;
    private Material gridLineMaterial;

    public IEnumerable<BattleUnit> Units => occupants.Values;

    private void Awake()
    {
        highlightSprite = CreateSquareSprite();

        if (showGridLines)
        {
            CreateRuntimeGridLines();
        }
    }

    public void RegisterSceneUnits()
    {
        BattleUnit[] sceneUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);

        foreach (BattleUnit unit in sceneUnits)
        {
            if (!occupants.ContainsValue(unit) && !unit.PlaceOn(this))
            {
                Debug.LogWarning($"Could not place {unit.name} on its starting grid position.", unit);
            }
        }
    }

    public bool TryPlace(BattleUnit unit, Vector2Int position)
    {
        if (unit == null || !IsInside(position) || occupants.ContainsKey(position))
        {
            return false;
        }

        occupants[position] = unit;
        unit.SetGridPosition(position);
        return true;
    }

    public bool TryMove(BattleUnit unit, Vector2Int destination)
    {
        if (unit == null || !IsInside(destination) || occupants.ContainsKey(destination))
        {
            return false;
        }

        occupants.Remove(unit.GridPosition);
        occupants[destination] = unit;
        unit.SetGridPosition(destination);
        return true;
    }

    public void Remove(BattleUnit unit)
    {
        if (unit != null && occupants.TryGetValue(unit.GridPosition, out BattleUnit occupant) && occupant == unit)
        {
            occupants.Remove(unit.GridPosition);
        }
    }

    public BattleUnit GetUnit(Vector2Int position)
    {
        occupants.TryGetValue(position, out BattleUnit unit);
        return unit;
    }

    public bool IsInside(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    public Vector3 GridToWorld(Vector2Int position)
    {
        return transform.position + new Vector3(position.x * cellSize, position.y * cellSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - transform.position;
        return new Vector2Int(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.y / cellSize));
    }

    public void ShowMovement(BattleUnit selectedUnit)
    {
        ClearHighlights();

        if (selectedUnit == null)
        {
            return;
        }

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = selectedUnit.GridPosition + direction;

            if (IsInside(position))
            {
                CreateHighlight(position, GetUnit(position) == null ? movableColor : blockedColor);
            }
        }
    }

    public SpriteRenderer ShowAttackTarget(Vector2Int position)
    {
        return CreateHighlight(position, blockedColor);
    }

    public void ClearHighlights()
    {
        foreach (SpriteRenderer highlight in highlights)
        {
            if (highlight != null)
            {
                Destroy(highlight.gameObject);
            }
        }

        highlights.Clear();
    }

    private SpriteRenderer CreateHighlight(Vector2Int position, Color color)
    {
        GameObject highlightObject = new($"Grid Highlight {position.x}, {position.y}");
        highlightObject.transform.SetParent(transform);
        highlightObject.transform.position = GridToWorld(position);
        highlightObject.transform.localScale = Vector3.one * cellSize;

        SpriteRenderer renderer = highlightObject.AddComponent<SpriteRenderer>();
        renderer.sprite = highlightSprite;
        renderer.color = color;
        renderer.sortingOrder = highlightSortingOrder;
        highlights.Add(renderer);
        return renderer;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new(1, 1);
        texture.name = "Runtime Grid Highlight";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void CreateRuntimeGridLines()
    {
        Shader spriteShader = Shader.Find("Sprites/Default");

        if (spriteShader == null)
        {
            Debug.LogWarning("Could not find the Sprites/Default shader for the runtime grid.", this);
            return;
        }

        gridLineMaterial = new Material(spriteShader)
        {
            name = "Runtime Grid Line Material"
        };

        GameObject gridLines = new("Runtime Grid Lines");
        gridLines.transform.SetParent(transform, false);

        float left = -cellSize * 0.5f;
        float right = (width - 0.5f) * cellSize;
        float bottom = -cellSize * 0.5f;
        float top = (height - 0.5f) * cellSize;

        for (int x = 0; x <= width; x++)
        {
            float xPosition = left + x * cellSize;
            CreateGridLine(gridLines.transform, new Vector3(xPosition, bottom), new Vector3(xPosition, top));
        }

        for (int y = 0; y <= height; y++)
        {
            float yPosition = bottom + y * cellSize;
            CreateGridLine(gridLines.transform, new Vector3(left, yPosition), new Vector3(right, yPosition));
        }
    }

    private void CreateGridLine(Transform parent, Vector3 start, Vector3 end)
    {
        GameObject lineObject = new("Grid Line");
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = gridLineMaterial;
        line.startColor = gridLineColor;
        line.endColor = gridLineColor;
        line.startWidth = cellSize * gridLineWidth;
        line.endWidth = cellSize * gridLineWidth;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.sortingOrder = gridLineSortingOrder;
    }

    private void OnDestroy()
    {
        if (gridLineMaterial != null)
        {
            Destroy(gridLineMaterial);
        }
    }
}