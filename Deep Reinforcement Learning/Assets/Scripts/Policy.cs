using System.Collections;
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
    private Dictionary<State, Action> policy = new Dictionary<State, Action>();

    public void InitializePolicy(List<State> states)
    {
        foreach (var state in states)
        {
            policy[state] = (Action)Random.Range(0, 4);  // Choix aléatoire d'une action
        }
    }

    public Action GetAction(State state)
    {
        if (policy.ContainsKey(state))
        {
            return policy[state];
        }
        return Action.Up;  // Valeur par défaut si l'état n'est pas trouvé
    }

    public void UpdatePolicy(State state, Action action)
    {
        if (policy.ContainsKey(state))
        {
            policy[state] = action;
        }
        else
        {
            policy.Add(state, action);
        }
    }
}