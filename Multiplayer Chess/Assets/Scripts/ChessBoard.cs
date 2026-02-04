using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public enum SpecialMove
{
    None = 0,
    EnPassant = 1,
    Castling = 2,
    Promotion = 3
}

public class ChessBoard : MonoBehaviour
{
    [Header("Art Components")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen; 

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private ChessPiece[,] chessPieces;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private bool isWhiteTurn;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private SpecialMove specialMove;
 

    public void Awake(){

        isWhiteTurn = true;
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
        Ray ray = currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if(currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");

            }
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x,currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if mouse clicked
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if(chessPieces[hitPosition.x,hitPosition.y] != null)
                {
                    //if it is players turn
                    if(chessPieces[hitPosition.x,hitPosition.y].team == 0 && isWhiteTurn || chessPieces[hitPosition.x,hitPosition.y].team == 1 && !isWhiteTurn)
                    {

                        currentlyDragging = chessPieces[hitPosition.x,hitPosition.y]; 

                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();
                                                
                        HighlightTiles();
                    }                
                }
            }
            //release mouse click
            if (Mouse.current.leftButton.wasReleasedThisFrame && currentlyDragging != null)
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                
                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                }
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x,currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
            if(currentlyDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        //If a piece is being dragged
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0;
            if(horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
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
        chessPieces[x,y].SetPosition(GetTileCenter(x,y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize,yOffset,y*tileSize) - bounds+ new Vector3(tileSize /2, 0, tileSize/2);
    }

    //Checkmate

    private void Checkmate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int team)
    {
        victoryScreen.SetActive(true);
        if(team == 1) victoryScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Black Team Wins";
        else if(team == 0) victoryScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "White Team Wins";
        else victoryScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Draw: Stalemate";
    }

    public void OnResetButton()
    {
        victoryScreen.SetActive(false);

        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    Destroy(chessPieces[x,y].gameObject);
                }
                chessPieces[x,y] = null;
            }
        }

        for(int i = 0; i < deadWhites.Count; i++) Destroy(deadWhites[i].gameObject);
        for(int i = 0; i < deadBlacks.Count; i++) Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnPieces();
        PositionAllPieces();

        isWhiteTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }

    //Special Moves
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count -1];
            ChessPiece pawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];

            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(pawn.currentX == enemyPawn.currentX)
            {
                if(pawn.currentY == enemyPawn.currentY - 1 ||pawn.currentY == enemyPawn.currentY + 1)
                {
                    if(enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize /2,0, tileSize/2)
                        + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize /2,0, tileSize/2)
                        + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }

        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece piece = chessPieces[lastMove[1].x, lastMove[1].y];
            if(piece.team == 0 && lastMove[1].y == 7)
            {
                ChessPiece queen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                queen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = queen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);

            }
            else if(piece.team == 1 && lastMove[1].y == 0)
            {
                ChessPiece queen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                queen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = queen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);

            }
        }

        if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count -1];
            if(lastMove[1].x == 2 )
            {
                if(lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3,0] = rook;
                    PositionSinglePiece(3,0);
                    chessPieces[0, 0] = null;
                }
                else if(lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3,7] = rook;
                    PositionSinglePiece(3,7);
                    chessPieces[0, 7] = null;
                }
            }
            else if(lastMove[1].x == 6)
            {
                if(lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5,0] = rook;
                    PositionSinglePiece(5,0);
                    chessPieces[7, 0] = null;
                }
                else if(lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5,7] = rook;
                    PositionSinglePiece(5,7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventCheck()
    {
        ChessPiece King = null;
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y]!= null)
                {
                    if(chessPieces[x, y].type == ChessPieceType.King)
                    {
                        if(chessPieces[x,y].team == currentlyDragging.team)
                        {
                            King = chessPieces[x,y];
                            break;
                        }
                    }
                }
                
            }
        }
        SimulateSinglePiece(currentlyDragging, ref availableMoves, King);
    }
    private void SimulateSinglePiece(ChessPiece piece, ref List<Vector2Int> moves, ChessPiece king)
    {
        //Save current values

        int actualX = piece.currentX;
        int actualY = piece.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Go through all possible moves to check if in check
        for(int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPostion = new Vector2Int(king.currentX, king.currentY);
            if(piece.type == ChessPieceType.King)
            {
                kingPostion = new Vector2Int(simX, simY);
            }

            //shallow copy 2D array of chess board
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> attackingPieces = new List<ChessPiece>();
            for(int x = 0; x < TILE_COUNT_X; x++)
            {
                for(int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x,y] != null)
                    {
                        simulation[x,y] = chessPieces[x,y];
                        if(simulation[x,y].team != piece.team)
                        {
                            attackingPieces.Add(simulation[x,y]);
                        }
                    }
                }
            }

            simulation[actualX, actualY] = null;
            piece.currentX = simX;
            piece.currentY =simY;
            simulation[simX,simY] = piece;

            var deadPiece = attackingPieces.Find(x => x.currentX == simX && x.currentY == simY);
            if(deadPiece != null)
            {
                attackingPieces.Remove(deadPiece);
            }

            List<Vector2Int> simMoves = new List<Vector2Int>();
            for(int j = 0; j <attackingPieces.Count; j++)
            {
                var pieceMoves = attackingPieces[j].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for(int k = 0; k < pieceMoves.Count; k++)
                {
                    simMoves.Add(pieceMoves[k]);
                }
            }

            if(ContainsValidMove(ref simMoves, kingPostion))
            {
                movesToRemove.Add(moves[i]);
            }
        }   

        //Remove from current move list
        for(int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }

        piece.currentX = actualX;
        piece.currentY = actualY;
    }

    private int CheckmateReached()
    {
        var lastMove = moveList[moveList.Count - 1];
        int enemy = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPiece = new List<ChessPiece>();
        List<ChessPiece> defendingPiece = new List<ChessPiece>();
        ChessPiece King = null;
        for(int x = 0; x < TILE_COUNT_X; x++)
        {
            for(int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y]!= null)
                {
                    if(chessPieces[x, y].team == enemy)
                    {
                        defendingPiece.Add(chessPieces[x,y]);
                        if(chessPieces[x,y].type == ChessPieceType.King) King = chessPieces[x,y];
                    }
                    else
                    {
                        attackingPiece.Add(chessPieces[x,y]);
                    }
                }
                
            }
        }

        List<Vector2Int> availableMoves = new List<Vector2Int>();
        for(int i = 0; i < attackingPiece.Count; i++)
        {
            var pieceMoves = attackingPiece[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for(int j = 0; j < pieceMoves.Count; j++)
            {
                availableMoves.Add(pieceMoves[j]);
            }
        }
        if(ContainsValidMove(ref availableMoves, new Vector2Int(King.currentX, King.currentY)))
        {
            for(int i = 0; i < defendingPiece.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPiece[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateSinglePiece(defendingPiece[i], ref defendingMoves, King);
                if(defendingMoves.Count != 0)
                {
                    return 0;
                }
            }

            return 1;
        }
        else
        {
            for(int i = 0; i < defendingPiece.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPiece[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateSinglePiece(defendingPiece[i], ref defendingMoves, King);
                if(defendingMoves.Count != 0)
                {
                    return 0;
                }
            }
            return 2;
        }

    }

    //Operations

    private void HighlightTiles()
    {
        for(int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    private void RemoveHighlightTiles()
    {
        for(int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for(int i = 0; i < moves.Count; i++)
        {
            if(moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }
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
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if(!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        if(chessPieces[x,y] != null)
        {

            ChessPiece other = chessPieces[x,y];
            if(cp.team == other.team)
            {
                return false;
            }
            else
            {
               if(other.team == 0)
                {
                    if(other.type == ChessPieceType.King)
                    {
                        Checkmate(1);
                    }

                    deadWhites.Add(other);
                    other.SetScale(Vector3.one * deathSize);
                    other.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize /2,0, tileSize/2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
                }
                else
                {
                    if(other.type == ChessPieceType.King) Checkmate(0);

                    deadBlacks.Add(other);
                    other.SetScale(Vector3.one * deathSize);
                    other.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize /2,0, tileSize/2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
                } 
            }
        }

        chessPieces[x,y] = cp;
        chessPieces[previousPosition.x,previousPosition.y] = null;
        PositionSinglePiece(x,y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x,y)});

        ProcessSpecialMove();
        switch (CheckmateReached())
        {
            default:
                break;
            case 1:
                Checkmate(cp.team);
                break;
            case 2:
                Checkmate(2);
                break;
        }


        return true;
    }
}
