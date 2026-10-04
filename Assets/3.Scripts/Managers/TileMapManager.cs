using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public void ChangeCurrentCharacter(Tilemap currenttilemap)
    {
        if (PlacementManager.Instance == null || currenttilemap == null) return;

        Tilemap previous = PlacementManager.Instance.tilemap;
        if (previous != null && previous != currenttilemap)
            previous.gameObject.SetActive(false);

        // Map 부모를 다시 켜도 이전 스테이지의 Tilemap이 함께 나타나지 않게 한다.
        Transform parent = currenttilemap.transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Tilemap sibling = parent.GetChild(i).GetComponent<Tilemap>();
                if (sibling != null && sibling != currenttilemap)
                    sibling.gameObject.SetActive(false);
            }
        }
        currenttilemap.gameObject.SetActive(true);
        PlacementManager.Instance.tileDatas.Clear();
        PlacementManager.Instance.tilemap = currenttilemap;
        PlacementManager.Instance.InitializeMapOrigin();

    }

    
}
