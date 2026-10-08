using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// WEEK 6 QUALITY OF LIFE: Players need to move the action menu away from units they want to click.
// This dedicated handle translates pointer dragging into menu movement without replacing any action-button callbacks.
// It clamps the visible controls rather than the oversized transparent panel, keeping the menu usable on the screen.
public class ActionsMenuDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform panel;
    private Canvas canvas;
    private System.Action moved;
    private Vector2 pointerStart, panelStart;

    //WEEK 6 QUALITY OF LIFE: Runtime-created handles need the existing panel and Canvas references.
    // Initialization stores those references and a callback that preserves the dragged position for the selected unit.
    // It does not change the Canvas input settings or attach a command to the handle's Button component.
    public void Initialize(RectTransform target, Canvas owner, System.Action onMoved)
    {
        panel = target;
        canvas = owner;
        moved = onMoved;
    }

    //  WEEK 6 QUALITY OF LIFE: A drag must start from the pointer's actual location to avoid a menu jump.
    // This captures the pointer and panel positions in the panel parent's coordinate system for left-button drags.
    // Canvas scaling and camera-based UI are handled through the same RectTransform conversion used by Unity UI.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (panel == null || eventData.button != PointerEventData.InputButton.Left) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)panel.parent, eventData.position, eventData.pressEventCamera, out pointerStart);
        panelStart = panel.anchoredPosition;
    }

    //WEEK 6 QUALITY OF LIFE: Holding and moving the handle should reposition the entire action menu.
    // Each drag applies the pointer displacement, clamps the visible menu and marks its position as manually chosen.
    // Right-button input is ignored so the existing battle Back command keeps its normal meaning.
    public void OnDrag(PointerEventData eventData)
    {
        if (panel == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)panel.parent, eventData.position, eventData.pressEventCamera, out Vector2 pointer)) return;
        panel.anchoredPosition = panelStart + pointer - pointerStart;
        ClampToCanvas();
        moved?.Invoke();
    }

    // WEEK 6 QUALITY OF LIFE: The saved ActionPanel stretches across the Canvas even though its visible controls are small.
    // Clamping uses the visible child graphics instead of that full-screen rectangle so dragging can still move the menu naturally.
    // The resulting correction keeps the handle and buttons inside the Canvas without making the background intercept clicks.
    public void ClampToCanvas()
    {
        if (panel == null || canvas == null) return;
        RectTransform canvasRect = (RectTransform)canvas.transform;
        Bounds bounds = VisualBounds(panel, canvasRect);
        Rect screen = canvasRect.rect;
        float x = bounds.size.x > screen.width ? screen.center.x - bounds.center.x :
            bounds.min.x < screen.xMin ? screen.xMin - bounds.min.x : bounds.max.x > screen.xMax ? screen.xMax - bounds.max.x : 0;
        float y = bounds.size.y > screen.height ? screen.center.y - bounds.center.y :
            bounds.min.y < screen.yMin ? screen.yMin - bounds.min.y : bounds.max.y > screen.yMax ? screen.yMax - bounds.max.y : 0;
        panel.position += canvasRect.TransformVector(new Vector3(x, y, 0));
    }

    // WEEK 6 QUALITY OF LIFE: Hidden controls and the transparent root image should not determine the draggable menu's size.
    // This gathers the corners of active visible child graphics in the requested coordinate system.
    // The same bounds place the handle above the existing controls and keep those controls on screen during dragging.
    public static Bounds VisualBounds(RectTransform root, RectTransform space)
    {
        Bounds bounds = new();
        bool found = false;
        Vector3[] corners = new Vector3[4];
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == root || !graphic.gameObject.activeInHierarchy || !graphic.enabled || graphic.color.a <= 0) continue;
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = space.InverseTransformPoint(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }
}