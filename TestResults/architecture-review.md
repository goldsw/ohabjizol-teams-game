---
cursor:
  subagentId: "bc-8d97949e-2e52-5e8d-853c-eea32f4e2cc1"
---

# 코드 평가

대상은 이 체크아웃의 `Assets/Scripts/Managers`와 `Assets/Scripts/Presentation`이다. 이 클라우드 체크아웃에 없는 로컬 미커밋 변경은 없다. 구역 그림은 `docs/architecture-zones.md`의 평가 기준일 뿐이고, 여기서 일을 나누거나 고치지 않는다.

## 잠재 위험

### 권한 누수

`LoadingSceneController.HandleInterviewLoaded`는 `sceneLoaded`에서 다음 씬의 `ComputerScreen`을 찾아 `ViewRoot`를 `Destroy`한다. 면접 씬의 수명은 로딩 컨트롤러의 정적 구독이 쥐고 있다. `viewRoot`가 비면 `ComputerScreen.ViewRoot`는 `transform.root`까지 올라간다.

면접 씬에는 스크린이 없어 지금은 파괴가 실행되지 않는다. 스크린이 생기는 순간 카메라·배경·조명이 든 세트가 지워진다. 면접에는 루트 `Main Camera`, 키 라이트, 바닥, `InterviewMarker`가 이미 있다.

### 콜백 소유

`MainMenuController.onPlay`는 공개 `Action` 하나다. 메뉴는 호출만 하고, 등록은 `ComputerTransitionController.Awake`가 한다. 조건은 같은 게임오브젝트에 `MainMenuController`가 있고 `onPlay`가 비어 있는 것이다. 메뉴 씬의 `MainMenu` 오브젝트는 그 조건에 맞다. 로딩 씬의 전환 컴포넌트에는 메뉴 컨트롤러가 없어 그 대입은 일어나지 않는다.

`Play`는 `_accepted`를 올린 뒤 `onPlay`를 호출하고, 이어서 `RememberPose`와 `LoadingScene` 로드를 항상 한다. 칸이 비거나 `CenterAndLock`이 스크린 없이 바로 반환해도 씬은 열린다. 다른 코드가 `onPlay`에 다른 델리게이트를 넣으면 `CenterAndLock`은 남지 않는다. 칸이 하나라 이전 등록은 교체된다.

`SettingsManager`의 `ChangeVolume`, `ChangeResolution`, `ChangeWindowMode`와 `SoundManager`의 `SpawnVfx`, `StopVfx`도 같은 모양의 단일 `Action`이다. 저장소 안에는 이 칸을 채우는 대입이 없다.

### 숨은 결합

중앙 정렬과 줌이 정적 필드 하나를 공유한다. `Play`가 `RememberPose`로 맞춘 뒤의 포즈를 남긴다. `Finish`는 줌이 끝난 카메라로 그 필드를 다시 쓴 다음 `ActivateInterview`를 호출한다. 면접은 `ApplyFixedPose`를 호출하지 않아 줌 결과는 면접 카메라로 이어지지 않는다. 같은 칸의 주인은 둘이다.

`CenterAndLock`은 `Camera.main`을 옮긴다. `Play`는 `cameraController`가 가리키는 카메라를 저장한다. 지금 메뉴 씬에서는 그 카메라가 메인 카메라다. 참조가 갈라지면 맞춘 대상과 로딩이 복원하는 대상이 달라진다.

`FitCanvas`는 플레이 중 `preWillRenderCanvases`마다 스크린 자식 루트 캔버스의 `sizeDelta`를 `worldCanvasSize`로 덮고, `CanvasScaler`가 있으면 파괴한다. 로딩 씬의 저장 `sizeDelta`는 `5000 x 3000`이고 `worldCanvasSize`는 `1700 x 1000`이다. 메뉴는 `1600 x 900`으로 저장값과 필드가 같다. 맞추기 스크립트는 두 씬에 같은 경로로 붙어 있다.

`ComputerScreen.Awake`는 렌더러 `sharedMaterial`을 런타임 머티리얼로 바꾼다. 씬이 가리키는 머티리얼은 URP 기본 머티리얼이라 `shader.name`에 `Unlit`이 없고, 선택은 `Shader.Find`로 떨어진다.

### 매니저와 연출

연출 스크립트는 `Ohabjizol.Managers`를 참조하지 않는다. 매니저 파일은 `UnityEngine`을 쓰지 않는다. 볼륨 게이지는 `SoundManager`, 창 적용 기록은 `WindowManager`, 시각 스폰은 `VfxManager`에 있다. 설정은 선택을 저장하고 액션만 올린다. 지금 연출이 매니저 일을 하거나, 매니저가 씬을 여는 코드는 없다.

`EndStage`의 `_ending`은 종료 콜백 안의 또 다른 `EndStage`를 예외 없이 무시한다. `StageEnded` 안에서 `StartStage`는 성공한다. 바깥 `_ending`이 아직 켜져 있으면 새 스테이지의 종료는 실행되지 않는다. `StartGame`과 `StartStage`의 같은 상태 재진입은 예외로 막혀 있다.

`Resolution` 생성자는 0 이하를 거절한다. `default(Resolution)`은 `0 x 0`이고 생성자를 타지 않는다. `SetResolution`과 `ApplyResolution`은 그 값을 저장한다.

## 구조화되지 않은 곳

- 중앙 정렬 흐름이 세 타입에 나뉘어 있다. `MainMenuController`가 호출하고, `ComputerTransitionController.CenterAndLock`이 거리 유지로 스크린을 보게 하고, `MainMenuCameraController.LockLook`은 고정 플래그만 켠다. `LockLook`은 요·피치·오프셋을 지우지 않는다. 고정이 풀리면 `LateUpdate`가 맞춤 전의 드리프트를 다시 쓴다.
- 줌의 시작은 `LoadingSceneController`, 수식은 `ComputerTransitionController.ComputeZoomPose`, 끝난 뒤의 씬 활성은 `Finish`가 받은 `Action`이다. 중앙 정렬과 줌은 수식은 나뉘어 있으나 한 타입과 한 정적 포즈에 같이 있다.
- 매니저 액션을 연결하는 스크립트가 없다. `Assets/Scripts` 안에서 `Managers`와 `Presentation` 밖에 플레이 흐름을 조립하는 타입도 없다. `TutorialInfo`의 `Readme`는 이 흐름 밖이다.
- 면접 씬에는 프레젠테이션 스크립트가 없다. 면접으로 넘어간 뒤의 정리는 로딩 컨트롤러의 정적 훅이 담당한다.
- asmdef가 없다. 매니저에 `UnityEngine`이 들어가도 컴파일 경계는 막지 못한다.
- PLAY 진입은 `Button.onClick`과 `Update`의 사각형 검사가 둘이다. `_accepted`는 `onPlay`보다 먼저 올라가서, 한 번의 클릭이 콜백과 로드를 두 번 열지는 않는다. 진입 자체는 여전히 두 곳이다.

로딩 시작의 `ApplyFixedCameraPose`는 저장된 포즈를 복원한다. 두 번째 중앙 정렬은 아니다. `Begin`은 `CenterAndLock`을 호출하지 않는다.
