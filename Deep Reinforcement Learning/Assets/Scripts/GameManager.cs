using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public State start;
    public State end;
    public List<State> obstacles;

    private Policy policy;
    private Dictionary<State, float> stateValues; // Used by PolicyEvaluation

    void Start()
    {
        InitializeObstacles();
        List<State> states = GenerateAllStates();

        start = new State(0, 0);
        end = new State(3, 3);
        policy = new Policy();
        policy.InitializePolicy(states, this);

        InitializeStateValues(states);
        PolicyEvaluation(states, 0.9f);

        printPolicy();
        printStateValues();

        PolicyImprovement(states);
        printPolicy();
    }

    void InitializeObstacles()
    {
        obstacles = new List<State>
        {
            // Ajoutez ici vos obstacles
            // Exemple : new State(1, 1), new State(2, 2)
        };
    }

    List<State> GenerateAllStates()
    {
        List<State> states = new List<State>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                State newState = new State(x, y);
                if (!obstacles.Contains(newState))
                {
                    states.Add(newState);
                }
            }
        }
        return states;
    }

    void InitializeStateValues(List<State> states)
    {
        stateValues = new Dictionary<State, float>();
        foreach (var state in states)
        {
            // Tous les états ont une valeur par défaut de 0, sauf l'état final qui a une valeur de 1
            stateValues[state] = state.Equals(end) ? 1f : 0f;
        }
    }

    /*void PolicyEvaluation(List<State> states, float discountFactor)
    {
        float theta = 0.01f; // Seuil pour déterminer quand arrêter l'itération
        float delta = 0f;
        do
        {
            delta = 0f;
            foreach (State state in states)
            {
                if (IsEnd(state)) continue;

                float oldValue = stateValues[state];

                Action action = policy.GetAction(state);
                State nextState = GetNextState(state, action);
                float reward = GetImmediateReward(state, action, nextState);
                float newValue = reward + (discountFactor * stateValues[nextState]);
                

                stateValues[state] = newValue;

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - newValue));
            }
        } while (delta > theta);
    }*/

    void PolicyEvaluation(List<State> states, float discountFactor)
    {
        float theta = 0.001f; // Seuil pour déterminer quand arrêter l'itération
        float delta = 0f;
        do
        {
            delta = 0f;
            foreach (State state in states)
            {
                if (IsEnd(state)) continue;

                float oldValue = stateValues[state];
                float valueSum = 0f;
                
                foreach (Action action in GetValidActions(state))
                {
                    State nextState = GetNextState(state, action);
                    float reward = GetImmediateReward(state, action, nextState);
                    valueSum += reward + (discountFactor * stateValues[nextState]);
                }

                // Assurez-vous de diviser par le nombre d'actions si vous souhaitez faire la moyenne
                float newValue = valueSum / GetValidActions(state).Count;
                stateValues[state] = newValue;

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - newValue));
            }
        } while (delta > theta);
    }

    public void PolicyImprovement(List<State> states)
    {
        foreach (State state in states)
        {
            if (IsEnd(state)) continue; // Aucune action requise pour les états terminaux

            Action bestAction = Action.Up; // Valeur par défaut, sera remplacée
            float bestValue = float.NegativeInfinity;

            foreach (Action action in GetValidActions(state))
            {
                State nextState = GetNextState(state, action);
                float value = stateValues[nextState];

                if (value > bestValue)
                {
                    bestValue = value;
                    bestAction = action;
                }
            }
            //Debug.Log("["+ state.X +","+ state.Y +"] =>"+ bestValue);
            Debug.Log("[" + state.X + "," + state.Y + "] =>" + GetValidActions(state).Count);
            // Mettre à jour la politique pour cet état avec la meilleure action trouvée
            policy.UpdatePolicy(state, bestAction);
        }
    }

    public float GetImmediateReward(State currentState, Action action, State nextState)
    {
        if (nextState.Equals(end))
        {
            return 1.0f;
        }
        else
        {
            return -.05f;
        }
    }

    // deprecated
    public float GetRewardByPolicy(State state)
    {
        State nextStateByPolicy = GetNextState(state, policy.GetAction(state));
        if (!stateValues.ContainsKey(nextStateByPolicy)) return 0f;
        return stateValues[nextStateByPolicy];
    }

    // deprecated
    private float SumOfNextStateValues(State state)
    {
        float sum = 0f;

        // Pour un modèle déterministe (prendre en compte les probalités de transition pour un modèle stochastique)
        foreach (Action action in System.Enum.GetValues(typeof(Action)))
        {
            State nextState = GetNextState(state, action);

            if (!obstacles.Contains(nextState) && !nextState.Equals(state))
            {
                sum += stateValues[nextState];
            }
        }
        return sum;
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

    public List<Action> GetValidActions(State state)
    {
        List<Action> validActions = new List<Action>();

        if (state.Y < gridSize.y - 1) validActions.Add(Action.Up); // Peut aller vers le haut
        if (state.X < gridSize.x - 1) validActions.Add(Action.Right); // Peut aller vers la droite
        if (state.Y > 0) validActions.Add(Action.Down); // Peut aller vers le bas
        if (state.X > 0) validActions.Add(Action.Left); // Peut aller vers la gauche

        // Check aussi les obstacles

        return validActions;
    }

    public bool IsEnd(State state)
    {
        return state.Equals(end);
    }

    private void printPolicy()
    {
        string gridPolicy = "Grid Policy:\n";
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < gridSize.x; x++)
            {
                State state = new State(x, y);
                string value = "X";
                Action stateAction = policy.GetAction(state);
                switch (stateAction)
                {
                    case Action.Up:
                        value = "^";
                        break;
                    case Action.Right:
                        value = ">";
                        break;
                    case Action.Down:
                        value = "v";
                        break;
                    case Action.Left:
                        value = "<";
                        break;
                }
                line += value + "\t";
            }
            gridPolicy += line + "\n"; // Ajoutez la ligne à la représentation de la grille
        }
        Debug.Log(gridPolicy); // Affichez la grille dans la console Unity
    }

    private void printStateValues()
    {
        string gridRepresentation = "Grid State Values:\n";
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < gridSize.x; x++)
            {
                State state = new State(x, y);
                float value = stateValues.ContainsKey(state) ? stateValues[state] : 0f; // Obtenez la valeur de l'état, ou 0 si non défini
                line += value.ToString("F2") + "\t";
            }
            gridRepresentation += line + "\n"; // Ajoutez la ligne à la représentation de la grille
        }
        Debug.Log(gridRepresentation); // Affichez la grille dans la console Unity
    }
}
