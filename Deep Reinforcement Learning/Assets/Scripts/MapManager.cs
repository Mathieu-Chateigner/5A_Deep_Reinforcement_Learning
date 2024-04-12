using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private List<Map> gridWorldMaps;
    private List<Map> sokobanMaps;

    public MapManager()
    {
        gridWorldMaps = new List<Map>();
        sokobanMaps = new List<Map>();
        gridWorldMaps.Add(GenerateGridWorldMap1());
        gridWorldMaps.Add(GenerateGridWorldMap2());
        sokobanMaps.Add(GenerateSokobanMap1());
    }

    public Map GetMap(Game game, int index)
    {
        if(game == Game.GridWorld)
        {
            return gridWorldMaps[index % gridWorldMaps.Count];
        }
        else
        {
            return sokobanMaps[index % sokobanMaps.Count];
        }
    }

    public Map GenerateGridWorldMap1()
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

        List<Vector2Int> crates = new List<Vector2Int> { };

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

    public Map GenerateGridWorldMap2()
    {
        Vector2Int dimensions = new Vector2Int(10, 10);

        List<Vector2Int> walls = new List<Vector2Int>
        {
            new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(2,1),
            new Vector2Int(4,4), new Vector2Int(5,4), new Vector2Int(4,3)
        };

        List<Vector2Int> crates = new List<Vector2Int> { };

        List<Vector2Int> targets = new List<Vector2Int>
        {
            new Vector2Int(1, 9)
        };

        Vector2Int spawnPosition = new Vector2Int(6, 0);

        // Création de l'état initial et final
        State startState = new State(spawnPosition.x, spawnPosition.y);
        State endState = new State(1, 9);

        return new Map(dimensions, walls, targets, startState, endState);
    }

    public Map GenerateSokobanMap1()
    {
        Vector2Int dimensions = new Vector2Int(7, 7); // Taille de la grille 7x7

        List<Vector2Int> walls = new List<Vector2Int>
        {
            new (2, 3), new (3, 4), new (2, 4)
        };

        for (var i = 0; i < dimensions.x; i++)
        {
            walls.Add(new (i, 0));
            walls.Add(new (i, dimensions.y-1));
        }
        
        for (var j = 0; j < dimensions.y; j++)
        {
            walls.Add(new (0, j));
            walls.Add(new (dimensions.x-1, j));
        }

        List<Vector2Int> crates = new List<Vector2Int>
        {
            new Vector2Int(4, 2)
        };

        List<Vector2Int> targets = new List<Vector2Int>
        {
            new Vector2Int(2, 5)
        };

        Vector2Int spawnPosition = new Vector2Int(3, 3);
        List<Vector2Int> spawnList = new List<Vector2Int> { spawnPosition }; // Converti en liste pour uniformiser avec crates et targets

        // Création de l'état initial et final (pour Sokoban, l'état final pourrait ne pas être directement défini)
        State startState = new State(spawnPosition, crates);
        State endState = null; // Dans Sokoban, l'état de fin est généralement implicite basé sur les objectifs

        return new Map(dimensions, walls, targets, startState, endState);
    }
}
