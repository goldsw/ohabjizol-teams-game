# 씬별 선정 BGM

- 선정 음원 5개만 사용함. 원본 경로는 아래와 같음.
- 메뉴: `C:\Users\eeseo\Downloads\사운드\메인 메뉴 BGM\메인 메뉴-1_선정.mp3`.
- 면접: `C:\Users\eeseo\Downloads\사운드\튜토리얼 + 인게임 BGM\튜토 리얼-2_선정.mp3`.
- 크레딧: `C:\Users\eeseo\Downloads\사운드\크래딧\크래딧 BGM-2_선정.mp3`.
- 좋은 엔딩: `C:\Users\eeseo\Downloads\사운드\Good 엔딩\Good-1_선정.mp3`.
- 나쁜 엔딩: `C:\Users\eeseo\Downloads\사운드\Bad 엔딩\Bad-1_선정.mp3`.
- 메뉴 음원 1개 → `MainMenuScene`.
- 로비 BGM은 이번 실행에서 튜토리얼 완료 또는 건너뛰기 처리 이후에만 재생함. 과거 완료 기록은 음악 시작 조건에 사용하지 않음.
- 앱 실행·Editor Play 시작 시 음악 허용 상태 초기화함. 완료 후 로비 복귀·로비에서 건너뛰기 처리 시 시작함.
- 면접 음원 1개 → `TutorialScene`, `InterviewScene` 공유함.
- 크레딧 음원 1개 → `CreditsScene`.
- 좋은 엔딩 음원 1개 → `GoodEndingScene`.
- 나쁜 엔딩 음원 1개 → `BadEndingScene`.
- Normal / Result / Production: 선정 BGM 없음. 매핑 없는 씬은 페이드 정지함.
- `LoadingScene`: 현재 음악 유지함.
- 기존 `EndingScene`: `Ohabjizol.Interview.EndingBoard` 표시 패널 기준 좋은/나쁜 엔딩 매핑 재사용함. 일반 엔딩 또는 판별 불가 시 정지함.

## 연결

- 플레이어: `Assets/Scripts/Presentation/SceneBgmPlayer.cs`.
- 공용 프리팹: `Assets/Resources/Audio/SceneBgmPlayer.prefab`.
- Resources 경로: `Audio/SceneBgmPlayer`. 첫 씬 로드 후 자동 생성, 씬 전환 시 유지함.
- 선정 클립 5개와 위 씬 배열을 프리팹에 지정함.
- 생성자: `Entry(string[] sceneNames, AudioClip clip, bool loop = true)`. Good/Bad는 마지막 인자 false 지정함.
- 기본 볼륨 0.6 / 페이드 0.35초 / 2D 재생함. Entry별 `loop` 기본값 true임.
- 메인 89.76초 / 튜토 89.88초 / 크레딧 126.05초: `loop=true`, Streaming 설정함.
- Good 6.36초 / Bad 9.34초: 짧은 엔딩음으로 `loop=false`, DecompressOnLoad 설정함.
- 같은 클립 공유 씬 간 재시작 없음. 전환 취소 시 현재 볼륨부터 최신 대상 적용함.
- 실제 시간 기준 페이드로 일시정지 중에도 전환함. 첫 프레임 이전 로드 지연 제외함.
- 프리팹 자체 `AudioListener`는 비활성 저장함. 씬 로드 시 다른 활성 리스너 없을 때만 활성화함.

## 설정 및 검증

- `Assets/Scripts/Managers/SoundManager.cs`: 기존 상태 관리 유지함.
- `Assets/Scripts/Presentation/Desktop/SettingsApplier.cs`: 기존 `AudioListener.volume` 설정 재사용함.
- 플레이어는 전역 리스너 볼륨과 `ignoreListenerVolume` 변경하지 않음.
- `Assets/Audio/Selected`에 선정 음원 5개만 복사함. 미선정 7개 제외함.
- 원본·복사본 SHA256 일치 확인함. 원본 파일 유지함.
- 컴파일 오류·경고 0건, Resources 프리팹 로딩·5개 클립 참조 확인함.
- 사운드 작업 중 Play 종료함. Play 재생·청음·테스트·커밋 미수행함.
- 상세 로그: `Logs/SceneAudioImport/import-manifest.json`, `compile.json`, `authored-audio.txt`, `verified-assets.txt`.
