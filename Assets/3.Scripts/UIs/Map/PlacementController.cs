using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using static UnityEngine.GraphicsBuffer;

public class PlacementController : UI_CharacterSelectWindows
{
    private Camera mainCamera;
    public static List<GameObject> _objects = new List<GameObject>();
    public int count => _objects.Count;
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

                        GameObject target = GameObject.Find(SelectionManager.SelectedPrefab.gameObject.name);
                        if (count >= max)
                        {
                            UIManager.ClaimPopUp("경고", "인원 초과", "확인");
                            SelectionManager.SelectedPrefab = null;
                            return;
                        }

                        if (target != null)
                        {
                            Debug.Log($"[Destroy] 이미 존재하는 {target.name} 오브젝트를 삭제합니다.");

                            // 기존 오브젝트가 있던 위치의 타일 찾기
                            Vector3Int oldCellPos = targetTilemap.WorldToCell(target.transform.position);

                            // 해당 타일 데이터 가져오기
                            var oldTileData = PlacementManager.Instance.GetTileData(oldCellPos);

                            if (oldTileData != null)
                            {
                                oldTileData.Character = null;
                                oldTileData.isempty = true;

                            }

                            // 오브젝트 삭제
                            ObjectManager.DestroyObject(target);

                            _objects.Remove(target);
                        }

                        Vector3 spawnPos = targetTilemap.GetCellCenterWorld(clickCellPos);
                        if (StageUIController.Instance != null) StageUIController.Instance.Refresh();

                        GameObject obj = ObjectManager.CreateObject(SelectionManager.SelectedPrefab, spawnPos);
                        obj.name = SelectionManager.SelectedPrefab.name;
                        Debug.Log($"{PlacementManager.Instance.tilemap}");
                        _objects.Add(obj);

                        

                        if (obj.TryGetComponent(out CharacterBase character))
                        {
                            var tileData = PlacementManager.Instance.GetTileData(clickCellPos);
                            if (tileData != null)
                            {
                                tileData.Character = character;
                            }

                            SelectionManager.SelectCharacter(character);
                            SpawnObject();
                        }

                        SelectionManager.SelectedPrefab = null;
                    }
                }
            }
        }
    }

    public static void RemoveAllObject()
    {
        SelectionManager.DeselectCharacter();

        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
            {
                GameObject obj = _objects[i];

                CharacterBase character = obj.GetComponent<CharacterBase>();
                MoveTileModule moveModule = obj.GetComponent<MoveTileModule>();

                // 1. 현재 점유하고 있는 타일 비우기
                if (moveModule != null)
                {
                    moveModule.ClearCharacterPosition();
                }

                // 2. 캐릭터 데이터 초기화
                if (character != null)
                {
                    SelectionManager.Instance.InitCharacter(character);
                }

                // 3. UI 카운트 감소
                UI_CharacterSelectWindows.Instance.RemoveCount();

                // 4. 마지막으로 삭제
                ObjectManager.DestroyObject(obj);
            }
        }

        _objects.Clear();
    }
    public void SpawnObject()
    {
        SelectionManager.CharacterBase.actionPoint = SelectionManager.CharacterBase.maxAP;
        SelectionManager.CharacterBase.steminaPoint = SelectionManager.CharacterBase.maxStemina;
        SelectionManager.CharacterBase.isSpawned = true;
    
    }
}