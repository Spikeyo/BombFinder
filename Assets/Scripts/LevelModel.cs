using System;

/// <summary>
/// LevelModel contains the grid dimensions and number of bombs for a particular difficulty setting
/// </summary>
[Serializable]
public class LevelModel
{
    public int NumBombs;
    public int NumCols;
    public int NumRows;
}
