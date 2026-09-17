using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TableBlock
{
    public List<Table> Tables { get; private set; } = new List<Table>();

    public int Capacity => Tables.Sum(t => t.Capacity);

    /// <summary>Actually bookable seats right now. Regular tables: full capacity
    /// while free. Bar stands: only the stools not yet claimed by other groups
    /// (a counter block stays open until every stool is taken).</summary>
    public int FreeCapacity => Tables.Sum(t => t.IsBarStand ? t.FreeSeats : t.Capacity);

    // IsRebookable: mesa liberada hace poco cuyos clientes siguen de pie al
    // lado levantándose/reaccionando — bloquea el reasiento (barra no, los
    // taburetes se reutilizan al instante a propósito).
    public bool IsOccupied => Tables.Any(t => t.IsOccupied || !t.IsRebookable);

    public void AddTable(Table table)
    {
        if (!Tables.Contains(table))
            Tables.Add(table);
    }
}
