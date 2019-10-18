using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable 649

public class TicTacToe : MonoBehaviour
{
    [SerializeField] private bool _maximize = true;
    [SerializeField] private int _maxDepth = 10;
    [SerializeField] private int _boardSize = 3;
    [SerializeField] private int _countToWin = 3;

    [Header("Visualizer")] 
    [SerializeField] private Mesh _empty, _circle, _cross;
    [SerializeField] private Material _emptyMat, _circleMat, _crossMat;

    private enum CellState : byte { Empty, Cross, Circle }
    private class Board
    {
        public readonly CellState[,] Cells;
        private readonly int _size;
        private readonly int _toWin;

        public Board(int size, int toWin)
        {
            Cells = new CellState[size, size];
            _size = size;
            _toWin = toWin;
        }

        public CellState CheckWinner()
        {
            List<Vector2Int> occupied = new List<Vector2Int>();
            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    if(Cells[x,y] != CellState.Empty)
                        occupied.Add(new Vector2Int(x,y));
                }
            }

            foreach (Vector2Int pos in occupied)
            {
                bool hWin = true, vWin = true, suWin = true, sdWin = true;
                CellState state = Cells[pos.x, pos.y];
                for (int i = 0; i < _toWin; i++)
                {
                    if (!OnBoard(pos.x + i, pos.y) || Cells[pos.x + i, pos.y] != state)
                        hWin = false;
                    if (!OnBoard(pos.x, pos.y + i) || Cells[pos.x, pos.y + i] != state)
                        vWin = false;
                    if (!OnBoard(pos.x + i, pos.y + i) || Cells[pos.x + i, pos.y + i] != state)
                        suWin = false;
                    if (!OnBoard(pos.x + i, pos.y - i) || Cells[pos.x + i, pos.y - i] != state)
                        sdWin = false;
                }

                if (hWin || vWin || suWin || sdWin)
                    return state;
            }

            return CellState.Empty;
        }

        public bool OnBoard(int x, int y)
        {
            return x >= 0 && x < _size && y >= 0 && y < _size;
        }
    }
    private Board _board;
    private Camera _cam;
    private Vector3 Offset => new Vector3(_boardSize - 1, _boardSize - 1) * 0.5f;
    
    private void Start()
    {
       _board = new Board(_boardSize, _countToWin);
       _cam = Camera.main;
    }
    private void Update()
    {
        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                Mesh mesh = _board.Cells[x,y] == CellState.Empty ? _empty : _board.Cells[x,y] == CellState.Cross ? _cross : _circle;
                Material material = _board.Cells[x, y] == CellState.Empty ? _emptyMat : _board.Cells[x, y] == CellState.Cross ? _crossMat : _circleMat;
                float scale = _board.Cells[x, y] == CellState.Empty ? 0.9f : 0.7f;
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(new Vector3(x, y, 0) - Offset, Quaternion.identity, Vector3.one * scale);
                Graphics.DrawMesh(mesh, matrix, material, 0);
            }
        }
        
        CellState winner = _board.CheckWinner();

        if (winner != CellState.Empty)
            return;
        
        if (_maximize || Input.GetButtonDown("Jump"))
        {
            (int score, Vector2Int? move) = MinMax(_board, 0, int.MinValue, int.MaxValue, _maximize);
            if (move == null) return;
            _board.Cells[move.Value.x, move.Value.y] = _maximize ? CellState.Cross : CellState.Circle;
            _maximize = !_maximize;
            Debug.Log($"Move: {move}, with score {score}");
        }
        if(!_maximize && Input.GetButtonDown("Fire1"))
        {
            Vector3 pos = _cam.ScreenToWorldPoint(Input.mousePosition) + Offset;
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            if (_board.OnBoard(gridPos.x, gridPos.y) && _board.Cells[gridPos.x, gridPos.y] == CellState.Empty)
            {
                _board.Cells[gridPos.x, gridPos.y] = CellState.Circle;
                _maximize = !_maximize;
            }
        }
    }
    private (int, Vector2Int?) MinMax(Board board, int depth, int alpha, int beta, bool maximize)
    {
        //Generate possible moves
        List<Vector2Int> moves = new List<Vector2Int>();
        for(int y = 0; y < _boardSize; y++)
            for(int x = 0; x < _boardSize; x++)
                if(board.Cells[x,y] == CellState.Empty)
                    moves.Add(new Vector2Int(x,y));
        
        if (depth == _maxDepth || moves.Count == 0) //TODO: return heuristic value rather than zero, closer to center is better?
            return (0, null);
        
        //Check winner
        CellState winner = board.CheckWinner();
        if (winner == CellState.Circle)
            return (-1, null);
        if (winner == CellState.Cross)
            return (1, null);
        
        (int score, Vector2Int? move) best = (maximize ? int.MinValue : int.MaxValue, null);

        //iterate over possible moves
        foreach (Vector2Int move in moves)
        {
            board.Cells[move.x, move.y] = maximize ? CellState.Cross : CellState.Circle;
            (int score, Vector2Int? _) = MinMax(board, depth + 1, alpha, beta, !maximize);
            board.Cells[move.x, move.y] = CellState.Empty;

            if (maximize && score > best.score || !maximize && score < best.score)
                best = (score, move);
            
            //Apply alpha beta pruning
            if (maximize) alpha = Mathf.Max(alpha, score);
            else beta = Mathf.Min(beta, score);
            if (alpha >= beta)
                break;
        }
        return best;
    }
}
