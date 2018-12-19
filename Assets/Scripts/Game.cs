using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Game is responsible for the game logic, including testing for victory/defeat conditions,
/// creating and destroying cells and setting their state.
/// </summary>
public class Game : MonoBehaviour
{
    private const float CellSize = 0.48f;

    private static readonly Random RandomGen = new Random();
    
    #region Prefab and Sprites fields
    
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private ParticleSystem explosionPrefab;
    
    [SerializeField] private Sprite hiddenSprite;
    [SerializeField] private Sprite flagSprite;
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite[] numberSprites;
    
    #endregion

    [SerializeField] private float delayBetweenExplosions;
    
    private LevelModel model;
    
    private Cell nextCellToExplode;
    
    private readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
    private readonly List<Cell> cellsWithBombs = new List<Cell>();
    
    public delegate void GameEndHandler();

    public event GameEndHandler GameLost;
    public event GameEndHandler GameWon;
    
    public int NumRevealedCells { get; private set; }
    public bool IsGameRunning { get; set; }

    public void Build(LevelModel model)
    {
        this.model = model;
        
        if (cells.Count > 0)
            DestroyCurrentCells();

        CreateCells();
        
        foreach (var position in cells.Keys)
        {
            PopulateNeighbouringCellsList(position);
        }
    }
    
    public void Play()
    {
        NumRevealedCells = 0;
        
        cellsWithBombs.Clear();

        foreach (var cell in cells.Values)
        {
            ResetCell(cell);
        }
        
        IsGameRunning = true;
    }
    
    public void HandleCellRightClick(Cell cell)
    {
        if (!cell.IsHidden)
            return;
        
        cell.IsFlagged = !cell.IsFlagged;
        
        cell.SpriteRenderer.sprite = cell.IsFlagged ? flagSprite : hiddenSprite;
    }

    public void HandleCellLeftClick(Cell cell)
    {
        if (cell.IsFlagged || !cell.IsHidden) return;

        // If this was the first cell to be clicked, add bombs to other, random cells.
        // This way, the first cell to be clicked can never be a bomb.
        if (NumRevealedCells == 0)
            AddRandomBombs(cell);
        
        RevealCell(cell);
    }
    
    private void DestroyCurrentCells()
    {
        foreach (var cell in cells.Values)
        {
            Destroy(cell.gameObject);
        }
        
        cells.Clear();
    }
    
    private void CreateCells()
    {
        var offset = new Vector2((CellSize * model.NumCols) / 2, (CellSize * model.NumRows) / 2);
                     
        for (int i = 0; i < model.NumCols; i++)
        {
            for (int j = 0; j < model.NumRows; j++)
            {
                CreateCell(i, j, offset);
            }
        }
    }

    private void CreateCell(int col, int row, Vector2 offset)
    {
        var cell = Instantiate(cellPrefab, this.transform);

        cells[new Vector2Int(col, row)] = cell;
        
        cell.transform.position = new Vector2
        (
            (CellSize * (2 * col + 1)) / 2 - offset.x, 
            (CellSize * (2 * row + 1)) / 2 - offset.y
        );
        
        cell.name = "Cell " + col + ", " + row;
    }
    
    private void ResetCell(Cell cell)
    {
        cell.HasBomb = false;
        cell.IsFlagged = false;
        cell.IsHidden = true;

        cell.SpriteRenderer.sprite = hiddenSprite;
    }
    
    private void PopulateNeighbouringCellsList(Vector2Int position)
    {
        int col = position.x;
        int row = position.y;
        var cell = cells[position];
        
        if (col > 0)
        {
            cell.NeighbouringCells.Add(cells[new Vector2Int(col - 1, row)]);
            if (row > 0)
            {
                cell.NeighbouringCells.Add(cells[new Vector2Int(col - 1, row - 1)]);
            }
            if (row < model.NumRows - 1)
            {
                cell.NeighbouringCells.Add(cells[new Vector2Int(col - 1, row + 1)]);
            }
        }
        
        if (col < model.NumCols - 1)
        {
            cell.NeighbouringCells.Add(cells[new Vector2Int(col + 1, row)]);
            if (row > 0)
            {
                cell.NeighbouringCells.Add(cells[new Vector2Int(col + 1, row - 1)]);
            }
            if (row < model.NumRows - 1)
            {
                cell.NeighbouringCells.Add(cells[new Vector2Int(col + 1, row + 1)]);
            }
        }
        
        if (row > 0)
        {
            cell.NeighbouringCells.Add(cells[new Vector2Int(col, row - 1)]);
        }
        if (row < model.NumRows - 1)
        {
            cell.NeighbouringCells.Add(cells[new Vector2Int(col, row + 1)]);
        }
    }

    
    private void AddRandomBombs(Cell clickedCell)
    {
        var cellList = cells.Values.ToList();
        
        //sets bombs in random cells that aren't the clickedCell
        for (int i = 0; i < model.NumBombs; i++)
        {
            var randomCell = cellList[RandomGen.Next(cellList.Count)];

            if (randomCell.HasBomb || randomCell == clickedCell)
            {
                i--;
                continue;
            }

            randomCell.HasBomb = true;
            cellsWithBombs.Add(randomCell);
        }
        
        // set the NumNeighbouringBombs value of each cell
        cellList.ForEach(x => x.NumNeighbouringBombs = x.NeighbouringCells.Count(y => y.HasBomb));
    }
    
    private void RevealCell(Cell cell)
    {
        NumRevealedCells++;
        cell.IsHidden = false;
        
        if (cell.HasBomb)
        {
            RevealBomb(cell);
        }
        else
        {
            RevealNumber(cell);
        }
    }

    private void RevealBomb(Cell cell)
    {
        cell.SpriteRenderer.sprite = bombSprite;
        
        // If IsGameRunning is false, it means that another bomb has already been revealed and that
        // the explosion sequence has started. After the first bomb reveal, revelation of all other cells
        // is done instantly but explosions are triggered between timed intervals.
        if (!IsGameRunning)
            return;
        
        IsGameRunning = false;
            
        foreach (var c in cells.Values) 
        {
            if (c.IsHidden)
                RevealCell(c);
        }
            
        nextCellToExplode = cell;
        ExplodeNextBomb();
    }
    
    private void RevealNumber(Cell cell)
    {
        cell.SpriteRenderer.sprite = numberSprites[cell.NumNeighbouringBombs];
        
        // if a cell contains no neighbouring bombs it automatically reveals all unflagged neighbouring cells
        if (cell.NumNeighbouringBombs == 0)
        {
            foreach (var neighbour in cell.NeighbouringCells)
            {
                if (neighbour.IsHidden && !neighbour.IsFlagged)
                    RevealCell(neighbour);
            }
        }
        
        CheckForVictory();
    }

    private void ExplodeNextBomb()
    {
        cellsWithBombs.Remove(nextCellToExplode);
        var explosion = Instantiate(explosionPrefab);
        Destroy(explosion.gameObject, explosionPrefab.main.duration);
        explosion.transform.position = nextCellToExplode.transform.position - new Vector3(0, 0, 0.1f);
         
        if (cellsWithBombs.Count > 0)
        {
            nextCellToExplode = cellsWithBombs[RandomGen.Next(cellsWithBombs.Count)];
            
            Invoke("ExplodeNextBomb", delayBetweenExplosions);
        }
        else
        {
            GameLost.Invoke();
        }
    }
    
    private void CheckForVictory()
    {
        if (IsGameRunning && NumRevealedCells == cells.Count - model.NumBombs)
        {
            IsGameRunning = false;
            GameWon.Invoke();
        }
    }
}