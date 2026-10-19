# 에이전트 D: 면접 화면 개편

먼저 `AI/아키텍처.md`의 "면접 화면" 절과 `AI/작업지침/00_팀장_분배와_계약.md`를 읽는다. B-1(창 프레임워크)이 끝난 뒤 시작한다. B-2(문서 창)와 A번(스테이지)은 같이 진행돼도 된다.

## 목표

면접 씬을 Discord 영상통화 화면 위에서 면접하는 모습으로 바꾼다. 세션 규칙은 그대로다. 화면이 부르는 세션 메서드는 여전히 `SelectLine`, `Accept`, `Reject`뿐이다.

## 소유 파일

- `Assets/Scripts/Interview/UI/InterviewScreen.cs`, `InterviewExit.cs`, 신규 `Assets/Scripts/Interview/UI/Layout/**`
- `Assets/Resources/Interview/**` (신규)
- `Assets/Scenes/InterviewScene.unity` (가능하면 건드리지 않는다)

`Interview/UI/Windows/**`는 B번 소유다.

## 에셋

| 원본 | 넣을 곳 | 임포트 |
| --- | --- | --- |
| `C:\Users\eeseo\Downloads\Discord_total.png` (1920 × 1080) | `Assets/Resources/Interview/Discord_total.png` | Sprite (Single), Filter Point, Compression None, Max Size 2048, Mipmap 끔 |
| `C:\Users\eeseo\Downloads\girl.png` (636 × 396) | `Assets/Resources/Interview/Portraits/girl.png` | Texture Default 또는 Sprite, Filter Point, Compression None, Mipmap 끔 |

- 파일 복사 후 Unity가 `.meta`를 만들면 그 `.meta`의 필드를 고친다. 에디터가 재임포트하도록 사용자에게 포커스를 요청한다.
- `girl.png`는 가로 두 칸이다. 왼쪽 0~318이 idle(입 다문 얼굴), 오른쪽 318~636이 talk(입 벌린 얼굴), 높이 396 전체. 슬라이스는 `.meta`를 편집하지 말고 런타임에 `Sprite.Create(texture, new Rect(0,0,318,396), ...)`, `new Rect(318,0,318,396)`으로 자른다.
- 실제 캐릭터 시트가 오면 같은 규칙(가로 2칸, idle → talk)을 따른다고 가정한다. 다르면 사용자에게 묻는다.

## 화면 배치

모든 영역은 1920 × 1080 기준 정규화 앵커다. 아래 값은 `Discord_total.png`를 재서 잡은 초기값이다. 위아래는 Unity 앵커 기준(아래가 0).

| 영역 | minX | minY | maxX | maxY | 내용 |
| --- | --- | --- | --- | --- | --- |
| 배경 | 0 | 0 | 1 | 1 | `Discord_total.png` 전체 |
| 상단 제목 | 0.33 | 0.90 | 0.80 | 0.97 | "입학 게임잼 · 주제 미연시 · 핵심 그래픽" |
| 큰 영상 | 0.332 | 0.344 | 0.943 | 0.839 | 현재 지원자 초상 + 이름·직군 |
| 작은 타일 1 | 0.438 | 0.165 | 0.567 | 0.318 | 합격자 1 |
| 작은 타일 2 | 0.578 | 0.165 | 0.706 | 0.318 | 합격자 2 |
| 작은 타일 3 | 0.718 | 0.165 | 0.846 | 0.318 | 합격자 3 |
| 왼쪽 패널 | 0.073 | 0.315 | 0.254 | 0.87 | 빈 자리 4칸, 현재 스탯, 요구 스탯 |
| 의심 표시 | 0.29 | 0.07 | 0.62 | 0.13 | "의심  없음" / "의심  문장" |
| 합격 | 0.66 | 0.065 | 0.81 | 0.14 | 우하단 |
| 불합격 | 0.825 | 0.065 | 0.985 | 0.14 | 우하단 |
| 작업표시줄 | 0 | 0 | 1 | 0.052 | 화면 맨 아래, 맨 위 레이어 |

- 합격 네 명이 차면 세션이 끝나므로 진행 중에 보이는 합격자는 최대 세 명이다. 작은 타일 세 칸이면 된다.
- 큰 영상 안 초상은 비율을 지킨다(`preserveAspect`).
- 배경 그림 안의 Discord 하단 컨트롤 바(마이크, 카메라, 통화 종료)와 작업표시줄·합불 버튼이 겹친다. 겹침이 거슬리면 사용자에게 묻는다. 그림을 고치지 않는다.

### `InterviewLayout`

위 표를 `[Serializable] class InterviewLayout`의 `Rect` 필드(정규화, x=minX, y=minY, width, height)로 만들고 `InterviewScreen`에 `[SerializeField]`로 단다. 플레이 중 `OnValidate`에서 다시 `Place`한다. 그러면 사용자가 플레이 모드에서 인스펙터로 맞추고 Copy Component로 값을 넘겨줄 수 있다.

같은 요소의 위치를 세 번 넘게 고치게 되면 멈추고 사용자에게 이 방식으로 조정을 요청한다. 토큰을 아끼는 것이 목적이다.

## 할 일

### 1. 세션 생성

`STAGE-HOOK`. A번 작업이 있으면 `new InterviewSession(StageCatalog.Get(StageSelection.Current))`. 없으면 기존 `PrototypeCatalog` 생성자를 쓰고 주석을 남긴다.

### 2. 레이어 순서

배경 → 영상(큰 화면, 타일) → 왼쪽 패널 → 의심 표시 → 합격·불합격 → 창 레이어 → 작업표시줄 → "면접 종료" 라벨.

창 레이어는 전체 화면을 덮는 빈 `RectTransform`(레이캐스트 없음)이고, 문서 창의 부모다. 창은 작업표시줄 아래로 끌려 들어가도 작업표시줄이 위에 보인다.

### 3. 작업표시줄

- 어두운 반투명 바. 왼쪽 시작 버튼 모양("청강OS"), 이어서 지원서, 포트폴리오, 일정표, 면접 기록 버튼. 색은 `DocumentKindText.Color`.
- 버튼을 누르면 해당 `DocumentWindow`를 `Toggle`. 열려 있는 창의 버튼은 밑줄이나 밝은 색으로 표시한다. 창의 `Opened`, `Closed` 이벤트로 갱신한다.
- 오른쪽에 스테이지 이름.

### 4. 문서 창

- 네 개를 `WindowFactory.Create<DocumentWindow>`로 한 번 만들고 `Bind(_session, kind, Refresh)`.
- 시작할 때 지원서 창만 연다. 기본 위치는 큰 영상 위로 계단식(각 40px 어긋나게).
- 기존 가운데 문서 패널(서류 버튼 네 개, 문장 버튼 두 개)은 없앤다. 그 정보는 이제 창 안에 있다.
- `Accept`, `Reject`, 문장 선택 뒤 `Refresh`가 열린 창 전부의 `Refresh`도 부른다.

### 5. 왼쪽 패널

- "팀" 제목, 자리 1~4: 지금 `WriteSlots`처럼 이름, 직군, 표시 능력. 비면 "빈 자리".
- "현재": 팀 표시 능력 합(`Team.DisplayedSum`). 요구 이상인 값은 초록, 미만은 노랑.
- "요구": `Theme.RequiredSkills`.
- 실제 능력이나 정확도는 보여 주지 않는다. 허위를 걸러내는 것이 게임이다.

### 6. 영상통화 참가자

`Layout/ParticipantPortrait.cs`: `Image` 하나에 `Sprite[] frames`를 1초마다 순환. `Time.unscaledDeltaTime`. 기본 프레임은 idle, talk 두 장.

- 큰 영상: `session.Current`. 지원자가 바뀌면 프레임 위상을 0으로 되돌린다. 세션이 끝나면 숨긴다.
- 작은 타일: `session.Team.Members` 앞 세 명. 비면 Discord 기본 아바타 위치 그대로(그림의 기본 아이콘이 보이게) 비워 둔다.
- 이름표: 초상 아래 반투명 바에 "이름 · 직군".
- 지금은 모든 사람이 `girl.png`다. 지원자별 시트는 나중 일이다. 지원자 → 시트 매핑 지점을 한 메서드로 둔다.

### 7. 정리

- `InterviewScreen`의 헬퍼(`CreateText` 등)를 `UiKit`으로 바꾼다. 문서 라벨·색은 `DocumentKindText`로.
- `ResultSceneName`, `HandoffSceneName`, `Result`, `InterviewExit`의 동작은 유지한다.

## 하지 않는 것

- 세션 규칙, 판정, 결과 계산.
- 창 프레임워크 자체. 버그는 B번에게 보고한다.

## 끝났다는 기준

- 통합 체크리스트의 면접·영상통화 항목이 통과한다.
- 여섯 명을 다 처리하거나 네 명 합격하면 지금처럼 결과 씬으로 넘어간다.
- 사용자에게 위치 조정을 요청했다면 받은 값이 `InterviewLayout` 기본값에 반영돼 있다.
