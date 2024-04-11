using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map
{
    public Vector2Int size { get; private set; }
    public List<Vector2Int> obstacles { get; private set; } // Walls
    public List<Vector2Int> targets { get; private set; } // Arrivée ou positions où les boîtes doivent être déplacées
    public State startState { get; private set; }
    public State endState { get; private set; }

    public Map(Vector2Int size, List<Vector2Int> obstacles, List<Vector2Int> targets, State startState, State endState)
    {
        this.size = size;
        this.obstacles= obstacles;
        this.targets = targets;
        this.startState = startState;
        this.endState = endState;
    }
}
