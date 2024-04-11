using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Rigidbody2D player;
    public LayerMask wallsLayer;
    public LayerMask boxesLayer;
    public LayerMask rewardLayer;

    private bool _canMove;
    private int _reward;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(KeyCode.RightArrow);
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(KeyCode.LeftArrow);
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Move(KeyCode.UpArrow);
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(KeyCode.DownArrow);
        }
    }

    private void Move(KeyCode key)
    {
        int x;
        int y;
        
        switch (key)
        {
            case KeyCode.RightArrow:
                x = 1;
                y = 0;
                break;
            case KeyCode.LeftArrow:
                x = -1;
                y = 0;
                break;
            case KeyCode.UpArrow:
                x = 0;
                y = 1;
                break;
            case KeyCode.DownArrow:
                x = 0;
                y = -1;
                break;
            default:
                return;
        }
        
        var playerPos = player.position;
        var playerNextPos = playerPos + new Vector2(x,y);
            
        if (!Physics2D.OverlapCircle(playerNextPos, .2f, wallsLayer))
        {
            _canMove = true;
            if (Physics2D.OverlapCircle(playerNextPos, .2f, boxesLayer))
            {
                var boxNextPos = playerNextPos + new Vector2(x,y);
                var tile = SokobanManager.Instance.tilemapBoxes.GetTile(
                    new Vector3Int(Mathf.RoundToInt(playerNextPos.x-0.5f), Mathf.RoundToInt(playerNextPos.y-0.5f), 0));
                if (Physics2D.OverlapCircle(boxNextPos, .2f, wallsLayer))
                {
                    _canMove = false;
                }
                else
                {
                    _canMove = true;
                    SokobanManager.Instance.tilemapBoxes.SetTile(new Vector3Int(Mathf.RoundToInt(playerNextPos.x-0.5f), Mathf.RoundToInt(playerNextPos.y-0.5f), 0), null);
                    SokobanManager.Instance.tilemapBoxes.SetTile(new Vector3Int(Mathf.RoundToInt(boxNextPos.x-0.5f), Mathf.RoundToInt(boxNextPos.y-0.5f),0), tile);

                    if (SokobanManager.Instance.tilemapRewards.GetTile(new Vector3Int(Mathf.RoundToInt(boxNextPos.x-0.5f), Mathf.RoundToInt(boxNextPos.y-0.5f),0)) != null)
                    {
                        _reward++;
                        Debug.Log(_reward);
                    }
                }
            }
            else
            {
                _canMove = true;
            }
        }
        else
        {
            _canMove = false;
        }

        if (!_canMove) return;
        
        player.transform.position = playerNextPos;
        _canMove = false;
    }
}
