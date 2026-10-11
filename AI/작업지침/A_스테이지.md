# 에이전트 1: 스테이지와 진행 저장

먼저 `AI/아키텍처.md`와 `AI/작업지침/00_팀장_분배와_계약.md`를 읽는다. 계약 시그니처는 그 문서가 기준이다.

## 목표

게임 루프를 네 스테이지로 나누고, 클리어 판정과 저장을 만들고, 결과 씬에서 메인으로 돌아가게 한다.

| 순서 | `StageId` | 표시 이름 |
| --- | --- | --- |
| 1 | `EntranceGameJam` | 입학 게임잼 |
| 2 | `GameProgrammingIntro` | 겜프입 프로젝트 |
| 3 | `SecondYear` | 2학년 프로젝트 |
| 4 | `ThirdYear` | 3학년 프로젝트 |

## 소유 파일

- `Assets/Scripts/Interview/Data/**`, `Rules/**`, `Stages/**`(신규), `Unity/**`, `Result/**`
- `Assets/Scenes/ResultScene.unity` (가능하면 건드리지 않는다. 버튼은 코드로 만든다)

`InterviewScreen.cs`, `InterviewExit.cs`는 4번 소유다. 세션 생성 줄을 바꾸는 일은 4번이 한다.

## 할 일

### 1. 순수 도메인 (`Interview/Stages`, UnityEngine 금지)

- `StageId`, `StageDefinition`, `StageCatalog`, `StageAccuracyRule`, `StageProgress`를 계약대로 만든다.
- `StageAccuracyRule.Percent`는 계약의 공식을 그대로 구현한다. `FulfillmentRate`는 건드리지 않는다.
- `StageProgress`는 `StageId` 개수만큼 클리어 여부와 최고 정확도를 든다. `Record`는 정확도가 70 이상이면 클리어로 바꾸고, 이미 클리어면 유지한다.

### 2. 스테이지 데이터 (`PROTOTYPE ONLY`)

- 스테이지 1은 지금 `PrototypeCatalog`의 "미연시" 테마와 여섯 명을 그대로 쓴다. `PrototypeCatalog`는 지우지 않는다. `StageCatalog`가 그것을 참조한다.
- 스테이지 2~4는 새 테마와 지원자를 만든다. 지금 `PrototypeCatalog`와 같은 모양(서류 네 장, 문장 두 줄, 올바른 의심 문장)을 따른다.
  - 지원자 수: 6, 7, 8, 8.
  - 허위 지원자: 1, 1, 2, 2. 부적합(`Inappropriate`): 1명씩.
  - 문장 id는 스테이지 접두어를 붙여 겹치지 않게 한다. 예: `s2-4-portfolio-1`.
  - 요구 능력 합은 스테이지마다 늘린다.
- 각 스테이지마다 정확도 70 이상이 나오는 합격 조합(최대 네 명)이 하나 이상 있어야 하고, 허위 지원자를 넣으면 70 미만이 되는 조합이 있어야 한다. 손으로 계산한 조합을 데이터 파일 상단 주석이 아니라 보고에 적는다.

### 3. 세션과 결과 (`Interview/Rules`)

- `InterviewSession(StageDefinition stage)` 생성자와 `Stage` 속성을 추가한다. 기존 `(GameJamTheme, IReadOnlyList<Applicant>)` 생성자는 `EntranceGameJam`으로 남긴다.
- `InterviewResult`에 `Stage`, `RequiredSkills`, `AccuracyPercent`, `IsCleared`를 넣는다. 계산은 `BuildResult`에서 한다. 결과 타입은 여전히 계산하지 않는다.

### 4. Unity 어댑터 (`Interview/Unity`)

- `StageProgressStore`: `PlayerPrefs` 키 `ohabjizol.stage-progress.v1`에 `JsonUtility` DTO로 저장한다. 키가 없거나 깨졌으면 빈 진행을 돌려준다.
- `StageSelection`: 정적 슬롯. `HasValue`가 거짓이면 `Current`는 `StageProgressStore.Load().NextStage`.
- `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`로 `StageSelection`과 `ResultCarry`를 비운다. 아키텍처 문서의 "어긋난 경계" 8번 후반부다. `ResultCarry`에 비우는 `internal` 메서드가 필요하면 추가한다.

### 5. 결과 씬 (`Interview/Result`)

- `ResultSceneEntry.Start`에서 결과를 `Take`한 직후 한 번만 `StageProgressStore.Load()` → `Record(result.Stage, result.AccuracyPercent)` → `Save`.
- `ResultBoard`의 요구 능력은 `PrototypeCatalog`가 아니라 `result.RequiredSkills`에서 읽는다.
- 결과판에 스테이지 이름, 정확도, "클리어" 또는 "재도전"을 보여 준다. 기존 패널 이름 검색은 그대로 두고, 새 텍스트와 "메인으로" 버튼은 코드로 만든다(`Ohabjizol.UI.UiKit`이 이미 있으면 쓴다. 없으면 `ResultBoard`의 기존 헬퍼를 쓴다).
- "메인으로"는 `SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single)`.

### 6. 테스트 (가능하면)

`Assets/Tests/Editor/StageAccuracyRuleTests.cs`에 EditMode 테스트를 둔다. asmdef가 없으므로 `Assembly-CSharp-Editor`에서 NUnit이 잡히는지 먼저 확인한다. 안 잡히면 테스트는 만들지 않고 보고에 적는다. 최소 케이스:

- 요구 10/5/15, 실제 12/5/12 → 87, 클리어
- 요구 10/5/15, 실제 0/0/0 → 0
- 요구 0/0/0 → 100
- 실제가 요구의 두 배 이상 → 해당 능력 오차 1
- 69.5 경계 반올림 → 70

## 하지 않는 것

- `Managers`의 `GameManager`, `StageManager`, `ScoreSettlement`는 쓰지 않는다. 스테이지 개념은 면접 도메인 안에 둔다. 아키텍처 문서의 "세션 모델이 둘" 문제는 팀장이 문서로 정리한다.
- 메뉴·로딩·면접 화면 UI.

## 끝났다는 기준

- 에디터에서 `InterviewScene`만 열고 플레이해도 `StageSelection.Current`가 유효하다.
- 결과 씬에서 메인으로 돌아갈 수 있고, 플레이를 껐다 켜도 진행이 남는다.
- 보고에 스테이지별 70 이상 조합과 허위 포함 시 70 미만 조합을 적는다.
