using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cell contains the states of a particular square and a list of all its neighbouring cells
/// </summary>
public class Cell : MonoBehaviour
{
	public SpriteRenderer SpriteRenderer;
	
	public readonly List<Cell> NeighbouringCells = new List<Cell>();
	
	public int NumNeighbouringBombs { get; set; }
	
	public bool IsHidden  { get; set; }
	public bool IsFlagged { get; set; }
	public bool HasBomb   { get; set; }
}
