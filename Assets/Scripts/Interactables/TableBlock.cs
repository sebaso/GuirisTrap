using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TableBlock
{
    public List<Table> Tables { get; private set; } = new List<Table>();

    public int Capacity => Tables.Sum(t => t.Capacity);

    public bool IsOccupied => Tables.Any(t => t.IsOccupied);

    public void AddTable(Table table)
    {
        if (!Tables.Contains(table))
            Tables.Add(table);
    }
}
