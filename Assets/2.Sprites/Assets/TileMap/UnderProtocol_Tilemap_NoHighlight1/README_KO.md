# Under Protocol — 밝기·선명도를 높인 타일맵 스프라이트 v2

기존 16종 타일을 밝은 회청색 금속 바닥으로 다시 그렸습니다. 판의 이음새와 볼트에 밝은 금속 테두리를 넣고, 균열·장애물·노란 경고선의 대비를 높였습니다.
청록색 하이라이트, 선택 테두리, 캐릭터, HUD는 포함하지 않았습니다.

## 기존 타일 교체

기존 프로젝트에서는 압축 안의 `Assets/UnderProtocol_Tilemap_NoHighlight` 폴더를 같은 경로에 덮어씁니다. PNG와 `.meta`를 함께 복사하세요. 파일명, 이미지 크기, PPU, GUID, 스프라이트 ID 및 시트 슬라이스 좌표를 유지해 기존 타일 참조를 그대로 사용할 수 있도록 구성했습니다.

## 구성

- 개별 PNG 16개: 각 256×256px, RGBA 형식, 불투명 바닥.
- 스프라이트 시트 1개: 1024×1024px, 4×4 배열, 256×256 단위 슬라이스 `.meta` 포함.
- 바닥 5종, 직선 외곽 4종, 모서리 4종, 장애물 타일 3종.
- 장애물 3종은 바닥과 함께 그려진 한 칸짜리 타일입니다. 투명한 독립 오브젝트는 아닙니다.
- `Preview/Tilemap_Example.png`: 제공한 타일을 10×10으로 배치한 사용 예시.
- `sprite_manifest.json`: 이름, 크기, 좌표, 분류.

## Unity에 넣기

1. 압축을 풉니다.
2. 압축 안의 `Assets/UnderProtocol_Tilemap_NoHighlight` 폴더와 바로 옆의 같은 이름 `.meta` 파일을 프로젝트의 `Assets`에 넣습니다. PNG 옆의 `.meta`들도 함께 복사합니다.
3. `Sprites`의 개별 PNG를 Tile Palette에 드래그하고 타일 저장 폴더를 선택합니다.
4. Grid의 Cell Size가 `(1, 1, 0)`이고 Transform Scale이 `(1, 1, 1)`일 때, 256 PPU 설정으로 타일 하나가 한 칸을 차지합니다. 기존 프로젝트가 다른 셀 크기를 사용한다면 그 설정에 맞춰 조정합니다.

같은 그림을 두 방식으로 제공했습니다. `Sprites`의 개별 PNG 또는 `Sheets/UPTM_Tileset_4x4.png` 안의 분할 스프라이트 중 한 방식을 골라 사용하면 됩니다.

## 제공한 임포트 설정

| 항목 | 값 |
| --- | --- |
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | 개별 PNG: Single / 시트: Multiple |
| Pixels Per Unit | 256 |
| Pivot | Center (0.5, 0.5) |
| Mesh Type | Full Rect |
| Filter Mode | Bilinear |
| Wrap Mode | Clamp |
| Generate Mip Maps | Off |
| Compression | None |

`.meta`를 누락해서 시트가 분할되지 않았다면, Sprite Mode를 Multiple로 바꾸고 Sprite Editor → Slice → Grid By Cell Size → Pixel Size 256×256, Offset 0×0, Padding 0×0 → Slice → Apply로 적용합니다.

## 타일 목록

| 번호 | PNG 이름 | 용도 |
| --- | --- | --- |
| 01 | UPTM_Floor_Base.png | 기본 금속 바닥 |
| 02 | UPTM_Floor_Worn.png | 마모된 금속 바닥 |
| 03 | UPTM_Floor_Cracked.png | 균열 금속 바닥 |
| 04 | UPTM_Floor_Bolted.png | 볼트 금속 바닥 |
| 05 | UPTM_Edge_Top.png | 위쪽 외곽 |
| 06 | UPTM_Edge_Right.png | 오른쪽 외곽 |
| 07 | UPTM_Edge_Bottom.png | 아래쪽 외곽 |
| 08 | UPTM_Edge_Left.png | 왼쪽 외곽 |
| 09 | UPTM_Corner_TopLeft.png | 왼쪽 위 모서리 |
| 10 | UPTM_Corner_TopRight.png | 오른쪽 위 모서리 |
| 11 | UPTM_Corner_BottomRight.png | 오른쪽 아래 모서리 |
| 12 | UPTM_Corner_BottomLeft.png | 왼쪽 아래 모서리 |
| 13 | UPTM_Obstacle_XCrate.png | X 보강 상자 + 바닥 |
| 14 | UPTM_Obstacle_Hatch.png | 금속 덮개 + 바닥 |
| 15 | UPTM_Obstacle_DoubleBox.png | 이중 패널 상자 + 바닥 |
| 16 | UPTM_Floor_Caution.png | 노란 경고선 바닥 |

## 제작 및 확인 범위

이미지 생성 도구로 첨부된 시트를 다시 그린 뒤, 실제 셀 경계대로 분리하고 각 타일을 256×256으로 정규화했습니다. 생성 후에는 밝기나 선명도 필터를 추가하지 않았습니다. 예시 지도는 최종 제공한 타일을 조합했습니다.

PNG 크기, 시트와 개별 이미지의 픽셀 일치, 슬라이스 좌표, 임포트 설정, GUID 중복, ZIP 무결성을 확인했습니다. 이 환경에는 Unity 에디터가 없어 Unity 6000.3.5f2 안에서의 실제 임포트는 실행하지 못했습니다.

Unity 공식 참고 문서:
- https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/sprite/sprite-editor-window-reference/reference
- https://docs.unity3d.com/ja/2020.1/Manual/Tilemap-CreatingTiles.html
