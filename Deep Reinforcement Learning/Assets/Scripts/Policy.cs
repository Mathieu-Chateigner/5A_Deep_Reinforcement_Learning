using System.Collections.Generic;
using UnityEngine;

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
    private GameManager _gm;

    public void InitializePolicy(List<State> states, GameManager gameManager)
    {
        _gm = gameManager;
        foreach (var state in states)
        {
            var validActions = gameManager.GetValidActions(state);
            if (validActions.Count <= 0) continue;
            var index = Random.Range(0, validActions.Count);
            var action = validActions[index];
            _policy[state] = action;  // Choix aléatoire d'une action valide
        }
        _gm.UpdateTilemap(_policy);
    }

    public Action GetAction(State state)
    {
        return _policy.GetValueOrDefault(state, Action.Up); // Valeur par défaut si l'état n'est pas trouvé
    }

    public Dictionary<State, Action> GetPolicy()
    {
        return _policy;
    }

    public void UpdatePolicy(State state, Action action)
    {
        _policy[state] = action;
    }
}