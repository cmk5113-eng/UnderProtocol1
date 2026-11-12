#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillList), true)]
public class SkillListEditor : Editor
{
    private const int GridSize = 11;
    private const float CellSize = 30f;

    private enum PatternMode { Range, ROE }
    private PatternMode mode;
    private Vector2Int? selectedRoeTile;
    private bool showConditions = true, showTargeting = true, showEffects = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawCommon();
        DrawConditions();
        DrawTargeting();
        DrawEffects();
        if (target is PassiveSkill)
        {
            DrawPassiveSettings();
        }
        else
        {
            EnsureDefaultRangePattern((SkillList)target);
            EditorGUILayout.Space(8);
            DrawPatternEditor();
        }
        serializedObject.ApplyModifiedProperties();
    }


    private void DrawConditions()
    {
        EditorGUILayout.Space(10);
        showConditions = EditorGUILayout.Foldout(showConditions, "02. 사용 / 발동 조건", true);
        if (!showConditions) return;
        EditorGUI.indentLevel++;
        DrawProperty("restrictClasses", "클래스 제한 사용");
        SerializedProperty restricted = serializedObject.FindProperty("restrictClasses");
        if (restricted != null && restricted.boolValue)
            DrawProperty("allowedClasses", "사용 가능 클래스 (복수 선택)");

        SerializedProperty numbers = serializedObject.FindProperty("allowedCharacterNumbers");
        if (numbers != null)
        {
            EditorGUILayout.LabelField("사용 가능 캐릭터 (미선택 = 제한 없음)");
            for (int row = 0; row < 3; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int col = 0; col < 4; col++)
                    {
                        int num = row * 4 + col + 1;
                        bool exists = false;
                        for (int n = 0; n < numbers.arraySize; n++)
                        {
                            if (numbers.GetArrayElementAtIndex(n).intValue == num)
                            {
                                exists = true;
                                break;
                            }
                        }
                        bool selected = GUILayout.Toggle(exists, "캐릭터 " + num, "Button");
                        if (selected == exists) continue;
                        if (selected)
                        {
                            numbers.InsertArrayElementAtIndex(numbers.arraySize);
                            numbers.GetArrayElementAtIndex(numbers.arraySize - 1).intValue = num;
                        }
                        else
                        {
                            for (int n = numbers.arraySize - 1; n >= 0; n--)
                                if (numbers.GetArrayElementAtIndex(n).intValue == num)
                                    numbers.DeleteArrayElementAtIndex(n);
                        }
                    }
                }
            }
        }

        DrawProperty("activationTrigger", "발동 트리거");
        DrawProperty("conditionLogic", "조건 조합 (AND / OR)");
        SerializedProperty conditions = serializedObject.FindProperty("activationConditions");
        if (conditions != null)
        {
            EditorGUILayout.LabelField("발동 조건 목록", EditorStyles.boldLabel);
            for (int i = 0; i < conditions.arraySize; i++)
            {
                SerializedProperty item = conditions.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("조건 " + (i + 1));
                        if (GUILayout.Button("삭제", GUILayout.Width(52)))
                        {
                            conditions.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("conditionKey"), new GUIContent("조건 키"));
                    SerializedProperty kind = item.FindPropertyRelative("valueType");
                    EditorGUILayout.PropertyField(kind, new GUIContent("값 종류"));
                    if (kind.enumValueIndex != (int)SkillValueType.None)
                    {
                        EditorGUILayout.PropertyField(item.FindPropertyRelative("comparison"), new GUIContent("비교"));
                        if (kind.enumValueIndex == (int)SkillValueType.Integer)
                            EditorGUILayout.PropertyField(item.FindPropertyRelative("intValue"), new GUIContent("정수 값"));
                        else
                            EditorGUILayout.PropertyField(item.FindPropertyRelative("boolValue"), new GUIContent("참 / 거짓"));
                    }
                }
            }
            if (GUILayout.Button("+ 조건 추가")) conditions.InsertArrayElementAtIndex(conditions.arraySize);
        }
        DrawProperty("activationChance", "발동 확률 (%)");
        EditorGUI.indentLevel--;
    }

    private void DrawTargeting()
    {
        EditorGUILayout.Space(10);
        showTargeting = EditorGUILayout.Foldout(showTargeting, "03. 타겟 / 공격 틱", true);
        if (!showTargeting) return;
        EditorGUI.indentLevel++;
        DrawProperty("targetSelection", "타겟 선정 방식");
        DrawProperty("targetCount", "타겟수 (1틱당, 0 = 기존 전체)");
        DrawProperty("minAttackTicks", "최소공격횟수 (틱)");
        DrawProperty("maxAttackTicks", "최대공격횟수 (틱)");
        SerializedProperty min = serializedObject.FindProperty("minAttackTicks");
        SerializedProperty max = serializedObject.FindProperty("maxAttackTicks");
        min.intValue = Mathf.Max(1, min.intValue);
        max.intValue = Mathf.Max(min.intValue, max.intValue);
        EditorGUILayout.HelpBox(
            "ROECount 입력란은 사용하지 않습니다. 기존 타일 패턴 편집기로 범위를 지정합니다. " +
            "타겟수는 매 틱 공격할 인원, 최소/최대공격횟수는 공격 틱 수입니다. " +
            "신규 타겟/틱 설정의 실제 전투 처리는 아직 연결되지 않았습니다.", MessageType.Info);
        EditorGUI.indentLevel--;
    }

    private void DrawEffects()
    {
        EditorGUILayout.Space(10);
        showEffects = EditorGUILayout.Foldout(showEffects, "04. 복합 효과 / 지속 조건", true);
        if (!showEffects) return;
        EditorGUI.indentLevel++;
        DrawProperty("durationType", "기본 지속조건");
        SerializedProperty duration = serializedObject.FindProperty("durationType");
        if (duration != null &&
            (duration.enumValueIndex == (int)SkillDurationType.Turns ||
             duration.enumValueIndex == (int)SkillDurationType.Uses))
            DrawProperty("durationValue", "기본 지속 수치");
        DrawProperty("maxActivationsPerTurnForSkill", "턴당 최대 발동횟수 (0 = 무제한)");

        SerializedProperty list = serializedObject.FindProperty("configuredEffects");
        if (list != null)
        {
            EditorGUILayout.LabelField("효과 목록 (위에서 아래 순서)", EditorStyles.boldLabel);
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    bool changed = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("효과 " + (i + 1), EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("▲", GUILayout.Width(30)))
                            { list.MoveArrayElement(i, i - 1); changed = true; }
                        using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
                            if (GUILayout.Button("▼", GUILayout.Width(30)))
                            { list.MoveArrayElement(i, i + 1); changed = true; }
                        if (GUILayout.Button("삭제", GUILayout.Width(48)))
                        { list.DeleteArrayElementAtIndex(i); changed = true; }
                    }
                    if (changed) break;
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("kind"), new GUIContent("효과 종류"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("timing"), new GUIContent("발동 시점"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("target"), new GUIContent("효과 대상"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("chance"), new GUIContent("발동 확률 (%)"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("delayTurns"), new GUIContent("지연 발동 턴"));
                    SerializedProperty durationKind = item.FindPropertyRelative("duration");
                    EditorGUILayout.PropertyField(durationKind, new GUIContent("지속 조건"));
                    if (durationKind.enumValueIndex == (int)SkillDurationType.Turns ||
                        durationKind.enumValueIndex == (int)SkillDurationType.Uses)
                        EditorGUILayout.PropertyField(item.FindPropertyRelative("durationValue"), new GUIContent("지속 수치"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("magnitude"), new GUIContent("위력 / 변화량"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("armorDamage"), new GUIContent("아머 데미지"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("element"), new GUIContent("속성"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("fieldType"), new GUIContent("필드효과"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("fieldTurns"), new GUIContent("필드 지속 턴"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("fieldPower"), new GUIContent("필드 위력"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("status"), new GUIContent("상태이상"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("statusStacks"), new GUIContent("상태이상 스택"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("movementDirection"), new GUIContent("이동 방향"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("movementTiles"), new GUIContent("이동 칸수"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("detailKey"), new GUIContent("상세 키"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("notes"), new GUIContent("비고"));
                }
            }
            if (GUILayout.Button("+ 효과 추가")) list.InsertArrayElementAtIndex(list.arraySize);
        }
        EditorGUILayout.HelpBox("추가 효과는 데이터 입력용입니다. 실제 전투 적용은 ExecuteSkill 연결이 필요합니다.", MessageType.Info);
        EditorGUI.indentLevel--;
    }

    private void DrawPassiveSettings()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("패시브 발동 / 수치", EditorStyles.boldLabel);
        DrawProperty("trigger", "발동 시점");
        DrawProperty("passiveEffect", "패시브 효과");
        DrawProperty("maxActivationsPerTurn", "턴당 발동 횟수 (0 = 무제한)");
        DrawProperty("passiveDamage", "추가 공격 / 지속 피해량");
        DrawProperty("movementPoints", "이동 횟수 회복량");
        DrawProperty("extraActions", "추가 행동 횟수");
        DrawProperty("durationTurns", "지속 적 턴 수");
        DrawProperty("bombRadius", "폭탄 반경 (0 = 한 칸)");
        EditorGUILayout.HelpBox(
            "CharacterData의 Staticpassive 또는 Passive 슬롯에 연결하면 배틀매니저가 자동 실행합니다. " +
            "양옆은 외곽의 인접 아군, 맞은편은 반대 가장자리의 같은 행/열입니다. " +
            "이동 보너스는 Stemina Point에 적용되고, 폭탄/화상은 적 행동 전에 피해를 줍니다. " +
            "None은 효과 없음이며 패시브 추가 공격은 공격 패시브를 연쇄 발동하지 않습니다.",
            MessageType.Info);
    }

    private void DrawCommon()
    {
        EditorGUILayout.LabelField("공통", EditorStyles.boldLabel);
        DrawProperty("skillName", "이름");
        DrawProperty("description", "설명");
        DrawProperty("id", "ID");
        DrawProperty("icon", "아이콘");
        DrawProperty("type", "스킬 종류");
        DrawProperty("classType", "클래스");
        DrawProperty("elementType", "속성");
        DrawProperty("targetType", "대상");
        DrawProperty("effectType", "효과");
        DrawProperty("cost", "코스트");
        DrawProperty("condition", "조건");
        DrawProperty("canRotate", "회전 가능");
        DrawProperty("cooldown", "쿨다운");
        DrawProperty("delay", "딜레이");
        DrawProperty("level", "레벨");
        DrawProperty("MaxLevel", "최대 레벨");
        DrawProperty("skillsList", "연결 스킬");
    }

    private void EnsureDefaultRangePattern(SkillList skill)
    {
        if (skill == null ||
            skill.rangePattern == null ||
            skill.rangePattern.Count > 0)
        {
            return;
        }

        // Existing skills with legacy range data keep their old fallback behavior.
        if (skill.range > 0)
            return;

        Undo.RecordObject(skill, "Create Default Skill Range");

        const int defaultRadius = 3;

        for (int x = -defaultRadius; x <= defaultRadius; x++)
        {
            for (int y = -defaultRadius; y <= defaultRadius; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (Mathf.Abs(x) + Mathf.Abs(y) <= defaultRadius)
                    skill.rangePattern.Add(new Vector2Int(x, y));
            }
        }

        EditorUtility.SetDirty(skill);
    }

    private void DrawPatternEditor()
    {
        SkillList skill = (SkillList)target;
        EditorGUILayout.LabelField("타일 패턴", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Toggle(mode == PatternMode.Range, "사정거리", "Button"))
                mode = PatternMode.Range;
            if (GUILayout.Toggle(mode == PatternMode.ROE, "ROE", "Button"))
                mode = PatternMode.ROE;
        }

        EditorGUILayout.HelpBox(
            mode == PatternMode.Range
                ? "중앙 C는 시전자입니다. 타일을 클릭해 시전 가능한 위치를 켜거나 끕니다."
                : "중앙 O는 현재 선택 타일(Pivot)이며 공격 타일로도 사용할 수 있습니다. 좌클릭하면 데미지가 1 → 2 → 3 → 1로 순환하며, 우클릭은 타일을 켜거나 끕니다.",
            MessageType.Info);

        DrawGrid(skill);
        DrawPresetButtons(skill);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("패턴 초기화"))
            {
                Undo.RecordObject(skill, "Clear Skill Pattern");
                if (mode == PatternMode.Range) skill.rangePattern.Clear();
                else { skill.roePattern.Clear(); selectedRoeTile = null; }
                EditorUtility.SetDirty(skill);
            }

            if (GUILayout.Button("좌우 반전"))
                MirrorPattern(skill);
        }

        if (mode == PatternMode.ROE && selectedRoeTile.HasValue)
            DrawSelectedRoeTile(skill, selectedRoeTile.Value);
    }


    private void DrawPresetButtons(SkillList skill)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("자주 쓰는 모양", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("직선 4"))
                ApplyPreset(skill, new[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(4, 0)
                });

            if (GUILayout.Button("3×3"))
                ApplyPreset(skill, MakeRectangle(1, 3, -1, 1));

            if (GUILayout.Button("십자"))
                ApplyPreset(skill, new[]
                {
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0)
                });

            if (GUILayout.Button("부채꼴"))
                ApplyPreset(skill, new[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(2, -1),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, 1),
                    new Vector2Int(3, -2),
                    new Vector2Int(3, -1),
                    new Vector2Int(3, 0),
                    new Vector2Int(3, 1),
                    new Vector2Int(3, 2)
                });
        }

        EditorGUILayout.LabelField(
            "프리셋을 누르면 현재 탭의 패턴을 교체합니다.",
            EditorStyles.miniLabel);
    }

    private Vector2Int[] MakeRectangle(int minX, int maxX, int minY, int maxY)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
                cells.Add(new Vector2Int(x, y));
        }

        return cells.ToArray();
    }

    private void ApplyPreset(SkillList skill, Vector2Int[] cells)
    {
        Undo.RecordObject(skill, "Apply Skill Pattern Preset");

        if (mode == PatternMode.Range)
        {
            skill.rangePattern.Clear();
            skill.rangePattern.AddRange(cells);
        }
        else
        {
            skill.roePattern.Clear();

            foreach (Vector2Int position in cells)
            {
                skill.roePattern.Add(new SkillPatternTile
                {
                    position = position,
                    distanceFromCaster = Mathf.Abs(position.x) + Mathf.Abs(position.y),
                    damage = skill.damage,
                    pushDistance = skill.pushDistance
                });
            }

            selectedRoeTile = null;
        }

        EditorUtility.SetDirty(skill);
    }

    private void DrawGrid(SkillList skill)
    {
        int half = GridSize / 2;
        EditorGUILayout.LabelField("기준 방향  →  (+X)", EditorStyles.centeredGreyMiniLabel);

        for (int y = half; y >= -half; y--)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                for (int x = -half; x <= half; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    bool pivot = position == Vector2Int.zero;
                    bool active = IsActive(skill, position);
                    bool selected = selectedRoeTile.HasValue && selectedRoeTile.Value == position && mode == PatternMode.ROE;
                    string label = pivot ? (mode == PatternMode.Range ? "C" : "O") : GetCellLabel(skill, position, active);

                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    if (active) style.fontStyle = FontStyle.Bold;
                    if (selected) style.fontSize = 13;

                    bool canToggle = mode == PatternMode.ROE || !pivot;

                    if (GUILayout.Button(
                            label,
                            style,
                            GUILayout.Width(CellSize),
                            GUILayout.Height(CellSize)) &&
                        canToggle)
                    {
                        if (mode == PatternMode.ROE && Event.current.button == 0)
                            CycleRoeTileDamage(skill, position);
                        else
                            ToggleCell(skill, position);
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }
    }

    private string GetCellLabel(SkillList skill, Vector2Int position, bool active)
    {
        if (!active) return "□";
        if (mode == PatternMode.Range) return "■";

        SkillPatternTile tile = skill.roePattern.FirstOrDefault(t => t.position == position);
        return tile != null && tile.damage != 0 ? tile.damage.ToString() : "■";
    }

    private bool IsActive(SkillList skill, Vector2Int position)
    {
        return mode == PatternMode.Range
            ? skill.rangePattern.Contains(position)
            : skill.roePattern.Any(t => t.position == position);
    }

    private void CycleRoeTileDamage(SkillList skill, Vector2Int position)
    {
        Undo.RecordObject(skill, "Cycle ROE Tile Damage");

        SkillPatternTile tile = skill.roePattern.FirstOrDefault(t => t.position == position);
        if (tile == null)
        {
            tile = new SkillPatternTile
            {
                position = position,
                distanceFromCaster = Mathf.Abs(position.x) + Mathf.Abs(position.y),
                damage = 1,
                pushDistance = skill.pushDistance
            };
            skill.roePattern.Add(tile);
        }
        else
        {
            tile.damage = tile.damage >= 1 && tile.damage < 3 ? tile.damage + 1 : 1;
        }

        selectedRoeTile = position;
        EditorUtility.SetDirty(skill);
    }

    private void ToggleCell(SkillList skill, Vector2Int position)
    {
        Undo.RecordObject(skill, "Edit Skill Pattern");

        if (mode == PatternMode.Range)
        {
            if (skill.rangePattern.Contains(position)) skill.rangePattern.Remove(position);
            else skill.rangePattern.Add(position);
        }
        else
        {
            SkillPatternTile existing = skill.roePattern.FirstOrDefault(t => t.position == position);
            if (existing != null)
            {
                skill.roePattern.Remove(existing);
                if (selectedRoeTile == position) selectedRoeTile = null;
            }
            else
            {
                skill.roePattern.Add(new SkillPatternTile
                {
                    position = position,
                    distanceFromCaster = Mathf.Abs(position.x) + Mathf.Abs(position.y),
                    damage = skill.damage,
                    pushDistance = skill.pushDistance
                });
                selectedRoeTile = position;
            }
        }

        EditorUtility.SetDirty(skill);
    }

    private void DrawSelectedRoeTile(SkillList skill, Vector2Int position)
    {
        SkillPatternTile tile = skill.roePattern.FirstOrDefault(t => t.position == position);
        if (tile == null) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"ROE 선택 타일 ({position.x}, {position.y})", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Vector2IntField("좌표", tile.position);
        EditorGUILayout.IntField("시전자로부터 거리 (고정)", tile.distanceFromCaster);
        EditorGUI.EndDisabledGroup();

        tile.damage = EditorGUILayout.IntField("데미지", tile.damage);
        tile.appliesDebuff = EditorGUILayout.Toggle("디버프", tile.appliesDebuff);
        if (tile.appliesDebuff)
            tile.debuffType = (SkillStatusEffectType)EditorGUILayout.EnumPopup("디버프 종류", tile.debuffType);

        tile.fieldEffect = (SkillTileFieldEffectType)EditorGUILayout.EnumPopup("필드 효과", tile.fieldEffect);
        if (tile.fieldEffect != SkillTileFieldEffectType.None)
        {
            tile.fieldEffectValue = Mathf.Max(1, EditorGUILayout.IntField("필드 세기", tile.fieldEffectValue));
            tile.fieldEffectDuration = Mathf.Max(1, EditorGUILayout.IntField("필드 지속 턴", tile.fieldEffectDuration));
            if (tile.fieldEffect == SkillTileFieldEffectType.Wind)
                tile.fieldPushDirection = (SkillPushDirection)EditorGUILayout.EnumPopup("바람 방향", tile.fieldPushDirection);
            EditorGUILayout.HelpBox("화염·전기: 턴 피해 / 얼음: 적 행동 차단 / 바람: 밀치기 / 대지·암흑: 적의 결계 피해 감소. 같은 칸은 새 필드로 교체합니다.", MessageType.Info);
        }
        tile.push = EditorGUILayout.Toggle("밀치기", tile.push);

        if (tile.push)
        {
            tile.pushDirection = (SkillPushDirection)EditorGUILayout.EnumPopup("어디로 밀칠까", tile.pushDirection);
            tile.pushTargetCount = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("몇 명 밀칠까", "0이면 제한 없음"), tile.pushTargetCount));
            tile.pushDistance = Mathf.Max(0, EditorGUILayout.IntField("몇 칸 밀칠까", tile.pushDistance));
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(skill, "Edit ROE Tile");
            EditorUtility.SetDirty(skill);
        }
    }

    private void MirrorPattern(SkillList skill)
    {
        Undo.RecordObject(skill, "Mirror Skill Pattern");

        if (mode == PatternMode.Range)
        {
            for (int i = 0; i < skill.rangePattern.Count; i++)
            {
                Vector2Int p = skill.rangePattern[i];
                skill.rangePattern[i] = new Vector2Int(p.x, -p.y);
            }
        }
        else
        {
            foreach (SkillPatternTile tile in skill.roePattern)
            {
                tile.position = new Vector2Int(tile.position.x, -tile.position.y);
                tile.distanceFromCaster = Mathf.Abs(tile.position.x) + Mathf.Abs(tile.position.y);
            }

            if (selectedRoeTile.HasValue)
            {
                Vector2Int p = selectedRoeTile.Value;
                selectedRoeTile = new Vector2Int(p.x, -p.y);
            }
        }

        EditorUtility.SetDirty(skill);
    }

    private void DrawProperty(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }
}
#endif
