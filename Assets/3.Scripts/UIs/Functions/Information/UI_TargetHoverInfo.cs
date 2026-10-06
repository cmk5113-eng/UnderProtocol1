using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class UI_TargetHoverInfo : OpenableUIBase
{
    [SerializeField] Vector2 shiftedPosition;
    [SerializeField] TMPro.TextMeshProUGUI nameText;
    [SerializeField] TMPro.TextMeshProUGUI infoText;
    [SerializeField] Image portrait;
    [SerializeField] TMPro.TextMeshProUGUI skillText;
    [SerializeField] TMPro.TextMeshProUGUI positionText;
    [Tooltip("기존 팝업은 켜고, 스테이지의 고정 정보 패널은 끕니다.")]
    [SerializeField] bool followCursor = true;
    [Tooltip("이미지 옆 텍스트 하나에 이름, 상태, 좌표를 함께 표시할 때 연결합니다.")]
    [SerializeField] TMPro.TextMeshProUGUI summaryText;
    CharacterBase target;

    void Awake()
    {
        // 정보 표시용 UI가 유닛과 타일의 마우스 판정을 가리지 않도록 한다.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    void OnEnable()
    {
        if (!followCursor) ClearDisplay();
    }

    void OnDisable() => target = null;

    void OnDestroy() => UnsubscribeInput();

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        UnsubscribeInput();
        InputManager.OnMouseHover += HoverInfoChange;
        InputManager.OnMouseMove += MoveToMouse;
    }

    public override void Unregistration(UIManager manager)
    {
        UnsubscribeInput();
        base.Unregistration(manager);
    }

    void UnsubscribeInput()
    {
        InputManager.OnMouseHover -= HoverInfoChange;
        InputManager.OnMouseMove -= MoveToMouse;
    }

    void LateUpdate()
    {
        if (!followCursor)
        {
            RefreshAtCursor(InputManager.CursorWorldPosition, InputManager.CursorHoverObject);
            return;
        }

        // 같은 대상을 계속 보고 있어도 피해, 회복, 턴 갱신을 즉시 표시한다.
        if (IsLiveTarget(target)) ShowCharacter(target, CurrentMap());
        else Close();
    }

    void HoverInfoChange(GameObject newTarget, GameObject oldTarget)
    {
        if (!followCursor) return;
        CharacterBase character = newTarget != null
            ? newTarget.GetComponentInParent<CharacterBase>() : null;
        if (IsLiveTarget(character))
        {
            target = character;
            ShowCharacter(character, CurrentMap());
            Open();
        }
        else
        {
            target = null;
            Close();
        }
    }

    void MoveToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {
        if (followCursor) transform.position = screenPosition + shiftedPosition;
    }

    static Tilemap CurrentMap() => PlacementManager.Instance != null
        ? PlacementManager.Instance.tilemap : null;

    static bool IsEnemy(CharacterBase character) => character is MonsterBase || character.isEnemy;

    static bool IsLiveTarget(CharacterBase character) => character != null
        && character.gameObject.activeInHierarchy && (!IsEnemy(character) || !character.IsDead);

    void RefreshAtCursor(Vector3 worldPosition, GameObject hoveredObject)
    {
        Tilemap map = CurrentMap();
        if (map == null || !map.gameObject.activeInHierarchy)
        {
            ClearDisplay();
            return;
        }

        if (hoveredObject != null && (hoveredObject.transform.IsChildOf(transform)
            || hoveredObject.GetComponentInParent<Selectable>() != null))
        {
            ClearDisplay();
            return;
        }

        Vector3Int cell = map.WorldToCell(worldPosition);
        cell.z = 0;
        if (!map.HasTile(cell))
        {
            ClearDisplay();
            return;
        }

        CharacterBase character = hoveredObject != null
            ? hoveredObject.GetComponentInParent<CharacterBase>() : null;
        if (!IsOnMap(character, map)) character = FindAtPoint(worldPosition, cell, map, false);

        TileData tile = PlacementManager.Instance.GetTileData(cell);
        if (character == null && IsOnMap(tile.Character, map)
            && CharacterCell(tile.Character, map) == cell)
            character = tile.Character;

        // 타일 중심도 검사해서 작은 스프라이트의 주변을 가리켜도 해당 유닛을 표시한다.
        if (character == null) character = FindAtPoint(map.GetCellCenterWorld(cell), cell, map, true);
        target = character;
        if (character != null) ShowCharacter(character, map);
        else ShowTile(cell, tile, map);
    }

    static Vector3Int CharacterCell(CharacterBase character, Tilemap map)
    {
        Vector3Int cell = map.WorldToCell(character.transform.position);
        cell.z = 0;
        return cell;
    }

    static bool IsOnMap(CharacterBase character, Tilemap map) => IsLiveTarget(character)
        && map.HasTile(CharacterCell(character, map));

    static CharacterBase FindAtPoint(Vector3 point, Vector3Int cell, Tilemap map, bool sameCell)
    {
        foreach (Collider2D hit in Physics2D.OverlapPointAll(point))
        {
            CharacterBase character = hit.GetComponentInParent<CharacterBase>();
            if (IsOnMap(character, map) && (!sameCell || CharacterCell(character, map) == cell))
                return character;
        }
        return null;
    }

    void ShowCharacter(CharacterBase character, Tilemap map)
    {
        MonsterBase monster = character as MonsterBase;
        MonsterData monsterData = monster != null ? monster.MonsterData : null;
        CharacterData characterData = character.Data;
        string displayName = monsterData != null ? monsterData.monsterName
            : characterData != null ? characterData.characterName : null;
        if (string.IsNullOrWhiteSpace(displayName)) displayName = character.Name;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = character.gameObject.name.Replace("(Clone)", "").Trim();

        Sprite image = characterData != null ? characterData.Portrait : null;
        if (image == null) image = character.portrait;
        if (image == null)
        {
            foreach (SpriteRenderer renderer in character.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.sprite == null) continue;
                image = renderer.sprite;
                break;
            }
        }

        string stats = IsEnemy(character)
            ? $"HP: {character.currentHP} / {character.MaxHP}"
            : $"공용 결계 HP: {BattleManager.HP:0.##}";
        string details = IsEnemy(character)
            ? monsterData != null ? $"공격력: {monsterData.atk}" : "몬스터"
            : $"행동력: {character.actionPoint}/{character.maxAP} · 이동력: {character.steminaPoint}/{character.maxStemina}";
        string position = map != null ? PositionLabel(CharacterCell(character, map)) : "";
        SetContent(image, displayName, stats, details, position);
    }

    void ShowTile(Vector3Int cell, TileData tile, Tilemap map)
    {
        bool outside = tile.Type == TileData.tiletype.outside;
        SetContent(map.GetSprite(cell), outside ? "외곽 타일" : "내부 타일",
            tile.isempty ? "비어 있음" : "장애물 있음",
            outside ? "아군 이동·배치 영역" : "몬스터 배치 영역", PositionLabel(cell));
    }

    static string PositionLabel(Vector3Int cell)
    {
        Vector2Int position = PlacementManager.Instance.ConvertToCustomPosition(cell);
        return $"좌표: ({position.x}, {position.y})";
    }

    void ClearDisplay()
    {
        target = null;
        SetContent(null, "대상 정보", "커서를 유닛이나 타일에 올리세요.", "", "");
    }

    void SetContent(Sprite image, string displayName, string stats, string details, string position)
    {
        if (portrait != null)
        {
            portrait.sprite = image;
            portrait.enabled = image != null;
            portrait.preserveAspect = true;
        }
        SetText(nameText, displayName);
        SetText(infoText, stats);
        SetText(skillText, details);
        SetText(positionText, position);
        string summary = $"<b>{displayName}</b>\n{stats}";
        if (!string.IsNullOrEmpty(details)) summary += "\n" + details;
        if (!string.IsNullOrEmpty(position)) summary += "\n" + position;
        SetText(summaryText, summary);
    }

    static void SetText(TMPro.TextMeshProUGUI text, string value)
    {
        if (text != null && text.text != value) text.SetText(value);
    }
}
