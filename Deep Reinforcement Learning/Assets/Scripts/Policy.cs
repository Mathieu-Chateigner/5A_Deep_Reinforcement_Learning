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

    public void InitializePolicy(List<State> states, GameManager gameManager)
    {
        foreach (State state in states)
        {
            List<Action> validActions = gameManager.GetValidActions(state);
            if (validActions.Count > 0)
            {
                int index = Random.Range(0, validActions.Count);
                policy[state] = validActions[index];  // Choix aléatoire d'une action valide
            }
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