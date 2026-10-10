using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MonsterHealthBar : MonoBehaviour
{
    private MonsterBase owner;
    private SpriteRenderer body;
    private Canvas canvas;
    private RectTransform barRect;
    private RectTransform fillRect;

    public void Bind(MonsterBase monster)
    {
        owner = monster;
        body = owner != null ? owner.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (canvas == null) CreateBar();
        Refresh();
    }

    private void CreateBar()
    {
        GameObject root = new GameObject("HPBar", typeof(RectTransform), typeof(Canvas), typeof(Image));
        root.transform.SetParent(transform, false);
        barRect = root.GetComponent<RectTransform>();
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        Image background = root.GetComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.09f, 0.95f);
        background.raycastTarget = false;

        GameObject fill = new GameObject("HP", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(root.transform, false);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        Image image = fill.GetComponent<Image>();
        image.color = new Color(0.9f, 0.18f, 0.22f);
        image.raycastTarget = false;
    }

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    private void OnDisable()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (canvas == null) return;
        bool visible = owner != null && owner.gameObject.activeInHierarchy && !owner.IsDead && owner.MaxHP > 0;
        if (canvas.gameObject.activeSelf != visible) canvas.gameObject.SetActive(visible);
        if (!visible) return;

        float ratio = Mathf.Clamp01((float)owner.currentHP / owner.MaxHP);
        fillRect.anchorMax = new Vector2(ratio, 1f);
        // Insets remain proportional at low HP instead of giving a negative fill width.
        fillRect.offsetMax = new Vector2(-2f * ratio, -2f);
        fillRect.offsetMin = new Vector2(2f * ratio, 2f);

        float width = 0.75f;
        Vector3 position = owner.transform.position + new Vector3(0f, 0.6f, 0f);
        if (body != null && body.sprite != null)
        {
            Bounds bounds = body.bounds;
            width = Mathf.Clamp(bounds.size.x * 0.85f, 0.55f, 2f);
            position = new Vector3(bounds.center.x, bounds.max.y + 0.16f, owner.transform.position.z);
            canvas.sortingLayerID = body.sortingLayerID;
            canvas.sortingOrder = body.sortingOrder + 1;
        }
        else if (owner.OccupiedMap != null)
        {
            var map = owner.OccupiedMap;
            Vector3Int top = owner.GetAnchorCell(map) + new Vector3Int(0, owner.FootprintSize.y - 1, 0);
            Vector3 center = map.GetCellCenterWorld(top);
            float cellHeight = (map.GetCellCenterWorld(top + Vector3Int.up) - center).magnitude;
            position.y = center.y + cellHeight * 0.5f + 0.16f;
            canvas.sortingOrder = 100;
        }
        else canvas.sortingOrder = 100;

        barRect.sizeDelta = new Vector2(width * 100f, 10f);
        Vector3 scale = transform.lossyScale;
        barRect.localScale = new Vector3(0.01f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            0.01f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        barRect.rotation = Quaternion.identity;
        barRect.position = position;
    }
}
