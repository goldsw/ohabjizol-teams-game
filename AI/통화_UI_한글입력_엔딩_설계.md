# 통화 UI·한글 입력·엔딩 최소 설계

## 확정·범위
- 수정안·구현 계약과 적용 결과를 함께 관리함. 이 문서를 기준으로 UI 및 IME/Ending 두 영역을 구현함.
- 공통 지침 `C:\Users\eeseo\.codex\AGENTS.md` 직접 읽음.
- 근거: [Luna 노트](../Logs/CallImeEnding/search-notes.md), [행 실측](../Logs/CallImeEnding/layout-before.json), [부모 실측](../Logs/CallImeEnding/slot-parent-before.json).
- 슬롯 Text 6개 모두 prefab 원본·TeamSummary 직속·rect396×760·pos(32,0) 동일함. 부모 보고 group 내부 Text 0개임.
- group 크기404.8×76, Y 간격15~20임. Text 전체 stretch와 행 간격 부족으로 겹침 확정함.
- `InterviewTeamSlotView.cs:75~80` reparent 후 stretch 경로와 부합함. prefab reparent 실패 당시 Undo 예외 자체는 미확인임.
- 실측 font는 NotoSansKR임. 한글 폰트 결함으로 확정할 근거 없음.

## UI/layout 계약
- 통화 채널 아래 VerticalLayoutGroup 목록·현재 지원자 row 1개 추가함.
- row는 HorizontalLayoutGroup, 프로필 고정 LayoutElement·이름/파트 caption 가변 폭 사용함.
- `InterviewTeamSlotView.BindCharacterPortrait(string characterId, ApplicantVisualCatalog catalog)` 추가함.
- 기존 `BindPortrait(SelectedApplicantDisplay, catalog)`는 새 API로 위임함. 동명 overload의 null 모호성 방지함.
- 기존 profile sprite·stencil Mask·showMaskGraphic=false·maskable=true·Idle crop 재사용함.
- `InterviewCallView.Bind(Applicant)`에서 별도 참가자 Image에 Idle·이름·파트 연결함. null이면 row 숨김/초기화함.
- 중앙 SetPose와 참가자 Idle 분리함. presenter 애니·반응시간(기본3초)·질문·download·팀 실제 능력치 유지함.
- 팀 group마다 새 authored caption 생성 → 기존 Text 설정 복사 → `_slotLabels` 6개 재연결함.
- 원본 Text는 Undo 기록 후 비활성화함. 삭제·원본 reparent·prefab 전체 unpack 금지함.
- 기존 group 조기 반환/Preflight 전제 수정함. 복구·재실행 시 caption 중복 생성과 잘못된 부모 참조 방지함.
- TeamSummary VerticalLayoutGroup·group LayoutElement 높이76 기준 적용함. 내부 프로필/문구는 Horizontal layout 사용함.
- Stage/Current/Required 별도 정보 유지함. 같은 layout 부모의 비행 항목은 ignoreLayout 적용함.
- padding/spacing/최소크기만 기본 제공함. Inspector 조절 허용·행별 좌표 미세보정 금지함.
- 같은 rect에 layout/fitter 중복 크기 지배 피함.
- 중앙 portrait 실측794.88×431.46·pivot(0.5,0.5)임. top pivot(0.5,1)·위 경계 유지·최종 크기 기존2/3 적용함.
- authored 기준 사용으로 반복 적용 시 축소 누적 방지함.

## IME 원인·안전 경계
- 설치 소스: `Library/PackageCache/com.unity.ugui@23e8d17bfaf9/Runtime/UGUI/UI/Core/InputField.cs`.
- 실제 stack: OnDeselect3282 → Append(string)2431 → Append(char)2468 → Substring임.
- OnDeselect3280~3282는 조합문자 유지 상태로 Append함. caret getter1032~1033은 raw 위치에 조합 길이를 더함.
- Append2456~2468은 조합 길이가 포함된 caret으로 원문 Substring하여 선택 끝 초과 가능함. 직접 오류 원인 확정함.
- protected 확인: m_Text282, m_CaretPosition298, m_CaretSelectPosition299임.
- private 확인: input325, compositionString335임. subclass 직접 접근 불가함.
- override 가능 확인: Append(string)2417, Append(char)2443, OnDeselect3278임.
- 최소안: 메모장 전용 `InterviewImeInputField : InputField`; OnDeselect 진입 표시 → 해당 구간의 virtual Append(string)을 raw 선택 인덱스 기반 확정으로 처리함.
- raw 양끝을 원문 길이에 clamp하여 선택만 교체함. 조합 1회 반영·커서 이동·변경 알림 보장함.
- 이 구간에서 native Append(char) 재진입 방지함. base.OnDeselect의 비활성/종료 이벤트 유지함.
- readOnly·줄바꿈·characterLimit·검증 설정 보존함. worker가 실제 메모장 설정과 필요한 추가 API만 확인함.
- 다른 입력 경로의 동일 취약성이 확인되면 같은 raw 처리 확장함. 전체 IME 경로 검증 완료로 간주하지 않음.
- PackageCache 수정·reflection·예외 삼키기·무조건 선택 해제·TMP 전체 이관 회피함.
- 메모장 OnInputChanged → model.Changed → UpdateInput 동기 echo 존재함.
- SetText480~481은 동일 문자열이면 반환함. echo를 현재 예외의 직접 원인으로 단정하지 않음.
- `InterviewNotepadWindow`에 로컬 발신 guard·동일값 guard 추가함. 외부 모델 변경은 조합 중 대기 후 종료 시 적용함.
- 세션 초기화/clear 최종값 유지함. StageNotepad API와 serialized InputField 타입 계약 유지함.

## Ending 최소 연결
- 완료 결과 이동은 `ResultBoard.cs:89` → EndingSceneRoute.For(run.Ending)임.
- GoodEndingScene→Good_Ending.png, NormalEndingScene→Normal_Ending.png, BadEndingScene→Re_Bad_Ending.png 연결함.
- PNG 위치 `Assets/Art/Ending/UI/텍스트 X/`임. 원본은 Ending/Source/Aseprite/텍스트 X/임.
- EndingBoard 기본 bake78~83은 단색 Image+Story임. 실제 씬 sprite 연결 여부는 현재 자료만으로 미확정임.
- `EndingBoard.ConfigureArtworkEditor(Sprite good, Sprite normal, Sprite bad)` 추가하여 기존 panel Image에 sprite·비율 유지 적용함.
- ResultPresentationBake에서 기존/신규 씬 모두 재연결함. BakeAuthoredUi early return만 의존하지 않음.
- 누적 run.Ending·완주·별·새 게임·fixed kind 유지함. 개별 stage ResultScene 분기와 혼용하지 않음.

## low worker 2개 경계
- 공통: gpt-6.1-sol·reasoning_effort=low·제한 문맥·공통 AGENTS 직접 읽음. 아래는 부모가 실행할 분할 계약임.
- UI/layout 소유: InterviewCallView.cs, InterviewTeamSlotView.cs, InterviewScreen.TeamWindow.cs, 전용 layout bake 파일임.
- IME+Ending 소유: InterviewNotepadWindow.cs, 새 InterviewImeInputField.cs, EndingBoard.cs, Assets/Editor/InterviewRefresh/ResultPresentationBake.cs임.
- StageNotepad는 외부 API 유지하며 변경 필요 시 IME worker만 소유함. presenter 변경 불필요함.
- 부모 소유: 씬/직렬화 통합·_slotLabels 연결·InputField 교체·아트 저장·백업임. worker 동시 씬 저장 금지함.
- 공용 editor 진입점 변경은 부모 통합함. MainMenu 수정 없이 추가 InterviewScene 닫아 초기 상태 복구함.
- 확인 조건: 6행 분리·Idle 유지·중앙2/3·한글 교체/포커스 해제/외부clear·결과 아트임. 아래 결과는 컴파일·정적 연결 범위임.

## 실행 파일·완료 계약
- UI worker 추가 허용: `UI/InterviewScreen.CallChannelAuthoring.cs`, `Assets/Editor/InterviewRefresh/CallUiLayoutBake.cs`임.
- UI 진입점: `CallUiLayoutBake.ApplyToCurrentScene()`, 메뉴 `Ohabjizol/Interview/Apply Call UI Layout`임. 슬롯 캡션 재연결·비활성 원본·행 레이아웃·통화 참가자·중앙 2/3를 적용하고 씬은 저장하지 않음.
- IME/Ending worker 추가 허용: `UI/InterviewScreen.NotepadAuthoring.cs`, `Assets/Editor/InterviewRefresh/InterviewImeEndingBake.cs`임.
- 기존 BakeNotepadWindowEditor·RecalculateFixedAppsWidthEditor는 삭제/교체 금지함. 입력 교체는 새 `UI/InterviewScreen.NotepadImeAuthoring.cs` partial로 추가하고 기존 Bake 끝에서 호출함.
- IME/Ending 진입점: `InterviewImeEndingBake.Apply()`, 메뉴 `Ohabjizol/Interview/Apply IME and Ending Artwork`임. InputField 교체는 Editor Undo로 기존 참조·설정·내용을 보존해 수행함.
- 엔딩 아트는 PNG 임포터를 변경하지 않고 `Assets/Art/Ending/Source/Aseprite/텍스트 X/`의 각 native Sprite를 사용함. 기존 PNG가 유효한 Sprite이면 그것도 가능함.
- 엔딩 3개는 추가 열기→대상 임시 활성→보드 연결→저장→자신이 연 씬만 닫기 순서임. 시작 활성 씬을 복원하고 사용자 배치·별 기준·라우팅을 보존함.
- 보고 파일: `Logs/CallImeEnding/ui-apply.json`, `ime-ending-apply.json`임. success/status·처리 개수·오류를 기록함.
- 부모는 2개 메뉴 실행 후 컴파일·정적 참조를 확인하고 InterviewScene만 저장함. 추가로 연 InterviewScene을 닫아 MainMenuScene 단독 상태로 복귀함.
- 이번 승인 범위는 구현·컴파일·에셋 연결임. PlayMode·합성 입력·자동 테스트·커밋은 실행하지 않음.

## 적용 결과 · 2026-10-20
- 팀 Caption 6개를 각 SlotGroup에 연결함. 저장·재로드 후 부모·NotoSansKR 참조 유지 확인함.
- 통화 참가자 1행을 연결함. 이동 가능한 `CallChannelAnchor`의 배치·간격은 Inspector에서 조정 가능함.
- 중앙 이미지 529.92×287.64, pivot=(0.5,1)로 변경함. 기존 크기의 2/3이며 위 경계를 유지함.
- 메모장 입력은 InterviewImeInputField 1개로 교체함. 다중줄 설정·참조 유지 확인함.
- Good/Normal/BadEndingScene 3개 저장함. 재로드 후 보드별 native Sprite 3종 참조·Build Settings 활성 상태 확인함.
- 최종 컴파일 오류 0건·경고 0건임. 기존 메모장 Bake API 누락은 별도 partial 분리로 복구함.
- 원래 MainMenuScene 단독·active·dirty=false 상태로 복원함. 실제 한글 IME 입력과 Play 동작은 미실측임.
- 근거: `Logs/CallImeEnding/recompile.json`, `ui-apply.json`, `ime-ending-apply.json`, `final-inspection.json`임.
