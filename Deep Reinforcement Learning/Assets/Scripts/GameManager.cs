using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public State start;
    public State end;
    public List<State> obstacles;

    public bool IsTerminal(State state)
    {
        return state.Equals(end);
    }

    public State GetNextState(State state, Action action)
    {
        State nextState = new State(state.X, state.Y);

        switch (action)
        {
            case Action.Up:
                nextState.Y = Mathf.Max(nextState.Y - 1, 0);
                break;
            case Action.Right:
                nextState.X = Mathf.Min(nextState.X + 1, gridSize.x - 1);
                break;
            case Action.Down:
                nextState.Y = Mathf.Min(nextState.Y + 1, gridSize.y - 1);
                break;
            case Action.Left:
                nextState.X = Mathf.Max(nextState.X - 1, 0);
                break;
        }

        if (obstacles.Contains(nextState))
        {
            return state;
        }

        return nextState;
    }

    public int GetReward(State state, State nextState)
    {
        if (nextState.Equals(end))
        {
            return 100;  // Récompense pour atteindre la fin
        }
        return -1;  // Petite pénalité pour chaque mouvement
    }
}
