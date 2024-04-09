using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public Camera camera;
    public Tilemap tilemapGridWorld;
    public List<Tile> tileList;
    
    private State _start;
    private State _end;
    private List<State> _obstacles;
    private Policy _policy;
    private Dictionary<State, float> _stateValues; // Used by PolicyEvaluation
    private Dictionary<State, Tile> _dictTileForState = new();
    private List<State> _states;

    private void Start()
    {
        camera.transform.position = new Vector3(gridSize.x/2, gridSize.y/2, -10);
        InitializeObstacles();
        _states = GenerateAllStates();
        
        _start = new State(0, 0);
        _end = new State(3, 3);
        _policy = new Policy();
        StartCoroutine(_policy.InitializePolicy(_states, this, tilemapGridWorld, tileList));

        InitializeStateValues();

        PrintCoordinates();

        // Policy iteration pipeline
        /*PolicyEvaluation(states, 0.9f);
        PolicyImprovement(states);*/

        // Value iteration pipeline
        PrintPolicy();
        //ValueIteration(0.9f);
        PrintStateValues();
        PrintPolicy();
    }

    private void InitializeObstacles()
    {
        _obstacles = new List<State>
        {
            // On ajoute nos obstacles içi
            //new State(1, 1),
            //new State(2, 2),
            //new State(4, 0)
        };
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
                tilemapGridWorld.SetTile(new Vector3Int(x,y,0), tileList.First(tile => tile.name.Equals("question_mark")));
            }
        }
        return states;
    }

    private void InitializeStateValues()
    {
        _stateValues = new Dictionary<State, float>();
        foreach (var state in _states)
        {
            // Tous les états ont une valeur par défaut de 0, sauf l'état final qui a une valeur de 1
            _stateValues[state] = state.Equals(_end) ? 1f : 0f;
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
                float reward = GetImmediateReward(state, action);
                float newValue = reward + (discountFactor * stateValues[nextState]);
                

                stateValues[state] = newValue;

                delta = Mathf.Max(delta, Mathf.Abs(oldValue - newValue));
            }
        } while (delta > theta);
    }*/

    // Should be ok
    public void ValueIteration(float discountFactor)
    {
        const float theta = 0.001f; // Seuil pour déterminer quand arrêter l'itération
        float delta;
        do
        {
            delta = 0f;
            foreach (var state in _states)
            {
                if (IsEnd(state)) continue;

                var oldValue = _stateValues[state];

                // On prend seulement le max des actions possibles
                var maxValue = Mathf.NegativeInfinity;
                var bestAction = Action.Up; // Default
                foreach (var action in GetValidActions(state))
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
    }

    public void PolicyImprovement(List<State> states)
    {
        foreach (var state in states)
        {
            if (IsEnd(state)) continue; // Aucune action requise pour les états terminaux

            var bestAction = Action.Up; // Valeur par défaut, sera remplacée
            var bestValue = float.NegativeInfinity;

            foreach (var action in GetValidActions(state))
            {
                var nextState = GetNextState(state, action);
                var value = _stateValues[nextState];

                if (!(value > bestValue)) continue;
                bestValue = value;
                bestAction = action;
            }
            //Debug.Log("["+ state.X +","+ state.Y +"] =>"+ bestValue);
            //Debug.Log("[" + state.X + "," + state.Y + "] =>" + GetValidActions(state).Count) ;
            // Mettre à jour la politique pour cet état avec la meilleure action trouvée
            _policy.UpdatePolicy(state, bestAction);
        }
    }

    private float GetImmediateReward(State currentState, Action action)
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
                nextState.X = Mathf.Min(nextState.X + 1, gridSize.x - 1);
                break;
            case Action.Down:
                nextState.Y = nextState.Y - 1;
                break;
            case Action.Left:
                nextState.X = Mathf.Max(nextState.X - 1, 0);
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
}
