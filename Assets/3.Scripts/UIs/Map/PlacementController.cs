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
                Debug.Log($"MoveTileModule.Instance : {MoveTileModule.Instance}");
                if (targetTilemap.HasTile(clickCellPos))
                {
                    if (SelectionManager.SelectedPrefab!= null)
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

                        if (StageUIController.Instance != null)
                            StageUIController.Instance.Refresh();

                        // 클릭한 타일의 TileData 가져오기

                        TileData tileData = PlacementManager.Instance.GetTileData(clickCellPos);

                    // 타일이 비어 있고, 배치 가능한 외부 타일인지 확인
                    if (tileData != null && tileData.isempty && tileData.Type == TileData.tiletype.outside)
                    {
                        GameObject obj = ObjectManager.CreateObject(SelectionManager.SelectedPrefab, spawnPos);

                        if (obj == null)
                            return;

                        obj.name = SelectionManager.SelectedPrefab.name;

                        _objects.Add(obj);

                        if (obj.TryGetComponent(out CharacterBase character))
                            {
                                // 해당 타일에 캐릭터 등록
                                tileData.Character = character;

                                SelectionManager.SelectCharacter(character);

                                SpawnObject();
                            }



                            SelectionManager.SelectedPrefab = null;
                        }
                    }
                }
            }
        }
    }
    public void test()
    {
        Debug.Log("dfpfpfppfp");
    }
    public static void RemoveAllObject()
    {
        if (_objects == null || _objects.Count == 0) return;

        SelectionManager.DeselectCharacter();

        // 1. 리스트 복사본 생성 (반복문 도중 원본 modification 방지)
        List<GameObject> tempObjects = new List<GameObject>(_objects);
        _objects.Clear();

        // 2. 복사본으로 안전하게 순회
        for (int i = tempObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = tempObjects[i];
            if (obj == null) continue;

            MoveTileModule moveModule = obj.GetComponent<MoveTileModule>();
            if (moveModule != null)
            {
                moveModule.ClearCharacterPosition();
            }

            CharacterBase character = obj.GetComponent<CharacterBase>();
            if (character != null && SelectionManager.Instance != null)
            {
                SelectionManager.Instance.InitCharacter(character);
            }

            // 반복문 내부에서 매번 호출할 필요가 없는 정적/UI 초기화는
            // 필요에 따라 루프 외부로 빼는 것을 권장합니다.
            SelectionManager._characterBase = null;

            if (StageUIController.Instance != null)
                StageUIController.Instance.Allreset();

            if (UI_CharacterSelectWindows.Instance != null)
                UI_CharacterSelectWindows.Instance.RemoveCount();

            ObjectManager.DestroyObject(obj);
        }
    }
    public void SpawnObject()
    {
        SelectionManager.CharacterBase.actionPoint = SelectionManager.CharacterBase.maxAP;
        SelectionManager.CharacterBase.steminaPoint = SelectionManager.CharacterBase.maxStemina;
        SelectionManager.CharacterBase.isSpawned = true;
    
    }
}