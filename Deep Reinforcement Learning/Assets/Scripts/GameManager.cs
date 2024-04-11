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
    
    private State _start; // deprecated
    private State _end; // deprecated
    private List<State> _obstacles; // deprecated
    private Policy _policy;
    private Dictionary<State, float> _stateValues; // Used by PolicyEvaluation
    private List<State> _states;
    private Map currentMap;
    private State currentState;

    private void Start()
    {
        //InitializeObstacles();
        //_states = GenerateAllStates();

        //TilemapManager.Instance.StartTilemap(_states);
        
        _start = new State(1, 1);
        _end = new State(5, 5);

        //TilemapManager.Instance.SetStartingValues(_start, _end);
        
        //PolicyIteration(_states, 0.9f); // Policy iteration
        //ValueIteration(states, 0.9f); // Value iteration

        //Map currentMap = GenerateSokobanMap();
        //_states = GenerateAllStates(Game.Sokoban, currentMap);

        currentMap = GenerateGridWorldMap();
        _states = GenerateAllStates(Game.GridWorld, currentMap);
        currentState = currentMap.startState;

        Debug.Log("Total states count : " + _states.Count);

        camera.transform.position = new Vector3(currentMap.size.x / 2, currentMap.size.y / 2, -10);

        _policy = new Policy();
        _policy.InitializePolicy(_states, this);
        //UpdateTilemap(_policy.GetPolicy());

        InitializeStateValues();

        //PrintCoordinates();
        
        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentMap.startState, _policy); // Affiche la map et le state
    }

    public void playPolicy()
    {
        Debug.Log("Play current policy");
        currentState = GetNextState(currentState, _policy.GetAction(currentState));
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
    }

    /*public void UpdateTilemap(Dictionary<State, Action> policy)
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
    }*/

    private List<State> GenerateAllStates(Game game, Map map)
    {
        var states = new List<State>();

        // GridWorld
        if (game == Game.GridWorld)
        {
            for (int x = 0; x < map.size.x; x++)
            {
                for (int y = 0; y < map.size.y; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!map.obstacles.Contains(position))
                    {
                        states.Add(new State(x, y));
                    }
                }
            }
        }
        // Sokoban
        else if (game == Game.Sokoban)
        {
            var allCrateCombinations = GetAllCombinations(map);
            foreach (var crateCombination in allCrateCombinations)
            {
                for (var x = 0; x < map.size.x; x++)
                {
                    for (var y = 0; y < map.size.y; y++)
                    {
                        Vector2Int playerPosition = new Vector2Int(x, y);
                        if (!map.obstacles.Contains(playerPosition) && !crateCombination.Contains(playerPosition))
                        {
                            states.Add(new State(playerPosition, crateCombination.ToList()));
                        }
                    }
                }
            }
        }

        return states;
    }

    private HashSet<HashSet<Vector2Int>> GetAllCombinations(Map map)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        // Générer une liste de toutes les positions possibles pour les caisses,
        // en excluant les positions d'obstacles.
        for (int x = 0; x < map.size.x; x++)
        {
            for (int y = 0; y < map.size.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!map.obstacles.Contains(pos))
                {
                    possiblePositions.Add(pos);
                }
            }
        }

        var allCombinations = new HashSet<HashSet<Vector2Int>>();

        GetCombinationsRecursive(possiblePositions, new List<Vector2Int>(), map.startState.crates.Count, 0, allCombinations);

        return allCombinations;
    }

    // Méthode récursive pour générer toutes les combinaisons possibles de positions des caisses.
    private void GetCombinationsRecursive(List<Vector2Int> possiblePositions, List<Vector2Int> currentCombination, int cratesLeft, int startPosition, HashSet<HashSet<Vector2Int>> allCombinations)
    {
        if (cratesLeft == 0)
        {
            // Si aucune caisse n'est laissée, ajoutez la combinaison actuelle à l'ensemble de toutes les combinaisons.
            allCombinations.Add(new HashSet<Vector2Int>(currentCombination));
            return;
        }

        for (int i = startPosition; i <= possiblePositions.Count - cratesLeft; i++)
        {
            currentCombination.Add(possiblePositions[i]);
            GetCombinationsRecursive(possiblePositions, currentCombination, cratesLeft - 1, i + 1, allCombinations);
            currentCombination.RemoveAt(currentCombination.Count - 1); // Retirer le dernier élément pour essayer la prochaine combinaison
        }
    }

    private void InitializeStateValues()
    {
        _stateValues = new Dictionary<State, float>();
        foreach (var state in _states)
        {
            // Default 0, 
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
        
        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
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
        

        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
    }

    public float GetImmediateReward(State currentState, Action action)
    {
        var nextState = GetNextState(currentState, action);
        
        if(currentState.game == Game.GridWorld)
        {
            if (nextState.Equals(_end))
            {
                return 1.0f;
            }

            Vector2Int playerPosition = new Vector2Int(nextState.X, nextState.Y);
            if (currentMap.obstacles.Contains(playerPosition))
            {
                return -1.0f;
            }
            return 0.0f;
        }
        else if (currentState.game == Game.Sokoban)
        {
            // todo
        }
        return 0.0f;
    }

    private State GetNextState(State state, Action action)
    {
        if(state.game == Game.GridWorld)
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
                default:
                    break;
            }

            Vector2Int playerPosition = new Vector2Int(nextState.X, nextState.Y);
            return currentMap.obstacles.Contains(playerPosition) ? state : nextState;
        }
       else if(state.game == Game.Sokoban)
       {
            // todo
       }
        return state;
    }

    public List<Action> GetValidActions(State state)
    {
        var validActions = new List<Action>();

        if(state.game == Game.GridWorld)
        {
            if (state.Y < gridSize.y - 1) validActions.Add(Action.Up); // Peut aller vers le haut
            if (state.X < gridSize.x - 1) validActions.Add(Action.Right); // Peut aller vers la droite
            if (state.Y > 0) validActions.Add(Action.Down); // Peut aller vers le bas
            if (state.X > 0) validActions.Add(Action.Left); // Peut aller vers la gauche
        }
        else if(state.game == Game.Sokoban)
        {
            // todo
        }

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
        for (var y = currentMap.size.y - 1; y >= 0; y--)
        {
            var line = "";
            for (var x = 0; x < currentMap.size.x; x++)
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

    public Map GenerateGridWorldMap()
    {
        Vector2Int dimensions = new Vector2Int(7, 7); // Taille de la grille 7x7

        List<Vector2Int> walls = new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(5,0),
            new Vector2Int(6,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(0,5),
            new Vector2Int(0,6), new Vector2Int(1,6), new Vector2Int(2,6),
            new Vector2Int(3,6), new Vector2Int(4,6), new Vector2Int(5,6),
            new Vector2Int(6,6), new Vector2Int(6,1), new Vector2Int(6,2),
            new Vector2Int(6,3), new Vector2Int(6,4), new Vector2Int(6,5)
        };

        List<Vector2Int> crates = new List<Vector2Int> {};

        List<Vector2Int> targets = new List<Vector2Int>
        {
            new Vector2Int(5, 5)
        };

        Vector2Int spawnPosition = new Vector2Int(1, 1);

        // Création de l'état initial et final
        State startState = new State(spawnPosition.x, spawnPosition.y);
        State endState = new State(5, 5);

        // Initialisation de la map
        return new Map(dimensions, walls, targets, startState, endState);
    }

    public Map GenerateSokobanMap()
    {
        Vector2Int dimensions = new Vector2Int(7, 7); // Taille de la grille 7x7

        List<Vector2Int> walls = new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(5,0),
            new Vector2Int(6,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(0,5),
            new Vector2Int(0,6), new Vector2Int(1,6), new Vector2Int(2,6),
            new Vector2Int(3,6), new Vector2Int(4,6), new Vector2Int(5,6),
            new Vector2Int(6,6), new Vector2Int(6,1), new Vector2Int(6,2),
            new Vector2Int(6,3), new Vector2Int(6,4), new Vector2Int(6,5)
        };

        List<Vector2Int> crates = new List<Vector2Int>
        {
            new Vector2Int(4, 2), new Vector2Int(4, 4)
        };

        List<Vector2Int> targets = new List<Vector2Int>
        {
            new Vector2Int(2, 5), new Vector2Int(5, 1)
        };

        Vector2Int spawnPosition = new Vector2Int(3, 3);
        List<Vector2Int> spawnList = new List<Vector2Int> { spawnPosition }; // Converti en liste pour uniformiser avec crates et targets

        // Création de l'état initial et final (pour Sokoban, l'état final pourrait ne pas être directement défini)
        State startState = new State(spawnPosition, crates);
        State endState = null; // Dans Sokoban, l'état de fin est généralement implicite basé sur les objectifs

        // Initialisation de la map
        return new Map(dimensions, walls, targets, startState, endState);
    }
}
