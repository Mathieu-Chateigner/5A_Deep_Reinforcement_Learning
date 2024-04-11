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

    private void Awake()
    {
        Instance = this;
    }

    private Tile GetPlayerTile()
    {
        return tileList.First(tile => tile.name.Equals("player"));
    }

    private Tile GetRewardTile()
    {
        return tileList.First(tile => tile.name.Equals("reward"));
    }
    
    private Tile GetQuestionMarkTile()
    {
        return tileList.First(tile => tile.name.Equals("question_mark"));
    }

    public void Display(Map map, State currentState, Policy policy)
    {
        tilemap.ClearAllTiles(); // Efface la tilemap actuelle pour le nouvel affichage

        // Afficher les murs/obstacles
        foreach (Vector2Int obstacle in map.obstacles)
        {
            tilemap.SetTile(new Vector3Int(obstacle.x, obstacle.y, 0), tileList.First(tile => tile.name.Equals("wall_128")));
        }

        // Afficher les cibles
        foreach (Vector2Int target in map.targets)
        {
            tilemap.SetTile(new Vector3Int(target.x, target.y, 0), tileList.First(tile => tile.name.Equals("x_blue_128")));
        }

       
        if (currentState.game == Game.Sokoban)
        {
            // Afficher le joueur
            tilemap.SetTile(new Vector3Int(currentState.player.x, currentState.player.y, 0), GetPlayerTile());

            // Afficher les caisses
            foreach (Vector2Int crate in currentState.crates)
            {
                tilemap.SetTile(new Vector3Int(crate.x, crate.y, 0), tileList.First(tile => tile.name.Equals("crate")));
            }
        }
        else if (currentState.game == Game.GridWorld)
        {
            // Afficher le joueur
            tilemap.SetTile(new Vector3Int(currentState.X, currentState.Y, 0), GetPlayerTile());

            // Afficher la policy (arrows)
            foreach (var (state, action) in policy.GetPolicy())
            {
                Vector3Int tilePosition = new Vector3Int(state.X, state.Y, 0);
                if (tilePosition == new Vector3Int(currentState.X, currentState.Y, 0)) continue; // Player tile

                /*bool onCrate = false;
                foreach (Vector2Int crate in currentState.crates)
                {
                    if (tilePosition == new Vector3Int(crate.x, crate.y, 0)) onCrate = true;
                }
                if (onCrate) continue; // Crate title*/

                bool onWall = false;
                foreach (Vector2Int obstacle in map.obstacles)
                {
                    if (tilePosition == new Vector3Int(obstacle.x, obstacle.y, 0)) onWall = true;
                }
                if (onWall) continue; // Wall tile

                bool onTarget = false;
                foreach (Vector2Int target in map.targets)
                {
                    if (tilePosition == new Vector3Int(target.x, target.y, 0)) onTarget = true;
                }
                if (onTarget) continue; // Target tile

                tilemap.SetTile(tilePosition, GetTileFromAction(action));
            }
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
}