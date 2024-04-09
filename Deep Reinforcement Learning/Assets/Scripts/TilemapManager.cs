using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    public Tilemap tilemap;
    public List<Tile> tileList;

    public static TilemapManager Instance;

    private State _start;
    private State _end;

    private void Start()
    {
        Instance = this;
    }

    public void SetStartingValues(State start, State end)
    {
        _start = start;
        _end = end;
    }

    public IEnumerator UpdateTilemap(Dictionary<State, Action> policy, System.Action onFinish)
    {
        foreach (var (state, action) in policy)
        {
            tilemap.SetTile(new Vector3Int(state.X, state.Y, 0), GetTileFromAction(action));
            
            if (state.Equals(_start))
                tilemap.SetTile(new Vector3Int(state.X, state.Y, 0), GetPlayerTile());
            
            if (state.Equals(_end))
                tilemap.SetTile(new Vector3Int(state.X, state.Y, 0), GetRewardTile());
            
            yield return new WaitForSeconds(0.1f);
        }
        
        onFinish?.Invoke();
    }

    public IEnumerator UpdateTilemapObstacles(List<State> listObstacles)
    {
        foreach (var obstacle in listObstacles)
        {
            tilemap.SetTile(new Vector3Int(obstacle.X, obstacle.Y, 0), tileList.First(tile => tile.name.Equals("obstacle")));
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private Tile GetTileFromAction(Action action)
    {
        return action switch
        {
            Action.Down => tileList.First(tile => tile.name.Equals("arrow_down")),
            Action.Up => tileList.First(tile => tile.name.Equals("arrow_up")),
            Action.Left => tileList.First(tile => tile.name.Equals("arrow_left")),
            Action.Right => tileList.First(tile => tile.name.Equals("arrow_right")),
            _ => tileList.First(tile => tile.name.Equals("question_mark"))
        };
    }

    private Tile GetPlayerTile()
    {
        return tileList.First(tile => tile.name.Equals("player"));
    }

    private Tile GetRewardTile()
    {
        return tileList.First(tile => tile.name.Equals("reward"));
    }
}