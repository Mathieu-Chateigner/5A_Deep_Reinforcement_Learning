using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public Camera camera;
    public Button buttonPolicyIteration;
    public Button buttonValueIteration;

    public bool usePolicyIteration;
    public bool useValueIteration;
    public bool useMonteCarlo;

    private State _start;
    private State _end;
    private List<State> _obstacles;
    private Policy _policy;
    private Dictionary<State, float> _stateValues; // Used by PolicyIteration and ValueIteration
    private List<State> _states;
    private Dictionary<State, float> returnsSum; // Used by MonteCarlo
    private Dictionary<State, int> returnsCount; // Used by MonteCarlo

    private void Start()
    {
        camera.transform.position = new Vector3(gridSize.x/2, gridSize.y/2, -10);
        InitializeObstacles();
        _states = GenerateAllStates();

        TilemapManager.Instance.StartTilemap(_states);

        _start = new State(0, 0);
        _end = new State(8, 8);

        TilemapManager.Instance.SetStartingValues(_start, _end);

        _policy = new Policy();
        _policy.InitializePolicy(_states, this);

        InitializeStateValues();

        PrintCoordinates();
        PrintPolicy();

        /*if(usePolicyIteration)
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
        }*/

        PrintPossibleActions();
        PrintStateValues();
        PrintPolicy();
    }

    private void InitializeObstacles()
    {
        _obstacles = new List<State>
        {
            // On ajoute nos obstacles i�i
            //new(3, 2),
            new(2, 2),
            new(4, 2),
            new(2, 3),
            new(4, 3),
            new(2, 2),
            new(5, 8),
            new(7, 6),
            new(9, 9),
            new(1, 7),
            new(9, 3),
            //new State(4, 0)
        };

        UpdateTilemapObstacles();
    }

    public void UpdateTilemap(Dictionary<State, Action> policy)
    {
        StartCoroutine(TilemapManager.Instance.UpdateTilemap(policy, () =>
        {
            buttonPolicyIteration.interactable = true;
            buttonValueIteration.interactable = true;
        }));
    }

    public void UpdateTilemapObstacles()
    {
        StartCoroutine(TilemapManager.Instance.UpdateTilemapObstacles(_obstacles));
    }

    private List<State> GenerateAllStates()
    {
        var states = new List<State>();
        for (var x = 0; x < gridSize.x; x++)
        {
            for (var y = 0; y < gridSize.y; y++)
            {
                var newState = new State(x, y);
                if (_obstacles.Contains(newState)) continue;
                states.Add(newState);
                //tilemapGridWorld.SetTile(new Vector3Int(x, y, 0), tileList.First(tile => tile.name.Equals("question_mark")));
            }
        }
        return states;
    }

    private void InitializeStateValues()
    {
        _stateValues = new Dictionary<State, float>();
        foreach (var state in _states)
        {
            // Tous les �tats ont une valeur par d�faut de 0, sauf l'�tat final qui a une valeur de 1
            _stateValues[state] = state.Equals(_end) ? 1f : 0f;
        }
    }

    public void PolicyIteration(float discountFactor)
    {
        bool policyStable = false;
        do
        {
            PolicyEvaluation(discountFactor);

            policyStable = PolicyImprovement();
        } while (!policyStable);

        UpdateTilemap(_policy.GetPolicy());
    }

    private void PolicyEvaluation(float discountFactor)
    {
        float theta = 0.001f; // Seuil pour d�terminer quand arr�ter l'it�ration
        float delta;
        do
        {
            delta = 0f;
            foreach (State state in _states)
            {
                if (IsEnd(state)) continue;

                float oldValue = _stateValues[state];

                Action action = _policy.GetAction(state);
                State nextState = GetNextState(state, action);
                float reward = IsEnd(nextState) ? 0f : GetImmediateReward(state, action);
                _stateValues[state] = reward + (discountFactor * _stateValues[nextState]);

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - _stateValues[state]));
            }
        } while (delta > theta);
    }

    public bool PolicyImprovement()
    {
        bool policyStable = true;

        foreach (State state in _states)
        {
            if (IsEnd(state)) continue; // Aucune action requise pour les �tats terminaux

            Action oldAction = _policy.GetAction(state);
            float maxValue = float.NegativeInfinity;
            Action bestAction = oldAction; // Default
            foreach (Action action in GetValidActions(state))
            {
                State nextState = GetNextState(state, action);
                float value = _stateValues[nextState];

                if (value > maxValue)
                {
                    maxValue = value;
                    bestAction = action;
                }
            }

            if (oldAction != bestAction)
            {
                policyStable = false;
                _policy.UpdatePolicy(state, bestAction);
            }
        }

        return policyStable;
    }

    // OK
    public void ValueIteration(float discountFactor)
    {
        const float theta = 0.001f; // Seuil pour d�terminer quand arr�ter l'it�ration
        float delta;
        do
        {
            delta = 0f;
            foreach (var state in _states)
            {
                if (IsEnd(state)) continue;

                var oldValue = _stateValues[state];

                // On prend seulement le max des actions possibles
                float maxValue = float.NegativeInfinity;
                Action bestAction = Action.Up; // Default
                foreach (Action action in GetValidActions(state))
                {
                    var nextState = GetNextState(state, action);

                    float reward = IsEnd(nextState) ? 0f : GetImmediateReward(state, action);
                    float value = reward + (discountFactor * _stateValues[nextState]);
                    if (value > maxValue)
                    {
                        maxValue = value;
                        bestAction = action;
                    }
                }

                _stateValues[state] = maxValue;

                // Update policy
                _policy.UpdatePolicy(state, bestAction);

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - maxValue));
            }
        } while (delta > theta);
        UpdateTilemap(_policy.GetPolicy());
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

            // Parcourir l'�pisode de la fin au d�but
            for (int t = episode.Count - 1; t >= 0; t--)
            {
                G = 0.9f * G + episode[t].Item3; // R�compense � t

                State stateT = episode[t].Item1;

                // First-Visit : si l'�tat n'a pas �t� d�j� visit� dans cet �pisode
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
        State currentState = _start; // Commencez � l'�tat de d�part
        HashSet<State> visitedStates = new HashSet<State>(); // Pour d�tecter les boucles

        int step = 0;
        while (!IsEnd(currentState))
        {
            Action action;
            List<Action> validActions = GetValidActions(currentState);

            if (Random.value < 0.9f) // Exploration avec une probabilit� epsilon
            {
                action = validActions[Random.Range(0, validActions.Count)];
            }
            else // Exploitation
            {
                action = _policy.GetAction(currentState);
            }

            //action = validActions[Random.Range(0, validActions.Count)];
            //action = policy.GetAction(currentState); // Suit la politique

            State nextState = GetNextState(currentState, action);
            float reward = GetImmediateReward(currentState, action);

            // Boucle d�tect�e ou �tat final impossible
            if (step++ > 100)
            {
                episode.Add((currentState, default(Action), -1f)); // Echec
                break;
            }

            episode.Add((currentState, action, reward));

            currentState = nextState; // Mettez � jour l'�tat courant pour la prochaine it�ration

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
                _policy.UpdatePolicy(state, bestAction);
            }
        }
    }

    public float GetImmediateReward(State currentState, Action action)
    {
        var nextState = GetNextState(currentState, action);

        if (nextState.Equals(_end))
        {
            return 1.0f;
        }

        if (_obstacles.Contains(nextState))
        {
            return 0.0f;
        }

        return 0.0f;
    }

    private State GetNextState(State state, Action action)
    {
        var nextState = new State(state.X, state.Y);

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
            default:
                break;
        }

        /*if (_obstacles.Contains(nextState))
        {
            return state;
        }*/

        return nextState;
    }

    public List<Action> GetValidActions(State state)
    {
        var validActions = new List<Action>();

        if (state.Y < gridSize.y - 1) validActions.Add(Action.Up); // Peut aller vers le haut
        if (state.X < gridSize.x - 1) validActions.Add(Action.Right); // Peut aller vers la droite
        if (state.Y > 0) validActions.Add(Action.Down); // Peut aller vers le bas
        if (state.X > 0) validActions.Add(Action.Left); // Peut aller vers la gauche

        // Check aussi les obstacles
        if (_obstacles.Contains(GetNextState(state, Action.Up)))
        {
            validActions.Remove(Action.Up);
        }
        if (_obstacles.Contains(GetNextState(state, Action.Right)))
        {
            validActions.Remove(Action.Right);
        }
        if (_obstacles.Contains(GetNextState(state, Action.Down)))
        {
            validActions.Remove(Action.Down);
        }
        if (_obstacles.Contains(GetNextState(state, Action.Left)))
        {
            validActions.Remove(Action.Left);
        }

        return validActions;
    }

    private bool IsEnd(State state)
    {
        return state.Equals(_end);
    }

    private void PrintPolicy()
    {
        var gridPolicy = "Grid Policy:\n";
        for (var y = gridSize.y - 1; y >= 0; y--)
        {
            var line = "";
            for (var x = 0; x < gridSize.x; x++)
            {
                var state = new State(x, y);
                var value = "X";
                var stateAction = _policy.GetAction(state);
                value = stateAction switch
                {
                    Action.Up => "^",
                    Action.Right => ">",
                    Action.Down => "v",
                    Action.Left => "<",
                    _ => value
                };
                line += value + "\t";
            }
            gridPolicy += line + "\n";
        }
        Debug.Log(gridPolicy);
    }

    private void PrintStateValues()
    {
        var gridRepresentation = "Grid State Values:\n";
        for (var y = gridSize.y - 1; y >= 0; y--)
        {
            var line = "";
            for (var x = 0; x < gridSize.x; x++)
            {
                var state = new State(x, y);
                var value = _stateValues.ContainsKey(state) ? _stateValues[state] : 0f;
                line += value.ToString("F2") + "\t";
            }
            gridRepresentation += line + "\n";
        }
        Debug.Log(gridRepresentation);
    }

    private void PrintCoordinates()
    {
        var gridCoordinates = "Grid Coordinates:\n";
        for (var y = gridSize.y - 1; y >= 0; y--)
        {
            var line = "";
            for (var x = 0; x < gridSize.x; x++)
            {
                var state = new State(x, y);
                line += state.X +","+ state.Y +"\t";
            }
            gridCoordinates += line + "\n";
        }
        Debug.Log(gridCoordinates);
    }

    private void PrintPossibleActions()
    {
        string grid = "Grid Possible Actions:\n";
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < gridSize.x; x++)
            {
                State state = new State(x, y);
                int count = GetValidActions(state).Count;
                if (_obstacles.Contains(state))
                {
                    count = 0;
                }
                line += count + "\t";
            }
            grid += line + "\n";
        }
        Debug.Log(grid);
    }

    public State GetEnd()
    {
        return _end;
    }
}
