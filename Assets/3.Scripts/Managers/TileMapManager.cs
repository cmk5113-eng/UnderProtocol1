using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public void ChangeCurrentCharacter(Tilemap currenttilemap)
    {
        if (PlacementManager.Instance == null || currenttilemap == null) return;
        PlacementManager.Instance.tileDatas.Clear();
        PlacementManager.Instance.tilemap = currenttilemap;
        PlacementManager.Instance.InitializeMapOrigin();

    }

    
}
