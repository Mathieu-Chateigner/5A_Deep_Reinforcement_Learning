using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Action
{
    Up,
    Right,
    Down,
    Left
}

public class Policy
{
    private readonly Dictionary<State, Action> _policy = new ();
    private Tilemap _tilemap;
    private List<Tile> _tileList;

    public IEnumerator InitializePolicy(List<State> states, GameManager gameManager, Tilemap tilemap, List<Tile> tileList)
    {
        _tilemap = tilemap;
        _tileList = tileList;
        
        foreach (var state in states)
        {
            var validActions = gameManager.GetValidActions(state);
            if (validActions.Count <= 0) continue;
            var index = Random.Range(0, validActions.Count);
            var action = validActions[index];
            _policy[state] = action;  // Choix aléatoire d'une action valide
            tilemap.SetTile(new Vector3Int(state.X, state.Y, 0), GetTileFromAction(action));
            yield return new WaitForSeconds(0.1f);
        }
    }

    public Action GetAction(State state)
    {
        return _policy.GetValueOrDefault(state, Action.Up); // Valeur par défaut si l'état n'est pas trouvé
    }

    public void UpdatePolicy(State state, Action action)
    {
        _policy[state] = action;
        _tilemap.SetTile(new Vector3Int(state.X, state.Y, 0), GetTileFromAction(action));
    }

    private Tile GetTileFromAction(Action action)
    {
        return action switch
        {
            Action.Down => _tileList.First(tile => tile.name.Equals("arrow_down")),
            Action.Up => _tileList.First(tile => tile.name.Equals("arrow_up")),
            Action.Left => _tileList.First(tile => tile.name.Equals("arrow_left")),
            Action.Right => _tileList.First(tile => tile.name.Equals("arrow_right")),
            _ => _tileList.First(tile => tile.name.Equals("question_mark"))
        };
    }
}