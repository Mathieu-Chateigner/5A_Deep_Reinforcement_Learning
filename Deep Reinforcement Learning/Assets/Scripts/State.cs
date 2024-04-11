using System;
using System.Collections.Generic;
using UnityEngine;

public enum Game
{
    GridWorld,
    Sokoban
}

public class State
{
    public Game game;
    public int X { get; set; }
    public int Y { get; set; }

    public Vector2Int player;
    public List<Vector2Int> crates;

    // GridWorld
    public State(int x, int y)
    {
        game = Game.GridWorld;
        X = x;
        Y = y;
    }

    // Sokoban
    public State(Vector2Int playerPosition, List<Vector2Int> cratePositions)
    {
        game = Game.Sokoban;
        player = playerPosition;
        crates = cratePositions;
    }

    public override bool Equals(object other)
    {
        if (!(other is State state)) return false;
        if(game == Game.GridWorld)
        {
            return X == state.X && Y == state.Y;
        }
        else if(game == Game.Sokoban)
        {
            if (player != state.player) return false; // Player position
            if (crates.Count != state.crates.Count) return false; // Crate number
            foreach(Vector2Int c in crates) {
                if(!state.crates.Contains(c)) // Crates position
                {
                    return false;
                }
            }
            return true;
        }
        
        return false;
    }

    public override int GetHashCode()
    {
        if(game == Game.GridWorld)
        {
            return HashCode.Combine(X, Y);
        }
        else if(game == Game.Sokoban)
        {
            int hash = player.GetHashCode();
            foreach (Vector2Int crate in crates)
            {
                hash = HashCode.Combine(hash, crate.GetHashCode());
            }
            return hash;
        }
        return 0;
    }
}