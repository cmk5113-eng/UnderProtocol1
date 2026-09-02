using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public void ChangeCurrentCharacter(Tilemap currenttilemap)
    {
        PlacementManager.Instance.tilemap = null;
        PlacementManager.Instance.tilemap = currenttilemap;
        Debug.Log(
       $"[Tilemap 변경] " +
       $"{PlacementManager.Instance.tilemap} → {currenttilemap}\n" +
       $"호출 위치:\n{System.Environment.StackTrace}"
   );
    }

    
}
