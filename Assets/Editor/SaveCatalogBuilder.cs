using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>에셋 GUID와 기본 장착 상태를 빌드에도 포함되는 Resources 카탈로그에 등록한다.</summary>
[InitializeOnLoad]
public static class SaveCatalogBuilder
{
    public const string CatalogPath = "Assets/Resources/SaveCatalog.asset";
    private static bool queued;

    static SaveCatalogBuilder()
    {
        QueueRebuild();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueRebuild();
        };
    }

    public static void QueueRebuild()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += RebuildWhenReady;
    }

    private static void RebuildWhenReady()
    {
        queued = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueRebuild();
            return;
        }
        Rebuild();
    }

    [MenuItem("Tools/Save/Rebuild Save Catalog")]
    public static void Rebuild()
    {
        // 플레이 중 변경된 ScriptableObject 장착 상태를 기본값으로 굳히지 않는다.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets" })
            .Distinct().OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(data => data != null)
            .Select(data => new SaveCharacterEntry
            {
                id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)),
                character = data,
                defaultActive = CopySlots(data.active, CharacterLoadoutPersistence.ActiveSlotCount),
                defaultPassive = CopySlots(data.passive, CharacterLoadoutPersistence.PassiveSlotCount)
            }).ToArray();
        var skills = AssetDatabase.FindAssets("t:SkillList", new[] { "Assets" })
            .Distinct().OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => new SaveSkillEntry
            {
                id = id,
                skill = AssetDatabase.LoadAssetAtPath<SkillList>(AssetDatabase.GUIDToAssetPath(id))
            }).Where(entry => entry.skill != null).ToArray();

        SaveCatalog catalog = AssetDatabase.LoadAssetAtPath<SaveCatalog>(CatalogPath);
        if (catalog != null && SameCharacters(catalog.characters, characters) && SameSkills(catalog.skills, skills))
            return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SaveCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.characters = characters;
        catalog.skills = skills;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Save] 저장 카탈로그 갱신: 캐릭터 {characters.Length}명, 스킬 {skills.Length}개");
    }

    private static T[] CopySlots<T>(T[] source, int count)
    {
        var target = new T[count];
        if (source != null) Array.Copy(source, target, Math.Min(source.Length, count));
        return target;
    }

    private static bool SameCharacters(SaveCharacterEntry[] left, SaveCharacterEntry[] right)
    {
        if (left == null || left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] == null || left[i].id != right[i].id || left[i].character != right[i].character
                || left[i].defaultActive == null || !left[i].defaultActive.SequenceEqual(right[i].defaultActive)
                || left[i].defaultPassive == null || !left[i].defaultPassive.SequenceEqual(right[i].defaultPassive))
                return false;
        return true;
    }

    private static bool SameSkills(SaveSkillEntry[] left, SaveSkillEntry[] right)
    {
        if (left == null || left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] == null || left[i].id != right[i].id || left[i].skill != right[i].skill) return false;
        return true;
    }
}

public class SaveCatalogAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Any(path => path != SaveCatalogBuilder.CatalogPath && path.EndsWith(".asset", StringComparison.Ordinal)))
            SaveCatalogBuilder.QueueRebuild();
    }
}

public class SaveCatalogBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        SaveCatalogBuilder.Rebuild();
        SaveCatalog catalog = AssetDatabase.LoadAssetAtPath<SaveCatalog>(SaveCatalogBuilder.CatalogPath);
        if (!new CharacterLoadoutPersistence(catalog).IsReady)
            throw new BuildFailedException("저장 카탈로그 설정을 확인해주세요.");
    }
}
