# Stage Map Editor 사용 및 검증

1. Unity에서 `Tools > Stage Map Editor`를 열고 편집할 Tilemap을 지정합니다. 비활성 맵도 타일판에서 편집할 수 있습니다.
2. Stage Binding이 없으면 에디터의 추가 버튼을 누릅니다. Stage Index는 스테이지 목록의 0부터 시작하는 번호입니다. 목록을 늘리면 5, 6, 7…도 사용할 수 있습니다. `-1`은 연결 해제 상태입니다.
3. 웨이브 목록에서 편집할 Wave를 선택합니다. 기존 WaveData를 슬롯에 넣거나, **새 WaveData 추가** / **이 슬롯에 WaveData 생성**을 사용합니다. 빈 슬롯 추가, `↑`/`↓` 순서 변경, `−` 제거도 지원합니다. 제거는 연결만 해제하고 에셋은 유지합니다.
4. Monster 탭에서 몬스터를 선택하고 타일판에 좌클릭 또는 드래그하여 배치합니다. 같은 칸에 다시 찍으면 교체되며 우클릭 또는 드래그하면 몬스터만 지웁니다. 빈 공간에는 배치할 수 없습니다.
5. **현재 WaveData 저장**으로 ID와 타일 좌표를 에셋에 저장하고, **스테이지/웨이브 목록 저장 (Scene)** 또는 **웨이브 목록 저장 (Scene)**으로 Stage Binding과 WaveManager의 목록을 저장합니다. WaveData를 여러 슬롯에서 공유하면 배치 내용도 공유됩니다.

## 스테이지 목록 추가·삭제

- WaveManager 인스펙터의 **Stage 추가** / 각 항목의 **Stage 삭제**를 사용합니다. 각 Stage의 Waves를 펼쳐 웨이브 에셋도 수정할 수 있습니다. 새 Stage는 빈 웨이브 목록으로 생성됩니다.
- 맵 에디터의 **Stage 추가**는 새 항목을 만들고 현재 Tilemap에 연결합니다. **현재 Stage 삭제**는 선택한 맵에 연결된 항목을 제거합니다. Stage가 0개일 때도 다시 추가할 수 있습니다.
- 삭제한 Stage를 참조하던 열린 Scene의 StageMapBinding과 WaveSetter 웨이브 연결은 `-1`로 해제합니다. 뒤쪽 연결 번호는 한 칸씩 앞으로 옮깁니다. WaveSetter의 기본 웨이브 번호 `index`와 클리어 기록 번호 `stageId`는 유지하며, 맵 Binding이 없는 버튼은 별도 웨이브 번호로 연결합니다. Ctrl+Z로 목록과 보정된 연결을 함께 복원할 수 있습니다.
- 기존 `stage1Waves`~`stage5Waves` 연결은 새 목록에 한 번 이관합니다. 새 목록을 비워도 이전 5개 항목이 다시 생성되지 않습니다. 저장 후 Scene을 다시 열어 연결을 확인하세요.
- 자동 인덱스 보정은 현재 로드된 Scene 객체에 적용됩니다. 다른 Scene이나 프리팹 에셋에 저장한 연결은 해당 파일에서 확인해야 합니다. 여러 Scene이 열려 있으면 수정된 Scene도 각각 저장하세요.
- 새 전투 버튼은 `StageButtonImageController > Requred Progress`로 필요 진행도를 설정합니다. 버튼 표시와 WaveSetter의 실제 입장이 같은 값을 사용하며, tempcontroller의 필요 진행도 배열은 사용하지 않습니다. 0이면 진행도 제한 없이 열립니다.

몬스터 팔레트는 해당 맵의 WaveLoader > Monster Datas를 사용합니다. 몬스터가 없으면 이 목록에 MonsterData를 등록하고, ID가 겹치지 않도록 설정하세요. 프리팹 루트에는 MonsterBase가 있어야 합니다. Scene View에서도 찍으려면 **Scene View에서도 배치**를 켭니다. Obstacle / Field Effect는 각 종류만 지우며 Erase 모드는 해당 칸의 모든 종류를 지웁니다.

## 자동 검증

```bash
python Tools/MapEditorTests/run_tests.py
```

.NET SDK가 필요합니다. `MAP_EDITOR_DOTNET`과 `MAP_EDITOR_CSC`로 경로를 지정할 수 있습니다. 실제 에디터, WaveManager, WaveData, WaveLoader, WaveSetter, StageButtonImageController 소스를 API double과 함께 C# 9로 컴파일합니다. 24개 시나리오에서 기존 맵/웨이브 편집 검증, 5개 기존 배열 이관, 새 컴포넌트 버전 표시, 30개 스테이지 작성과 전투 진입, 스테이지 삭제 후 연결 보정과 번호 보존, 삭제 후 빈 목록 유지, 연결 해제 및 복원, 잘못된 번호와 Play Mode 편집 차단을 확인합니다. 버튼별 필요 진행도의 경계(99/100/101), tempcontroller 무시, 직접 함수 호출의 입장 검사, 버튼별 조건 격리, 진행도/클리어 기록 변경 시 표시 갱신도 포함합니다.

이 검증은 Unity의 실제 렌더링, 임포트, Undo 엔진, 에셋/프리팹/Scene 직렬화 또는 Play Mode 테스트를 대신하지 않습니다.

## Unity 6000.3.5f2에서 확인할 항목

- 창을 열고 Play Mode 진입 전부터 연결된 WaveData가 나타나는지 확인합니다.
- 비활성 Tilemap에서 배치하고 몬스터 썸네일과 좌표를 확인합니다. 확대/스크롤 후에도 보이는 칸에 정확히 찍히는지, 타일판 밖의 클릭은 무시되는지 확인합니다.
- 배치 및 목록 변경을 Ctrl+Z/Ctrl+Y로 되돌리고 복원합니다.
- 두 저장 버튼을 사용하고 Scene과 Unity를 다시 열어 목록 순서와 배치가 유지되는지 확인합니다. 프리팹 인스턴스에서는 변경된 값이 Scene의 override로 남아야 합니다.
- 각 맵의 전투에 진입하여 목록 순서대로 선택되고 저장한 ID가 저장한 타일 좌표에 생성되는지 확인합니다. Play Mode에서는 작성 UI가 비활성화되어야 합니다.
- 기존 5개 Stage의 WaveData가 같은 순서로 이관되는지 확인합니다. 6개 이상 Stage를 추가하고 저장·재시작한 후에도 목록이 유지되어야 합니다.
- 중간 Stage를 삭제한 뒤 모든 로드된 맵과 전투 버튼의 연결을 확인합니다. 삭제한 연결은 `-1`, 뒤쪽 연결은 이전보다 하나 작은 번호여야 하며, WaveSetter의 기본 웨이브/클리어 번호는 같아야 합니다. Ctrl+Z/Ctrl+Y로 한 번에 복원·삭제되는지 확인합니다.
- 기존 Scene/Prefab의 `Requred Progress` 값이 유지되는지 확인합니다. 100으로 설정한 버튼은 진행도 99에서 잠기고 100부터 열려야 합니다. 다른 저장 슬롯을 로드했을 때 버튼의 잠김/미클리어/클리어 표시와 실제 입장 조건이 일치해야 합니다.
