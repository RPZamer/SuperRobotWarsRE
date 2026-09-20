using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class GridMovement : MonoBehaviour
{
    

    // The grid Size i set in unity is 0.5 x and z, 0.25 for y.
    [SerializeField] Grid GridTile;
    [SerializeField] Tilemap CustomTile;
    [SerializeField] int MovementRange;
    [SerializeField] float MovementSpeed = 5f;
    [SerializeField] int MoveCount;

    Vector3Int CurrentCell;
    Vector3 FixedPosition;
    bool isMoving;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // We need to find the current cell of the players and enemies on start
        CurrentCell = GridTile.WorldToCell(transform.position);

        FixedPosition = GridTile.GetCellCenterWorld(CurrentCell);
        transform.position = FixedPosition; // this snaps the player/enemy to the center of the cell.
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, FixedPosition, MovementSpeed * Time.deltaTime);
            if (transform.position == FixedPosition)
            {
                isMoving = false;
            }
            return;
        }

        // If the player/enemy moved more than its inital range, this stops all movement until the next turn (turns will be implemented later)
        if (MoveCount >= MovementRange)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0)) // left mouse click (for some reason unity indentifies it as 0?)
        {
            MoveToMouse();
        }
    }

    void MoveToMouse()
    {
        Ray Ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()); // this grabs the mouses position by shooting a ray from the camera to the mouse position.

        Plane GridPlane = new Plane(Vector3.forward, GridTile.transform.position);
        
        if (GridPlane.Raycast(Ray, out float CameraDistance))   // checks if the ray hits the grid plane
        {
            Vector3 MousePosition = Ray.GetPoint(CameraDistance);

            Vector3Int ClickedCell = GridTile.WorldToCell(MousePosition);   // this grabs the cell that the mouse is currently over

            CustomTiles ClickedCellType = CustomTile.GetTile<CustomTiles>(ClickedCell);

            if (GridManager.Instance.IsOccupied(ClickedCell))
            {
                Debug.Log("Cell is occupied");
                return;
            }

            if (ClickedCellType == null || ClickedCellType.NotPassable || !ClickedCellType.GroundUnitPassable)
            {
                Debug.Log("Cannot Move here");
                return;
            }

            

            int Distance = Mathf.Abs(ClickedCell.x - CurrentCell.x) + Mathf.Abs(ClickedCell.y - CurrentCell.y);
            Debug.Log("Distance: " + Distance); // debug log for testing

            // if the player can move to the clicked cell, if not it gives a debug log with the max range
            if (Distance <= MovementRange)
            {
                // clear the old cell (the current one before the script updates it)
                GridManager.Instance.FreeCell(CurrentCell);

                CurrentCell = ClickedCell;
                FixedPosition = GridTile.GetCellCenterWorld(CurrentCell);
                isMoving = true;

                // then set the new cell as occupied
                GridManager.Instance.OccupyCell(CurrentCell, gameObject);
            }
            else
            {
                Debug.Log("Exceeds Max distance of " + MovementRange);
            }
        }
    }
}
