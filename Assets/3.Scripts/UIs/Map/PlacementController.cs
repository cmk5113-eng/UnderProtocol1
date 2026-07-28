using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlacementController : UI_CharacterSelectWindows
{
    private Camera mainCamera;
    static List<GameObject> _objects = new List<GameObject>();
    int count => _objects.Count;
    int max = 12;
    public TextMeshProUGUI Current;
    public TextMeshProUGUI Max;
    public static GameObject CurrentSkill;

    private void RefreshUI()
    {
        if (Current != null) Current.text = count.ToString();
        if (Max != null) Max.text = max.ToString();
    }

    void Start()
    {
        mainCamera = Camera.main;
        InitTilemap();
    }

    private bool InitTilemap()
    {
        if (PlacementManager.Instance == null) return false;

        if (PlacementManager.Instance.tilemap == null)
        {
            GameObject mainTileObj = GameObject.FindGameObjectWithTag("MainTile");
            if (mainTileObj != null)
            {
                PlacementManager.Instance.tilemap = mainTileObj.GetComponent<Tilemap>();
            }
        }

        return PlacementManager.Instance.tilemap != null;
    }

    void Update()
    {
        RefreshUI();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (!InitTilemap()) return;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            mouseWorldPos.z = 1;

            Tilemap targetTilemap = PlacementManager.Instance.tilemap;
            Vector3Int clickCellPos = targetTilemap.WorldToCell(mouseWorldPos);

            if (ModeManager.Instance != null && ModeManager.Instance.CurrentMode == ModeManager.GameMode.CharacterSelect)
            {
                if (targetTilemap.HasTile(clickCellPos))
                {
                    if (SelectionManager.SelectedPrefab != null)
                    {
                        ModeManager.Instance.ChangeMode(ModeManager.GameMode.None);

                        GameObject target = GameObject.Find(SelectionManager.SelectedPrefab.name + "(Clone)");
                        if (count >= max)
                        {
                            UIManager.ClaimPopUp("경고", "인원 초과", "확인");
                            SelectionManager.SelectedPrefab = null;
                            return;
                        }

                        if (target != null)
                        {
                            Debug.Log($"[Destroy] 이미 존재하는 {target.name} 오브젝트를 삭제합니다.");
                            ObjectManager.DestroyObject(target);
                            _objects.Remove(target);
                        }

                        Vector3 spawnPos = targetTilemap.GetCellCenterWorld(clickCellPos);
                        if (StageUIController.Instance != null) StageUIController.Instance.Refresh();

                        GameObject obj = ObjectManager.CreateObject(SelectionManager.SelectedPrefab, spawnPos);
                        _objects.Add(obj);

                        if (obj.TryGetComponent<CharacterBase>(out var character))
                        {
                            var tileData = PlacementManager.Instance.GetTileData(clickCellPos);
                            if (tileData != null)
                            {
                                tileData.Character = character;
                            }

                            SelectionManager.SetSelectedCharacter(character);
                        }

                        SelectionManager.SelectedPrefab = null;
                    }
                }
            }
        }
    }

    public static void RemoveAllObject()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
            {
                ObjectManager.DestroyObject(_objects[i]);
            }
        }
        _objects.Clear();
    }
}