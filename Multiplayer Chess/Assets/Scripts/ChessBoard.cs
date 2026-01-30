using UnityEngine;
using System;

public class ChessBoard : MonoBehaviour
{
    [Header("Art Components")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    private ChessPiece[,] chessPieces;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    public void Awake(){
        GenerateGrid(tileSize,TILE_COUNT_X,TILE_COUNT_Y);
        SpawnPieces();
        PositionAllPieces();
    }

    private void Update(){
        if(!currentCamera){
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if(currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");

            }
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x,currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x,currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
        }
    }

    private void GenerateGrid(float tileSize, int tileCountX,int tileCountY){
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX/2) * tileSize,0,(tileCountY/2) * tileSize) + boardCenter;


        tiles = new GameObject[tileCountX,tileCountY];
        for(int i = 0; i < tileCountX; i++){
            for(int j = 0; j <tileCountY; j ++){
                tiles[i,j] = GenerateTile(tileSize, i, j);
            }
        }
    }
    private GameObject GenerateTile(float tileSize, int x, int y){
        GameObject tileObject= new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x*tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y+1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, y *tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (y+1) *tileSize) - bounds;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }
    //Spawn Pieces

    private void SpawnPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int white = 0, black = 1;

        //White Team
        chessPieces[0,0] = SpawnSinglePiece(ChessPieceType.Rook, white);
        chessPieces[1,0] = SpawnSinglePiece(ChessPieceType.Knight, white);
        chessPieces[2,0] = SpawnSinglePiece(ChessPieceType.Bishop, white);
        chessPieces[3,0] = SpawnSinglePiece(ChessPieceType.Queen, white);
        chessPieces[4,0] = SpawnSinglePiece(ChessPieceType.King, white);
        chessPieces[5,0] = SpawnSinglePiece(ChessPieceType.Bishop, white);
        chessPieces[6,0] = SpawnSinglePiece(ChessPieceType.Knight, white);
        chessPieces[7,0] = SpawnSinglePiece(ChessPieceType.Rook, white);

        for(int i =0; i< TILE_COUNT_X; i++)
        {
            chessPieces[i,1] = SpawnSinglePiece(ChessPieceType.Pawn, white);
        }


        //Black Team
        chessPieces[0,7] = SpawnSinglePiece(ChessPieceType.Rook, black);
        chessPieces[1,7] = SpawnSinglePiece(ChessPieceType.Knight, black);
        chessPieces[2,7] = SpawnSinglePiece(ChessPieceType.Bishop, black);
        chessPieces[3,7] = SpawnSinglePiece(ChessPieceType.Queen, black);
        chessPieces[4,7] = SpawnSinglePiece(ChessPieceType.King, black);
        chessPieces[5,7] = SpawnSinglePiece(ChessPieceType.Bishop, black);
        chessPieces[6,7] = SpawnSinglePiece(ChessPieceType.Knight, black);
        chessPieces[7,7] = SpawnSinglePiece(ChessPieceType.Rook, black);

        for(int i =0; i< TILE_COUNT_X; i++)
        {
            chessPieces[i,6] = SpawnSinglePiece(ChessPieceType.Pawn, black);
        }
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type-1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //Positioning

    public void PositionAllPieces()
    {
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            for(int j = 0; j <TILE_COUNT_Y; j++)
            {
                if(chessPieces[i,j] != null)
                {
                    PositionSinglePiece(i,j,true);
                }
            }
        }
    }
    public void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x,y].currentX = x;
        chessPieces[x,y].currentY = y;
        chessPieces[x,y].transform.position = GetTileCenter(x,y);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize,yOffset,y*tileSize) - bounds+ new Vector3(tileSize /2, 0, tileSize/2);
    }
    //Operations

    private Vector2Int LookupTileIndex(GameObject hitinfo)
    {
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            for(int j = 0; j < TILE_COUNT_Y; j++)
            {
                if(tiles[i,j] == hitinfo)
                {
                    return new Vector2Int(i,j);
                }
            }
        }
        return -Vector2Int.one; //invalid access
    }
}
