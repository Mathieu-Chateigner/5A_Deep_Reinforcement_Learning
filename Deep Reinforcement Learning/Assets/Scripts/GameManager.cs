using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public bool usePolicyIteration;
    public bool useValueIteration;
    public bool useMonteCarlo;

    public State start;
    public State end;
    public List<State> obstacles;

    private Policy policy;
    private Dictionary<State, float> stateValues; // Used by PolicyIteration and ValueIteration

    private Dictionary<State, float> returnsSum;
    private new Dictionary<State, int> returnsCount; // Used by MonteCarlo

    void Start()
    {
        InitializeObstacles();
        List<State> states = GenerateAllStates();

        start = new State(0, 0);
        end = new State(3, 3);
        policy = new Policy();
        policy.InitializePolicy(states, this);

        InitializeStateValues(states);

        //printCoordinates();
        printPolicy();

        if(usePolicyIteration)
        {
            PolicyIteration(states, 0.9f); // Policy iteration
        }
        else if(useValueIteration)
        {
            ValueIteration(states, 0.9f); // Value iteration
        }
        else if(useMonteCarlo)
        {
            stateValues = MonteCarloFirstVisitOnPolicy(states, 20);
        }

        printPossibleActions();
        printStateValues();
        printPolicy();
    }

    void InitializeObstacles()
    {
        obstacles = new List<State>
        {
            // On ajoute nos obstacles içi
            new State(3, 2),
            new State(2, 2),
            new State(4, 2),
            new State(2, 3),
            new State(4, 3),
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

    void PolicyIteration(List<State> states, float discountFactor)
    {
        bool policyStable = false;
        do
        {
            PolicyEvaluation(states, discountFactor);

            policyStable = PolicyImprovement(states);
        } while (!policyStable);
    }

    void PolicyEvaluation(List<State> states, float discountFactor)
    {
        float theta = 0.001f; // Seuil pour déterminer quand arrêter l'itération
        float delta;
        do
        {
            delta = 0f;
            foreach (State state in states)
            {
                if (IsEnd(state)) continue;

                float oldValue = stateValues[state];

                Action action = policy.GetAction(state);
                State nextState = GetNextState(state, action);
                float reward = IsEnd(nextState) ? 0f : GetImmediateReward(state, action);
                stateValues[state] = reward + (discountFactor * stateValues[nextState]);

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - stateValues[state]));
            }
        } while (delta > theta);
    }

    public bool PolicyImprovement(List<State> states)
    {
        bool policyStable = true;

        foreach (State state in states)
        {
            if (IsEnd(state)) continue; // Aucune action requise pour les états terminaux

            Action oldAction = policy.GetAction(state);
            float maxValue = float.NegativeInfinity;
            Action bestAction = oldAction; // Default
            foreach (Action action in GetValidActions(state))
            {
                State nextState = GetNextState(state, action);
                float value = stateValues[nextState];

                if (value > maxValue)
                {
                    maxValue = value;
                    bestAction = action;
                }
            }

            if (oldAction != bestAction)
            {
                policyStable = false;
                policy.UpdatePolicy(state, bestAction);
            }
        }

        return policyStable;
    }

    // OK
    void ValueIteration(List<State> states, float discountFactor)
    {
        float theta = 0.001f; // Seuil pour déterminer quand arrêter l'itération
        float delta;
        do
        {
            delta = 0f;
            foreach (State state in states)
            {
                if (IsEnd(state)) continue;

                float oldValue = stateValues[state];

                // On prend seulement le max des actions possibles
                float maxValue = float.NegativeInfinity;
                Action bestAction = Action.Up; // Default
                foreach (Action action in GetValidActions(state))
                {
                    State nextState = GetNextState(state, action);

                    float reward = IsEnd(nextState) ? 0f : GetImmediateReward(state, action);
                    float value = reward + (discountFactor * stateValues[nextState]);
                    if (value > maxValue)
                    {
                        maxValue = value;
                        bestAction = action;
                    }
                }
                
                stateValues[state] = maxValue;

                // Update policy
                policy.UpdatePolicy(state, bestAction);

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - maxValue));
            }
        } while (delta > theta);
    }

    public Dictionary<State, float> MonteCarloFirstVisitOnPolicy(List<State> states, int numEpisode)
    {
        returnsSum = new Dictionary<State, float>();
        returnsCount = new Dictionary<State, int>();

        foreach (var state in states)
        {
            returnsSum[state] = 0.0f;
            returnsCount[state] = 0;
        }

        for (int e = 0; e < numEpisode; e++)
        {
            // Generate using policy, an episode State0, Action0, Reward0, State1, Action1, Reward1, ..., StateT, ActionT, RewardT
            // G = 0
            // for timeStep t = T - 1 to t = 0 (of the episode e) do {
            //      G  = G + RewardT+1
            //      if StateT is not the sequence State0, State1, ..., StateT-1 then {
            //          returnsSum[StateT] = returnsSum[StateT] + G
            //          returnsCount[StateT] = returnsCount[StateT] + 1
            //      }
            // }

            List<(State, Action, float)> episode = GenerateEpisode(); // Simulation

            float G = 0;
            HashSet<State> visitedStates = new HashSet<State>();

            // Parcourir l'épisode de la fin au début
            for (int t = episode.Count - 1; t >= 0; t--)
            {
                G = 0.9f * G + episode[t].Item3; // Récompense à t

                State stateT = episode[t].Item1;

                // First-Visit : si l'état n'a pas été déjà visité dans cet épisode
                if (!visitedStates.Contains(stateT))
                {
                    returnsSum[stateT] += G;
                    returnsCount[stateT] += 1;
                    visitedStates.Add(stateT);
                }
            }

            Dictionary<State, float> values = new Dictionary<State, float>();
            foreach (State state in returnsSum.Keys)
            {
                if (IsEnd(state)) continue;
                if (returnsCount[state] > 0)
                {
                    values[state] = returnsSum[state] / returnsCount[state];
                }
            }
            UpdatePolicyBasedOnStateValues(states, values); // On policy
        }

        // Compute average
        Dictionary<State, float> stateValues = new Dictionary<State, float>();
        foreach (State state in returnsSum.Keys)
        {
            if (IsEnd(state)) continue;
            if(returnsCount[state] > 0)
            {
                stateValues[state] = returnsSum[state] / returnsCount[state];
            }
        }
        return stateValues;
    }

    private List<(State, Action, float)> GenerateEpisode()
    {
        List<(State, Action, float)> episode = new List<(State, Action, float)>();
        State currentState = start; // Commencez à l'état de départ
        HashSet<State> visitedStates = new HashSet<State>(); // Pour détecter les boucles

        int step = 0;
        while (!IsEnd(currentState))
        {
            Action action;
            List<Action> validActions = GetValidActions(currentState);

            if (Random.value < 0.9f) // Exploration avec une probabilité epsilon
            {
                action = validActions[Random.Range(0, validActions.Count)];
            }
            else // Exploitation
            {
                action = policy.GetAction(currentState);
            }

            //action = validActions[Random.Range(0, validActions.Count)];
            //action = policy.GetAction(currentState); // Suit la politique
            
            State nextState = GetNextState(currentState, action);
            float reward = GetImmediateReward(currentState, action);

            // Boucle détectée ou état final impossible
            if (step++ > 100)
            {
                episode.Add((currentState, default(Action), -1f)); // Echec
                break;
            }

            episode.Add((currentState, action, reward));

            currentState = nextState; // Mettez à jour l'état courant pour la prochaine itération
            
            if (IsEnd(currentState))
            {
                episode.Add((currentState, default(Action), 1f));
                break;
            }
        }

        return episode;
    }

    void UpdatePolicyBasedOnStateValues(List<State> states, Dictionary<State, float> values)
    {
        foreach (State state in states)
        {
            if (!IsEnd(state))
            {
                List<Action> validActions = GetValidActions(state);
                float maxValue = float.NegativeInfinity;
                Action bestAction = Action.Up;

                foreach (Action action in validActions)
                {
                    State nextState = GetNextState(state, action);
                    float value = values.ContainsKey(nextState) ? values[nextState] : 0f;
                    if (value > maxValue)
                    {
                        maxValue = value;
                        bestAction = action;
                    }
                }
                policy.UpdatePolicy(state, bestAction);
            }
        }
    }

    public float GetImmediateReward(State currentState, Action action)
    {
        State nextState = GetNextState(currentState, action);
        if (nextState.Equals(end))
        {
            return 1.0f;
        }
        else if (obstacles.Contains(nextState))
        {
            return 0.0f;
        }
        else
        {
            return 0.0f;
        }
    }
    
    public State GetNextState(State state, Action action)
    {
        State nextState = new State(state.X, state.Y);

        switch (action)
        {
            case Action.Up:
                nextState.Y = nextState.Y + 1;
                break;
            case Action.Right:
                nextState.X = nextState.X + 1;
                break;
            case Action.Down:
                nextState.Y = nextState.Y - 1;
                break;
            case Action.Left:
                nextState.X = nextState.X - 1;
                break;
        }

        /*if (obstacles.Contains(nextState))
        {
            return state;
        }*/

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
        if (obstacles.Contains(GetNextState(state, Action.Up)))
        {
            validActions.Remove(Action.Up);
        }
        if (obstacles.Contains(GetNextState(state, Action.Right)))
        {
            validActions.Remove(Action.Right);
        }
        if (obstacles.Contains(GetNextState(state, Action.Down)))
        {
            validActions.Remove(Action.Down);
        }
        if (obstacles.Contains(GetNextState(state, Action.Left)))
        {
            validActions.Remove(Action.Left);
        }

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
            gridPolicy += line + "\n";
        }
        Debug.Log(gridPolicy);
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
                float value = stateValues.ContainsKey(state) ? stateValues[state] : 0f;
                line += value.ToString("F2") + "\t";
            }
            gridRepresentation += line + "\n";
        }
        Debug.Log(gridRepresentation);
    }

    private void printCoordinates()
    {
        string gridCoordinates = "Grid Coordinates:\n";
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < gridSize.x; x++)
            {
                State state = new State(x, y);
                line += state.X +","+ state.Y +"\t";
            }
            gridCoordinates += line + "\n";
        }
        Debug.Log(gridCoordinates);
    }

    private void printPossibleActions()
    {
        string grid = "Grid Possible Actions:\n";
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < gridSize.x; x++)
            {
                State state = new State(x, y);
                int count = GetValidActions(state).Count;
                if (obstacles.Contains(state))
                {
                    count = 0;
                }
                line += count + "\t";
            }
            grid += line + "\n";
        }
        Debug.Log(grid);
    }
}
