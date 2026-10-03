# Stage Map Editor 사용 및 검증

1. Unity에서 `Tools > Stage Map Editor`를 열고 편집할 Tilemap을 지정합니다. 비활성 맵도 타일판에서 편집할 수 있습니다.
2. Stage Binding이 없으면 에디터의 추가 버튼을 누릅니다. Stage Index는 `0 = stage1Waves`부터 `4 = stage5Waves`까지입니다.
3. 웨이브 목록에서 편집할 Wave를 선택합니다. 기존 WaveData를 슬롯에 넣거나, **새 WaveData 추가** / **이 슬롯에 WaveData 생성**을 사용합니다. 빈 슬롯 추가, `↑`/`↓` 순서 변경, `−` 제거도 지원합니다. 제거는 연결만 해제하고 에셋은 유지합니다.
4. Monster 탭에서 몬스터를 선택하고 타일판에 좌클릭 또는 드래그하여 배치합니다. 같은 칸에 다시 찍으면 교체되며 우클릭 또는 드래그하면 몬스터만 지웁니다. 빈 공간에는 배치할 수 없습니다.
5. **현재 WaveData 저장**으로 ID와 타일 좌표를 에셋에 저장하고, **웨이브 목록 저장 (Scene)**으로 Stage Binding과 WaveManager의 목록을 저장합니다. WaveData를 여러 슬롯에서 공유하면 배치 내용도 공유됩니다.

몬스터 팔레트는 해당 맵의 WaveLoader > Monster Datas를 사용합니다. 몬스터가 없으면 이 목록에 MonsterData를 등록하고, ID가 겹치지 않도록 설정하세요. 프리팹 루트에는 MonsterBase가 있어야 합니다. Scene View에서도 찍으려면 **Scene View에서도 배치**를 켭니다. Obstacle / Field Effect는 각 종류만 지우며 Erase 모드는 해당 칸의 모든 종류를 지웁니다.

## 자동 검증

```bash
python Tools/MapEditorTests/run_tests.py
```

.NET SDK가 필요합니다. `MAP_EDITOR_DOTNET`과 `MAP_EDITOR_CSC`로 경로를 지정할 수 있습니다. 실제 에디터, WaveManager, WaveData, WaveLoader 소스를 API double과 함께 C# 9로 컴파일합니다. 13개 시나리오에서 편집 모드 조회, 배치/교체/삭제, 웨이브 및 맵 간 격리, 목록 추가/제거/이동, 선택 유지, Undo 호출, 신규 에셋 연결과 취소, 저장 대상, 데이터 검증, 클릭/드래그 좌표 및 WaveLoader의 데이터 소비를 확인합니다.

이 검증은 Unity의 실제 렌더링, 임포트, Undo 엔진, 에셋/프리팹/Scene 직렬화 또는 Play Mode 테스트를 대신하지 않습니다.

## Unity 6000.3.5f2에서 확인할 항목

- 창을 열고 Play Mode 진입 전부터 연결된 WaveData가 나타나는지 확인합니다.
- 비활성 Tilemap에서 배치하고 몬스터 썸네일과 좌표를 확인합니다. 확대/스크롤 후에도 보이는 칸에 정확히 찍히는지, 타일판 밖의 클릭은 무시되는지 확인합니다.
- 배치 및 목록 변경을 Ctrl+Z/Ctrl+Y로 되돌리고 복원합니다.
- 두 저장 버튼을 사용하고 Scene과 Unity를 다시 열어 목록 순서와 배치가 유지되는지 확인합니다. 프리팹 인스턴스에서는 변경된 값이 Scene의 override로 남아야 합니다.
- 각 맵의 전투에 진입하여 목록 순서대로 선택되고 저장한 ID가 저장한 타일 좌표에 생성되는지 확인합니다. Play Mode에서는 작성 UI가 비활성화되어야 합니다.
