# 에이전트 C: 메인 화면을 데스크톱으로

먼저 `AI/아키텍처.md`의 "메뉴", "로딩" 절과 `AI/작업지침/00_팀장_분배와_계약.md`를 읽는다. B-1(창 프레임워크)이 끝난 뒤 시작한다.

## 목표

모니터 위 "MAIN MENU / PLAY" 화면을 Windows 같은 GUI 운영체제 바탕화면으로 바꾼다. 전부 임시 UI다.

## 소유 파일

- `Assets/Scripts/Presentation/**` (신규 파일은 `Presentation/Desktop/`)
- `Assets/Scenes/MainMenuScene.unity`, `Assets/Scenes/LoadingScene.unity` (가능하면 건드리지 않는다)
- `Assets/Scripts/Managers/SettingsManager.cs`는 생성해서 쓸 수만 있다. 수정 금지

## 지켜야 할 기존 동작

- 카메라 경로는 그대로다. 실행 → `MainMenuCameraController.AlignToCenter` → `ComputerTransitionController.RememberPose` → `LoadingScene`. `MainMenuController.Play`의 `_accepted` 한 번만 규칙도 유지한다.
- 캔버스는 지금처럼 `ComputerScreen.PlaceWorldCanvas`로 모니터 위에 놓는 월드 스페이스, 1600 × 900.
- 결과 씬에서 `MainMenuScene`으로 돌아오는 경로가 새로 생긴다(A번 작업). 두 번째 진입에서도 카메라와 캔버스가 정상인지 확인한다.

## 할 일

### 1. 바탕화면

- 배경: 단색 또는 그라데이션. 사용자가 준 Windows 바탕화면 스크린샷처럼 왼쪽 위부터 세로로 아이콘이 늘어선다.
- 아이콘 세 개: 청강코드, 설정, 튜토리얼. 정사각 이미지(임시 색 블록 + 머리글자) 아래 한글 라벨.
- 하단 작업표시줄(임시): 왼쪽 시작 버튼 모양, 오른쪽 현재 스테이지 이름과 시계.
- 아이콘은 움직이지 않는다. 드래그 핸들러를 달지 않는다.
- 한 번 클릭은 선택 강조만. 더블클릭이 실행이다.
  - `IPointerClickHandler`의 `eventData.clickCount >= 2`를 먼저 쓴다.
  - 월드 스페이스에서 `clickCount`가 안 올라오면 직접 0.35초 간격으로 잰다. 지금 `MainMenuController`가 `Button.onClick`과 `Update` 마우스 검사를 둘 다 두는 이유가 월드 캔버스 레이캐스트이니, 같은 문제가 나면 그 방식을 따른다.
- 바탕화면 빈 곳을 누르면 선택이 풀린다.
- 한글이 보이므로 텍스트는 전부 `UiKit.Font`.

### 2. 청강코드 (Discord 짝퉁)

더블클릭하면:

1. `ChungkangCodeWindow : InfoWindow`가 뜬다. 사용자가 준 Discord 화면처럼 어두운 창. 왼쪽 서버 열 네 칸이 네 스테이지다. 클리어한 스테이지는 체크, 잠긴 스테이지는 어둡게, `StageProgress.NextStage`가 강조.
2. 약 1초 뒤 "접속 중…"으로 바뀌고 `StageSelection.Set(progress.NextStage)` 후 기존 `Play` 경로로 들어간다(중앙 맞춤 → 포즈 저장 → 로딩 씬).
3. 로딩 씬은 지금의 게이지·완료 문구·줌을 그대로 쓴다. 문구만 청강코드 톤으로 바꾼다. 예: `bootText` "청강코드 실행 중…", `waitText` "서버에 연결하는 중…", `completeText` "연결 완료". 색은 Discord 계열(`#313338`, `#5865F2`). 로딩 텍스트도 한글이므로 폰트를 바꾼다.
4. 줌 후 면접 씬. 여기는 손대지 않는다.

PLAY 버튼과 "MAIN MENU" 제목은 없앤다. `playButton` 직렬화 필드는 다른 씬 참조가 없는지 확인 후 정리한다.

### 3. 설정

`SettingsWindow : InfoWindow`. 내부는 `Presentation/Desktop/SettingsApplier.cs`가 `SettingsManager`를 생성해 액션을 채운다. `Managers`는 엔진을 모르므로 엔진 호출은 전부 어댑터에 있다.

| 항목 | UI | 적용 |
| --- | --- | --- |
| 사운드 | 마스터 볼륨 슬라이더 0~1 | `AudioListener.volume`, 즉시 |
| 해상도 | `◀ 1920 × 1080 ▶` 좌우 버튼 (드롭다운 대신) | 적용 버튼에서 `Screen.SetResolution` |
| 창 모드 | 전체 화면 / 창 토글 | 같은 적용 버튼 |
| 진행 초기화 | 버튼, 한 번 더 눌러 확인 | `StageProgressStore.ResetAll()` |
| 컴퓨터 종료 | 붉은 버튼 | 빌드 `Application.Quit()`, 에디터 `EditorApplication.isPlaying = false` (`#if UNITY_EDITOR`) |

- 해상도 목록은 1280 × 720, 1600 × 900, 1920 × 1080 고정이면 충분하다. `Managers/Resolution.cs`의 타입을 쓴다.
- 볼륨·해상도·창 모드는 `PlayerPrefs`에 저장하고 메인 진입 때 다시 적용한다. 키 접두어 `ohabjizol.settings.`.

### 4. 튜토리얼

아이콘과 `TutorialWindow : InfoWindow`만. 정보 영역에 "튜토리얼 시작" 버튼 하나. 누르면 "준비 중" 문구만 바뀐다.

## 위치 잡기

데스크톱 아이콘·창 기본 위치는 직렬화 필드로 뺀다. 같은 요소를 세 번 넘게 고치게 되면 멈추고, 어떤 필드를 어떤 값 근처로 맞추면 되는지 적어 사용자에게 인스펙터 조정을 요청한다.

## 하지 않는 것

- 면접 씬, 결과 씬.
- 아이콘 드래그, 우클릭 메뉴, 실제 튜토리얼 내용.

## 끝났다는 기준

- 통합 체크리스트의 데스크톱·청강코드·설정·튜토리얼 항목이 통과한다.
- 월드 스페이스 캔버스에서 B번의 창 드래그·닫기가 된다. 안 되면 원인을 B번에게 보고한다(직접 고치지 않는다).
- 결과 씬 → 메인 복귀 후 두 번째 실행도 된다.
