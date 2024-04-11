using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public Camera camera;
    public List<Button> listButtons = new ();

    private State _start; // deprecated
    private State _end; // deprecated
    private List<State> _obstacles; // deprecated
    private Policy _policy;
    private Dictionary<State, float> _stateValues; // Used by PolicyEvaluation and ValueIteration
    private List<State> _states;
    private Map currentMap;
    private State currentState;

    private Dictionary<State, float> returnsSum;
    private new Dictionary<State, int> returnsCount;
    private MapManager mapManager;

    private void Start()
    {
        mapManager = new MapManager();
        //InitializeObstacles();
        //_states = GenerateAllStates();

        //_start = new State(1, 1);
       

        //PolicyIteration(_states, 0.9f); // Policy iteration
        //ValueIteration(states, 0.9f); // Value iteration

        //currentMap = GenerateSokobanMap();
        //_states = GenerateAllStates(Game.Sokoban, currentMap);

        Game game = Game.GridWorld;
        currentMap = mapManager.GetMap(game, 0);
        _end = currentMap.endState; // ONLY FOR GRID WORLD (deprecated)
        _states = GenerateAllStates(game, currentMap);
        currentState = currentMap.startState;

        Debug.Log("Total states count : " + _states.Count);

        camera.transform.position = new Vector3(currentMap.size.x / 2, currentMap.size.y / 2, -10);

        _policy = new Policy();
        _policy.InitializePolicy(_states, this);

        InitializeStateValues();

        //PrintCoordinates();

        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentMap.startState, _policy); // Affiche la map et le state
        SetInteractivnessButtons(true);
    }

    public void playPolicy()
    {
        Debug.Log("Play current policy");
        currentState = GetNextState(currentState, _policy.GetAction(currentState));
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
    }

    public void MonteCarlo(int numEpisode)
    {
        SetInteractivnessButtons(false);
        MonteCarloFirstVisitOnPolicy(numEpisode);
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
        SetInteractivnessButtons(true);
    }

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
        // Positions possibles pour les caisses sans les positions d'obstacles
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
            // Si aucune caisse n'est laissée, ajoutez la combinaison actuelle aux combinaisons
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
            if(state.game == Game.GridWorld)
            {
                _stateValues[state] = state.Equals(currentMap.endState) ? 1f : 0f;
            }
            else if(state.game == Game.Sokoban)
            {
                // Ratio de caisses sur les cibles
                float score = CalculateScore(state, currentMap.targets);
                _stateValues[state] = score;
            }
        }
    }

    // Ratio of crates on targets
    private float CalculateScore(State state, List<Vector2Int> targets)
    {
        if (state.crates == null || state.crates.Count == 0)
        {
            return 0f; // Pas de caisse
        }

        // Compte le nombre de caisse sur une cible
        int cratesOnTarget = 0;
        foreach (var crate in state.crates)
        {
            if (targets.Contains(crate))
            {
                cratesOnTarget++;
            }
        }

        // Ratio nombre de caisses
        return (float)cratesOnTarget / state.crates.Count;
    }

    public void PolicyIteration(float discountFactor)
    {
        SetInteractivnessButtons(false);
        bool policyStable = false;
        do
        {
            PolicyEvaluation(discountFactor);

            policyStable = PolicyImprovement();
        } while (!policyStable);

        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
        SetInteractivnessButtons(true);
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
        SetInteractivnessButtons(false);
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


        PrintPolicy();
        TilemapManager.Instance.Display(currentMap, currentState, _policy); // Affiche la map et le state
        SetInteractivnessButtons(true);
    }

    public Dictionary<State, float> MonteCarloFirstVisitOnPolicy(int numEpisode)
    {
        returnsSum = new Dictionary<State, float>();
        returnsCount = new Dictionary<State, int>();

        foreach (var state in _states)
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

            // Backpropagation
            for (int t = episode.Count - 1; t >= 0; t--)
            {
                G = 0.9f * G + episode[t].Item3; // Reward t

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
            UpdatePolicyBasedOnStateValues(_states, values); // On policy
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
        State currentState = currentMap.startState; // Commencez � l'�tat de d�part
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

        if(currentState.game == Game.GridWorld)
        {
            if (nextState.Equals(_end))
            {
                return 1.0f;
            }

            Vector2Int playerPosition = new Vector2Int(nextState.X, nextState.Y);
            if (currentMap.obstacles.Contains(playerPosition))
            {
                return 0.0f;
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

        if (state.game == Game.GridWorld)
        {
            if (state.Y < gridSize.y - 1) validActions.Add(Action.Up); // Peut aller vers le haut
            if (state.X < gridSize.x - 1) validActions.Add(Action.Right); // Peut aller vers la droite
            if (state.Y > 0) validActions.Add(Action.Down); // Peut aller vers le bas
            if (state.X > 0) validActions.Add(Action.Left); // Peut aller vers la gauche

            // Check aussi les obstacles
            Vector2Int playerPosition = new Vector2Int(state.X, state.Y);
            State upNextState = GetNextState(state, Action.Up);
            State rightNextState = GetNextState(state, Action.Right);
            State downNextState = GetNextState(state, Action.Down);
            State leftNextState = GetNextState(state, Action.Left);
            if (currentMap.obstacles.Contains(new Vector2Int(upNextState.X, upNextState.Y)))
            {
                validActions.Remove(Action.Up);
            }
            if (currentMap.obstacles.Contains(new Vector2Int(rightNextState.X, rightNextState.Y)))
            {
                validActions.Remove(Action.Right);
            }
            if (currentMap.obstacles.Contains(new Vector2Int(downNextState.X, downNextState.Y)))
            {
                validActions.Remove(Action.Down);
            }
            if (currentMap.obstacles.Contains(new Vector2Int(leftNextState.X, leftNextState.Y)))
            {
                validActions.Remove(Action.Left);
            }
        }
        else if (state.game == Game.Sokoban)
        {
            // todo
        }
        return validActions;
    }

    private bool IsEnd(State state)
    {
        return state.Equals(_end); // to review (sokoban map has no defined endstate)
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
            new Vector2Int(6,3), new Vector2Int(6,4), new Vector2Int(6,5),
            new Vector2Int(4,4), new Vector2Int(5,4), new Vector2Int(4,3)
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

        return new Map(dimensions, walls, targets, startState, endState);
    }

    private void SetInteractivnessButtons(bool interactable)
    {
        foreach (var button in listButtons)
        {
            button.interactable = interactable;
        }
    }
}
