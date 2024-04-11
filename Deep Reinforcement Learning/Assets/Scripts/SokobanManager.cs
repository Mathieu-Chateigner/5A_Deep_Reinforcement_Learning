using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SokobanManager : MonoBehaviour
{
    public Tilemap tilemapWalls;
    public Tilemap tilemapBoxes;
    public Tilemap tilemapRewards;

    public static SokobanManager Instance;

    private void Awake()
    {
        Instance = this;
    }
}
