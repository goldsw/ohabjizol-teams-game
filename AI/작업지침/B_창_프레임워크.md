# 에이전트 B: 창 프레임워크와 문서 창

먼저 `AI/아키텍처.md`와 `AI/작업지침/00_팀장_분배와_계약.md`를 읽는다.

일은 두 단계다. B-1을 끝내면 바로 팀장에게 보고한다. C번과 D번이 그것을 기다린다.

## 소유 파일

- `Assets/Scripts/UI/**` (신규, 네임스페이스 `Ohabjizol.UI`)
- `Assets/Scripts/Interview/UI/Windows/**` (신규, 네임스페이스 `Ohabjizol.Interview`)

## B-1. 공용 창 프레임워크

`Ohabjizol.UI`는 게임 네임스페이스(`Interview`, `Presentation`, `Managers`)를 참조하지 않는다.

### `UiKit`

`InterviewScreen`, `MainMenuController`, `ResultBoard`, `LoadingSceneController`에 흩어진 `CreateText`, `CreateImage`, `CreateButton`, `Place`, `Stretch`, `WhiteSprite`, `UiFont`, `EnsureEventSystem`을 한 곳으로 모은다. 기존 파일은 고치지 않는다. 그 정리는 각 소유자가 한다.

- 폰트: `Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim" }, 40)`, 실패하면 `LegacyRuntime.ttf`.
- 버튼 네비게이션은 `None`. `InterviewScreen.Paint`의 색 블록 규칙을 따른다.

### 창 구조

요구 그대로다. 창은 툴바 → UI → 정보로 이루어진다.

```
Window (Root, WindowToolbar 또는 파생 컴포넌트)
├─ Toolbar   상단 바. 제목. 마우스로 집어 창 전체를 옮긴다
│  └─ Controls   툴바 오른쪽 UI. 닫기 버튼(X). 누르면 창이 닫힌다
└─ Info      툴바 아래 메인 정보 영역. InfoWindow 파생이 마음대로 채운다
```

- `WindowToolbar`가 툴바·UI·드래그·열고 닫기를 맡는다. `InfoWindow : WindowToolbar`가 정보 영역을 `protected abstract void BuildInfo(RectTransform info)`와 `public virtual void Refresh()`로 열어 둔다. 어떤 구현이든 이 두 함수만 오버라이드하면 된다.
- `WindowFactory.Create<T>`는 `GameObject` + `AddComponent<T>` + `Build`. `Build`는 한 번만 실행된다. `OnBuilt`에서 `InfoWindow`가 `BuildInfo(Info)`를 부른다.
- `Close`는 `SetActive(false)`. 파괴하지 않는다. 다시 열면 마지막 위치에 뜬다. `Open`은 `BringToFront` 후 `OnOpened`, `InfoWindow`는 `OnOpened`에서 `Refresh`.
- 창 아무 곳이나 누르면 `BringToFront`(`SetAsLastSibling`).
- 정보 영역 배경은 `raycastTarget = true`. 창 뒤의 UI가 눌리지 않게 한다.

### 드래그

- 툴바에 `IBeginDragHandler`, `IDragHandler`를 단 내부 컴포넌트를 붙인다.
- 좌표는 `RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out local)`. 시작 시 오프셋을 저장한다.
- 이 방식이어야 두 캔버스에서 같이 동작한다.
  - 면접 씬: 스크린 스페이스 오버레이, 1920 × 1080 `CanvasScaler`. 카메라 `null`.
  - 메인 씬: 모니터 위 월드 스페이스 캔버스, 1600 × 900. 카메라 있음. `MainMenuCameraController`가 마우스로 카메라를 흔든다.
- 툴바가 부모 밖으로 완전히 나가지 않게 자른다(툴바 높이만큼은 항상 보이게).

### 모양 (임시)

- 툴바 높이 40, 어두운 회색. 제목 왼쪽 정렬. 닫기 버튼 40 × 40, 붉은 호버.
- 정보 영역은 밝은 회색(`#C8D0D8` 근처). 사용자가 준 지원서 시안이 이 색이다.

### 데모

`Assets/Scripts/UI/WindowFrameworkDemo.cs`: 빈 씬에 붙이면 오버레이 캔버스와 창 두 개를 만든다. 사용자가 드래그·닫기를 눈으로 확인하는 용도다. 씬 파일은 만들지 않는다.

## B-2. 문서 창 (`Interview/UI/Windows`)

### `DocumentKindText`

`InterviewScreen`의 `DocumentLabel`, `DocumentColor`를 옮겨 온다. 색 값은 그대로.

### `DocumentWindow : InfoWindow`

`Bind(session, kind, changed)` 후 `Refresh`가 `session.Current`에서 그 종류의 서류를 찾아 그린다.

- 제목: `DocumentKindText.Korean(kind)`. 정보 영역 상단에 서류 색 띠.
- 지원서 창만 머리에 이름, 주 직군, 표시 능력을 보여 준다. 사용자의 지원서 시안(제목 "지원서", 그 아래 "이름" 칸)이 이 배치다.
- 문장 두 줄은 버튼이다. 누르면 `session.SelectLine(line.Id)` 후 `changed?.Invoke()`. 고른 문장은 `InterviewScreen`과 같은 강조색과 "의심  " 접두.
- `session.IsFinished`거나 `Current`가 없으면 버튼을 끈다. 그 종류의 서류가 없으면 "제출되지 않음".
- 지원자가 바뀌어도 창은 열린 채로 새 지원자를 그린다. 그 호출은 D번이 `Refresh`로 한다.
- 기본 크기 560 × 700 (1920 × 1080 기준). 위치는 D번이 정한다.

## 하지 않는 것

- `InterviewScreen.cs`를 고치지 않는다. 연결은 D번이 한다.
- 메뉴의 설정·청강코드·튜토리얼 창 내용은 C번이 `InfoWindow`를 파생해 만든다.

## 끝났다는 기준

- B-1: 데모로 오버레이에서 드래그·닫기·앞으로 가져오기가 된다. 월드 스페이스는 C번이 확인한다.
- B-2: D번이 `Bind`와 `Refresh`만으로 네 창을 띄울 수 있다.
