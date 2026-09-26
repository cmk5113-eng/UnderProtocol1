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
 
    public void LeaveBattle()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive)
            BattleManager.Instance.AbortBattle();
        else
            RemoveAllObject();
    }

    public static void RemoveAllObject()
    {
        if (BattleManager.Instance != null) BattleManager.Instance.ResetBattle();
        if (UseSkill.Instance != null) UseSkill.Instance.ClearAllHighlights();
        SelectionManager.DeselectCharacter();
        SelectionManager._characterBase = null;
        SelectionManager._characterData = null;
        SelectionManager.SelectedPrefab = null;
        CurrentSkill = null;

        var objects = new HashSet<GameObject>();
        if (_objects != null) objects.UnionWith(_objects);
        if (MonsterBase._monsters != null) objects.UnionWith(MonsterBase._monsters);
        // 비활성 맵이나 다른 배치 경로로 생성된 전투 유닛도 정리한다.
        foreach (CharacterBase character in FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (character.isSpawned || character.gameObject.activeSelf)
                objects.Add(character.gameObject);

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            MovementModule movement = obj.GetComponent<MovementModule>();
            if (movement != null) movement.StopMovement();
            CharacterBase character = obj.GetComponent<CharacterBase>();
            if (character != null && SelectionManager.Instance != null)
                SelectionManager.Instance.InitCharacter(character);
            // Destroy는 프레임 끝에 실행되므로 재진입 시 검색/입력에서 즉시 제외한다.
            ObjectManager.DestroyObject(obj);
            obj.SetActive(false);
        }

        _objects = new List<GameObject>();
        MonsterBase._monsters.Clear();
        if (SelectionManager.Instance != null) SelectionManager.Instance.unitOnStage.Clear();
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.tileDatas.Clear();
            PlacementManager.Instance.tilemap = null;
        }

        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        if (wave != null)
        {
            wave.selectedWaves = null;
            wave.currentWave = null;
            wave.currentWaveIndex = 0;
        }

        if (ScrollUI.Instance != null) ScrollUI.Instance.ResetValue();
        if (StageUIController.Instance != null) StageUIController.Instance.Allreset();
        if (UI_CharacterSelectWindows.Instance != null) UI_CharacterSelectWindows.Instance.RemoveCount();
        if (ModeManager.Instance != null) ModeManager.Instance.ChangeMode(ModeManager.GameMode.None);
    }
    public void SpawnObject()
    {
        SelectionManager.CharacterBase.actionPoint = SelectionManager.CharacterBase.maxAP;
        SelectionManager.CharacterBase.steminaPoint = SelectionManager.CharacterBase.maxStemina;
        SelectionManager.CharacterBase.isSpawned = true;
    
    }
}
