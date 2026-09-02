using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public void ChangeCurrentCharacter(Tilemap currenttilemap)
    {
        PlacementManager.Instance.tilemap = null;
        PlacementManager.Instance.tilemap = currenttilemap;

    }

    
}
