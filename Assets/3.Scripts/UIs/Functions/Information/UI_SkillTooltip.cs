using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_SkillTooltip : MonoBehaviour
{
    private RectTransform panel;
    private RectTransform viewport;
    private RectTransform anchor;
    private TextMeshProUGUI text;
    private readonly Vector3[] corners = new Vector3[4];
    private float preferredHeight;

    public static UI_SkillTooltip Create(RectTransform parent, TMP_FontAsset font)
    {
        GameObject root = new GameObject("SkillTooltip", typeof(RectTransform), typeof(Image), typeof(UI_SkillTooltip));
        root.transform.SetParent(parent, false);
        UI_SkillTooltip tooltip = root.GetComponent<UI_SkillTooltip>();
        tooltip.viewport = parent;
        tooltip.panel = root.GetComponent<RectTransform>();
        tooltip.panel.anchorMin = tooltip.panel.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.panel.pivot = new Vector2(0.5f, 0f);
        Image background = root.GetComponent<Image>();
        background.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 12f);
        labelRect.offsetMax = new Vector2(-14f, -12f);
        tooltip.text = label.GetComponent<TextMeshProUGUI>();
        if (font != null) tooltip.text.font = font;
        tooltip.text.fontSize = 22f;
        tooltip.text.enableAutoSizing = true;
        tooltip.text.fontSizeMin = 14f;
        tooltip.text.fontSizeMax = 22f;
        tooltip.text.alignment = TextAlignmentOptions.TopLeft;
        tooltip.text.color = Color.white;
        tooltip.text.raycastTarget = false;
        root.SetActive(false);
        return tooltip;
    }

    public void Show(SkillList skill, RectTransform source)
    {
        if (skill == null || source == null) { Hide(); return; }
        anchor = source;
        string title = string.IsNullOrWhiteSpace(skill.skillName) ? skill.name : skill.skillName;
        string content = $"<b>{title}</b>";
        if (!string.IsNullOrWhiteSpace(skill.description)) content += "\n" + skill.description;
        if (!string.IsNullOrWhiteSpace(skill.condition)) content += "\n조건: " + skill.condition;
        text.SetText(content);
        float width = Mathf.Min(360f, Mathf.Max(80f, viewport.rect.width - 16f));
        preferredHeight = Mathf.Clamp(text.GetPreferredValues(content, width - 28f, Mathf.Infinity).y + 24f, 72f, 420f);
        panel.sizeDelta = new Vector2(width, preferredHeight);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        PositionAbove();
    }

    private void LateUpdate()
    {
        if (anchor == null || !anchor.gameObject.activeInHierarchy || Input.GetMouseButtonDown(1)) { Hide(); return; }
        PositionAbove();
    }

    private void PositionAbove()
    {
        anchor.GetWorldCorners(corners);
        float left = float.PositiveInfinity, right = float.NegativeInfinity, top = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector3 local = viewport.InverseTransformPoint(corner);
            left = Mathf.Min(left, local.x);
            right = Mathf.Max(right, local.x);
            top = Mathf.Max(top, local.y);
        }
        float y = top + 12f;
        float height = Mathf.Min(preferredHeight, Mathf.Max(48f, viewport.rect.yMax - y - 8f));
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
        float halfWidth = panel.sizeDelta.x * 0.5f;
        float x = Mathf.Clamp((left + right) * 0.5f, viewport.rect.xMin + halfWidth + 8f,
            viewport.rect.xMax - halfWidth - 8f);
        panel.localPosition = new Vector3(x, y, 0f);
    }

    public void Hide()
    {
        anchor = null;
        gameObject.SetActive(false);
    }
}
