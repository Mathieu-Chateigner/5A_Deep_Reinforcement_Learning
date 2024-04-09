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
    
    private State _start;
    private State _end;
    private List<State> _obstacles;
    private Policy _policy;
    private Dictionary<State, float> _stateValues; // Used by PolicyEvaluation
    private List<State> _states;

    private void Start()
    {
        camera.transform.position = new Vector3(gridSize.x/2, gridSize.y/2, -10);
        InitializeObstacles();
        _states = GenerateAllStates();
        
        _start = new State(0, 0);
        _end = new State(8, 8);
        
        TilemapManager.Instance.SetStartingValues(_start, _end);
        
        _policy = new Policy();
        _policy.InitializePolicy(_states, this);

        InitializeStateValues();

        PrintCoordinates();

        //PolicyIteration(_states, 0.9f); // Policy iteration
        //ValueIteration(states, 0.9f); // Value iteration
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

                    var reward = IsEnd(nextState) ? 0f : GetImmediateReward(state, action); // To fix values between 0 and 1
                    var value = reward + (discountFactor * _stateValues[nextState]);
                    if (!(value > maxValue)) continue;
                    maxValue = value;
                    bestAction = action;
                }
                
                _stateValues[state] = maxValue;

                // Update policy
                _policy.UpdatePolicy(state, bestAction);
                
                delta = Mathf.Max(delta, Mathf.Abs(oldValue - maxValue));
            }
        } while (delta > theta);
        UpdateTilemap(_policy.GetPolicy());
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
            return -1.0f;
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

        return _obstacles.Contains(nextState) ? state : nextState;
    }

    public List<Action> GetValidActions(State state)
    {
        var validActions = new List<Action>();

        if (state.Y < gridSize.y - 1) validActions.Add(Action.Up); // Peut aller vers le haut
        if (state.X < gridSize.x - 1) validActions.Add(Action.Right); // Peut aller vers la droite
        if (state.Y > 0) validActions.Add(Action.Down); // Peut aller vers le bas
        if (state.X > 0) validActions.Add(Action.Left); // Peut aller vers la gauche

        // Check aussi les obstacles

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

    public State GetEnd()
    {
        return _end;
    }
}
