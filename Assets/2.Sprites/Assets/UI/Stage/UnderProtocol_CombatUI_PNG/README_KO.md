# Under Protocol 전투 UI PNG 팩

사용자가 선택한 예시 화면의 UI를 Unity용 부품으로 정리한 팩입니다.
배경과 타일맵은 제외했으며, 32개 UI 부품은 실제 알파 투명 배경입니다.

## 포함 파일

- `Assets/UnderProtocol_CombatUI/Panels`: 프레임·버튼·정보창 16개.
- `Assets/UnderProtocol_CombatUI/Icons`: 스킬·상태 아이콘, 체력 게이지 부품, 단축키 배지 16개.
- `Assets/UnderProtocol_CombatUI/PortraitSamples`: 선택한 원본 화면에서 잘라낸 초상화 샘플 7개.
- `Assets/UnderProtocol_CombatUI/Sheets/UPUI_All_Sprites.png`: 39개를 모은 2048×512 시트와 슬라이스 `.meta`.
- `Preview/UI_ContactSheet.png`: 이름과 크기를 표시한 구성 목록.
- `Preview/Reference_StageScreen.png`: 사용자가 선택한 원본 화면 그대로인 배치 참고용 이미지.
- `ui_text_reference.json`: 원본 화면의 문구·숫자 예시. UI에 자동 연결되는 데이터는 아닙니다.
- `sprite_manifest.json`: 개별 PNG 크기, 시트 좌표, 9-slice 테두리 정보.

## Unity 적용

1. 압축 안의 `Assets/UnderProtocol_CombatUI` 폴더와 같은 이름의 폴더 `.meta`를 프로젝트 `Assets`에 복사합니다. 각 PNG 옆의 `.meta`도 함께 넣습니다.
2. Canvas 아래에 UI → Image를 만들고, `Source Image`에 원하는 개별 PNG 스프라이트를 넣습니다. 버튼은 Button의 Image에 배경을 넣고, 아이콘을 자식 Image로 겹치면 됩니다.
3. UI 크기는 Rect Transform의 Width·Height로 조정합니다. 제공한 PNG 크기는 1672×941 예시 화면에서의 사용 크기를 기준으로 정리했습니다. 원본 크기로 시작할 때 Image의 Set Native Size를 사용할 수 있습니다.
4. 확장 가능한 프레임은 Image Type을 Sliced로 설정합니다. 이 팩은 `.meta`에 9-slice Border를 넣었습니다. 구분선이 들어간 WaveTurn/PortraitStrip과 아이콘은 Simple을 사용합니다.
5. 체력바는 Gauge_HealthTrack 위에 Gauge_HealthFill을 겹칩니다. Fill의 Image Type을 Filled, Fill Method를 Horizontal, Origin을 Left로 놓고 Fill Amount를 조절합니다.
6. 프레임의 제목·수치·스킬 이름·버튼 문구는 TextMeshPro로 올립니다. 단축키 배지도 빈 배경이며, 숫자는 TextMeshPro로 추가합니다.

Canvas Scaler의 Scale With Screen Size를 사용한다면 Reference Resolution을 작업 중인 기준 해상도에 맞춥니다. 이 예시 화면을 기준으로 시작할 때는 1672×941을 사용할 수 있습니다. UI의 PPU는 100으로 설정했습니다.

## 초상화 샘플에 관한 구분

32개 UI 부품은 글자·숫자·초상화를 제외하고 원본 디자인을 참고해 재구성했습니다. 초상화 7개는 원본 스크린샷의 픽셀을 그대로 잘랐으므로 원래 프레임·선택 상태·어둡기 등이 포함되어 있습니다. Slot04와 Slot06에는 원본의 '행동 완료' 문구가 남아 있습니다. 초상화 샘플은 참고용이며, 실제 게임에서는 보유한 CharacterData의 고해상도 portrait를 새 프레임 안에 배치하면 됩니다.

PNG 시트와 개별 PNG는 같은 그림입니다. 편한 쪽 하나를 선택해서 사용하면 됩니다.

## 스프라이트 목록

| 번호 | 파일 | 용도 | 크기(px) |
| --- | --- | --- | --- |
| 01 | UPUI_Panel_StageTitle.png | 스테이지 제목 패널 | 300×76 |
| 02 | UPUI_Panel_Objective.png | 목표 패널 | 330×52 |
| 03 | UPUI_Panel_WaveTurn.png | 웨이브·적·턴 정보 패널 | 592×58 |
| 04 | UPUI_Panel_BaseHealth.png | 거점 체력 정보 패널 | 454×58 |
| 05 | UPUI_Panel_CharacterInfo.png | 선택 캐릭터 정보 패널 | 590×172 |
| 06 | UPUI_Panel_SkillTooltip.png | 스킬 설명 패널 | 805×50 |
| 07 | UPUI_Panel_PortraitStrip.png | 6인 초상화 목록 배경 | 464×84 |
| 08 | UPUI_Panel_TurnState.png | 현재 턴 표시 패널 | 194×38 |
| 09 | UPUI_Frame_PortraitNormal.png | 일반 초상화 프레임 | 76×84 |
| 10 | UPUI_Frame_PortraitSelected.png | 선택 초상화 프레임 | 76×84 |
| 11 | UPUI_Frame_PortraitLarge.png | 큰 초상화 프레임 | 168×168 |
| 12 | UPUI_Frame_SkillNormal.png | 일반 스킬 슬롯 | 146×146 |
| 13 | UPUI_Frame_SkillSelected.png | 선택 스킬 슬롯 | 158×158 |
| 14 | UPUI_Frame_PassiveDisabled.png | 패시브·비활성 슬롯 | 118×90 |
| 15 | UPUI_Button_EndTurn.png | 턴 종료 버튼 배경 | 194×146 |
| 16 | UPUI_Button_Pause.png | 일시정지 버튼 배경 | 64×64 |
| 17 | UPUI_Icon_Pause.png | 일시정지 | 28×32 |
| 18 | UPUI_Icon_Objective.png | 목표 마름모 | 24×24 |
| 19 | UPUI_Icon_JobReticle.png | 직업 조준선 | 28×28 |
| 20 | UPUI_Icon_ActionPoint.png | 행동력 | 30×30 |
| 21 | UPUI_Icon_Movement.png | 이동력 | 28×28 |
| 22 | UPUI_Icon_BasicAttack.png | 기본 공격 쌍권총 | 98×98 |
| 23 | UPUI_Icon_SkillDash.png | 범용 스킬 질주 | 124×110 |
| 24 | UPUI_Icon_SkillRadar.png | 범용 스킬 레이더 | 110×110 |
| 25 | UPUI_Icon_Ultimate.png | 궁극기 궤도 별 | 114×114 |
| 26 | UPUI_Icon_PassiveRun.png | 패시브 달리기 | 60×60 |
| 27 | UPUI_Icon_EndTurn.png | 턴 종료 화살표 | 48×42 |
| 28 | UPUI_Divider_Horizontal.png | 가로 구분선 | 384×4 |
| 29 | UPUI_Gauge_HealthTrack.png | 체력 게이지 빈 바탕 | 216×20 |
| 30 | UPUI_Gauge_HealthFill.png | 체력 게이지 채움 | 208×12 |
| 31 | UPUI_Badge_KeyDark.png | 단축키 어두운 배지 | 28×28 |
| 32 | UPUI_Badge_KeyLight.png | 단축키 밝은 배지 | 28×28 |
| 33 | UPUI_PortraitSample_Slot01.png | 원본 화면 초상화 샘플 1 | 75×81 |
| 34 | UPUI_PortraitSample_Slot02.png | 원본 화면 초상화 샘플 2 | 73×81 |
| 35 | UPUI_PortraitSample_Slot03.png | 원본 화면 초상화 샘플 3 | 70×81 |
| 36 | UPUI_PortraitSample_Slot04.png | 원본 화면 초상화 샘플 4 | 72×81 |
| 37 | UPUI_PortraitSample_Slot05.png | 원본 화면 초상화 샘플 5 | 73×81 |
| 38 | UPUI_PortraitSample_Slot06.png | 원본 화면 초상화 샘플 6 | 73×81 |
| 39 | UPUI_PortraitSample_Large.png | 원본 화면 초상화 샘플 7 | 168×166 |

## 제작·검증 범위

기본 제공 이미지 생성 도구로 선택한 화면의 패널/아이콘을 재구성했고, 실제 알파 영역을 기준으로 개별 PNG를 분리했습니다. 생성 프롬프트는 Generation_Prompts.json에 포함했습니다. 그림이 없는 외곽과 초상화 프레임의 창은 투명합니다. 패널과 버튼의 어두운 본체는 그래픽의 일부입니다.

39개 PNG의 크기, 알파, 시트와의 픽셀 일치, 슬라이스 좌표, 9-slice Border 범위, GUID 중복 및 ZIP 무결성을 검사했습니다. Unity 에디터가 없는 환경이므로 Unity 6000.3.5f2에서의 실제 임포트는 실행하지 못했습니다. PNG/.meta 에셋 팩이며, 완성된 UI 프리팹이나 전투 로직은 포함하지 않습니다.

Unity 공식 문서:
- https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-Image.html
- https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html
