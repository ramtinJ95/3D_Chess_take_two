using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 0.8f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDraging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake()
    {
        generateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        spawnAllPieces();
        positionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Get the indexes of the tile we hit with the ray we emit when we click
            Vector2Int hitPosition = lookUpTileIndex(info.transform.gameObject);

            if (currentHover == new Vector2Int(-1, -1))
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;
            }

            if (currentHover != new Vector2Int(-1, -1))
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;
            }

            if(Input.GetMouseButtonDown(0))
            {
                if(chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // is it our turn?
                    if(true)
                    {
                        currentlyDraging = chessPieces[hitPosition.x, hitPosition.y];

                    }
                }
            }
            if (currentlyDraging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDraging.currentX, currentlyDraging.currentY);
                bool validMove = moveTo(currentlyDraging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDraging.SetPosition(getTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDraging = null;
                }
                else
                {
                    currentlyDraging = null;
                }
            }
        }
        else
        {
            if (currentHover != new Vector2Int(-1, -1))
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                currentHover = new Vector2Int(-1, -1);
            }

            if(currentlyDraging && Input.GetMouseButtonUp(0))
            {
                currentlyDraging.SetPosition(getTileCenter(currentlyDraging.currentX, currentlyDraging.currentY));
                currentlyDraging = null;

            }
        }

        // if dragging a piece
        if (currentlyDraging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDraging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }


    // Generate the board

    private void generateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountY / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = generateSingletile(tileSize, x, y);
            }
        }
    }

    // these are positions in our small 8x8 grid that represents the board
    private GameObject generateSingletile(float tileSize, int xPos, int yPos)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", xPos, yPos));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4]; // 4 since we are creating squares
        vertices[0] = new Vector3(xPos * tileSize, yOffset, yPos * tileSize) - bounds;
        vertices[1] = new Vector3(xPos * tileSize, yOffset, (yPos + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((xPos + 1) * tileSize, yOffset, yPos * tileSize) - bounds;
        vertices[3] = new Vector3((xPos + 1) * tileSize, yOffset, (yPos + 1) * tileSize) - bounds;


        /*
         * Here we are populating the array. The verticies has to come in a
         * counter clockwise order, because of how unity renders triangles,
         * if they are clockwise in order the engine assumes it is on the
         * backside of an object and does not render them. 
         */
        int[] triangles = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        tileObject.layer = LayerMask.NameToLayer("Tile");
        mesh.RecalculateNormals(); // Make the light being calculated properly in the scene

        tileObject.AddComponent<BoxCollider>();
        return tileObject;
    }

    // Spawning of the pieces
    private void spawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0;
        int blackTeam = 1;

        //white team
        chessPieces[0, 0] = spawningSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = spawningSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = spawningSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = spawningSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = spawningSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = spawningSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = spawningSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = spawningSinglePiece(ChessPieceType.Rook, whiteTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = spawningSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //black team
        chessPieces[0, 7] = spawningSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = spawningSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = spawningSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = spawningSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = spawningSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = spawningSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = spawningSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = spawningSinglePiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = spawningSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    private ChessPiece spawningSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    // Positioning
    private void positionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    positionSinglePiece(x, y, true);
            }
        }
    }
    // force used for eiter teleport the piece on board or smooth movement
    private void positionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;

        chessPieces[x, y].SetPosition(getTileCenter(x, y), force);
    }

    private Vector3 getTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize/2, 0, tileSize/2);
    }

    // Helpers
    private Vector2Int lookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    private bool moveTo(ChessPiece cp, int x, int y)
    {
        // is there another piece at this position we want to go to
        if(chessPieces[x,y] != null)
        {
            ChessPiece othercp = chessPieces[x, y];
            if (cp.team == othercp.team) // so we cant move to position with our own pieces
                return false;

            // if its the enemy team
            if(othercp.team ==0)
            {
                deadWhites.Add(othercp);
                othercp.SetScale(Vector3.one * deathSize);
                othercp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                deadBlacks.Add(othercp);
                othercp.SetScale(Vector3.one * deathSize);
                othercp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;
        positionSinglePiece(x, y);
        return true;
    }
}
