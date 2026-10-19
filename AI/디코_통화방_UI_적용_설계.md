# 디코 통화방 UI 적용 설계

## 결론·범위

- 기존 authored view의 객체·직렬화 참조를 유지하며 native Aseprite Sprite를 연결하는 명시적 Editor 적용기 1개 추가함.
- 설계 기록임. 실제 적용 결과와 사용법은 [적용 과정](디코_통화방_UI_적용_과정.md)에 기록함.
- Hierarchy·Inspector 편집 가능한 외형 적용함. 런타임 재생성·전체 Bake·기존 UI 삭제 금지함.
- G: 원본 수정 금지, 사용자 기존 변경 보존함. 상세 사용자 적용 설명은 구현 후 별도 작성함.

## 확인 근거

- `Assets/Scripts/Interview/UI/InterviewScreen.Authoring.cs:11`: `BakeAuthoredUi()`에서 `HasAuthoredUi`이면 `BakeTeamWindow()` 후 반환함. 기존 통화방 스킨 갱신 진입점으로 사용 불가함.
- 동일 파일: `_discordViewRoot/Background`가 `Resources.Load<Sprite>("Interview/Discord_total")` 사용함. 생성 경로와 신규 스킨 적용 경로 분리 필요함.
- `Assets/Scripts/Interview/UI/InterviewCallView.cs`: `_portrait`에 `SetPose(Sprite)`가 런타임 Sprite를 할당함. `_name`, `_dots`, `speechBubble`, `_visualCatalog` 참조 유지 필요함.
- `Logs/ArtUIValidation/aseprite-ui/ProbeBody.cs.txt`: Sprite 열거·`Image.sprite`·직렬화 `m_Sprite` 참조 확인 패턴 있음. 이번 아트 실행 증거는 아님.
- `Assets/Editor/MainMenuEnvironmentAuthoring.cs`: 소유자 직렬화 참조·선행 검증·Undo·명시적 저장 패턴 참고함.
- 부모/Luna 확인: `InterviewRefreshBake.ApplyAndSave → BakeExisting → BakeAuthoredUi` 경로 있음. 신규 스킨 적용은 이 전체 Bake 경로에서 분리함.
- 부모 확인: `InterviewScene.unity` 기존 수정·`Assets/Art/Discord` 신규 상태 보존함. 백업·원본 SHA는 `Logs/DiscordCallUI/Before`에 저장됨.

## 최소 구현 파일·타입

- 신규 `Assets/Editor/InterviewDiscordSkinAuthoring.cs`: Editor 전용 static 타입 `InterviewDiscordSkinAuthoring` 제안함.
- `Apply()` → 대상·Sprite 검증 → 변경 계획 → Undo 적용 → 대상 씬 dirty 표시함.
- `ApplyAndSave()`로 대상 씬 명시적 저장함. 실행은 열린 Editor 연결 복구 후 수행함.
- 저장 대상: 기존 `Assets/Scenes/InterviewScene.unity` 및 신규 Editor 스크립트 `.meta`임.
- 기존 런타임·Authoring 수정, 신규 런타임 컴포넌트·카탈로그·PNG 불필요함.

## root·기존 객체 해석

- 정확한 대상 씬의 `InterviewScreen` 1개를 사용함. 다중 대상·Prefab asset은 중단함.
- `SerializedObject(screen).FindProperty("_discordViewRoot")`에서 실제 저장된 참조 획득함. 전체 씬 이름 검색·새 Canvas 생성 금지함.
- 비활성 자식 포함 root 상대 경로 사용함. 필수 대상 누락·중복·타입 불일치는 변경 전 중단함.
- 필수 배경 대상: `Background`의 기존 `Image`임. 위치·크기·앵커·피벗·CanvasScaler·sibling 순서 유지함.
- `MainVideo/Portrait`: 지원자 런타임 초상화 소유권 유지함. 통화방 배경·정적 참가자 아트로 덮어쓰지 않음.
- `FixedParticipant0..2`: 기존 타일 Image 사용 가능함. `Icon`은 현재 생성 코드상 `Text`이므로 Image로 가정하면 안 됨.
- 참가자 타일·Icon Text는 기존 시각을 유지함. 신규 `SkinVisual`의 중복 Graphic은 비활성화하고 `Name` 동적 텍스트를 유지함.
- `DownloadPanel`, `Accept`, `Reject`, `Chat` 등 기존 Graphic만 명시적 매핑 가능함. 문서 창·작업표시줄·팀 창 대상 확장 금지함.

## native Sprite 바인딩

- 기본 배경: `Assets/Resources/Interview/Discord_total.png`임. Aseprite는 질문·서류·다운로드·채팅 UI에 사용함.
- Luna 메타 확인: DISCORD 1280×720, 통화방 UI의 Check 13×13 / download 13×13 / File 87×25 / Question 87×25 / Team_List 50×92임. AnimatedSprite 하위 Sprite 메타 존재함.
- `오합지졸-디코` 추가 아트: chat-invoice 51×93 / chat 16×16 / discord_icon 32×32 / LIVE_UI 32×16 / LIVE 83×16 / profile_bg 48×32 / profile_left_under 72×50 / profile 16×16 / sdf 226×32 / server·shap·size·soudn·test_icon 각 16×16임. 정확한 Sprite 식별자는 실행 inventory로 확정함.
- 기존 AnimatedSprite importer 유지함. 메인 GameObject에 `LoadAssetAtPath<Sprite>`만 시도하지 말고 `AssetDatabase.LoadAllAssetsAtPath(path)`에서 실제 `Sprite` 객체 선택함.
- 원본 경로 + 정확한 Sprite 이름 + 필요 시 localFileID로 의도 명시함. 배열 첫 항목 임의 선택 금지함. 적용 시 GUID/localFileID 및 rect를 기록함.
- `Background → Discord_total` 완성 배경을 사용함. `DISCORD`는 빈 프레임임. 파일명만으로 아트의 역할을 판단하지 않음.
- 다중 Sprite는 명시된 1개로 해석함. importer GameObject·SpriteRenderer 인스턴스화 금지함.
- `Image.sprite`에 참조를 직렬화함. 필요 시 틴트만 흰색으로 교정함. `SetNativeSize()`로 기존 레이아웃을 변경하지 않음.
- 실제 Sprite 서브에셋 0개·모호한 선택·필수 아트 누락이면 전체 적용 전 중단함. PNG 추출·import mode 변경·Resources 대체로 우회하지 않음.
- 다중 프레임·레이어 아트는 합성 배경 여부 확인 필요함. 부분 레이어를 완성 배경으로 취급하거나 SpriteRenderer용 Clip을 UI.Image에 직접 연결하지 않음.

## 이벤트·사용자 레이아웃 보존

- 기존 `Button`, `InterviewQuestionView`, `InterviewCallView` 및 직렬화 연결 유지함. 이벤트 추가·삭제·재바인딩 금지함.
- 질문 회전, 질문 2개+빈칸 3개, 판정, 서류 다운로드·문서 창·작업표시줄·팀 창 이벤트와 데이터 유지함.
- 사용자가 조정한 `ApplicantCenter`, 질문 위치·회전·RectTransform 및 캐릭터 모션 경로 유지함. 새 좌표 계산·정렬·레이아웃 Bake 금지함.
- 지원자 표시·말풍선·알림·읽지 않음 상태·패널 열림 상태를 적용기가 초기화하지 않음. `Configure`·`SetPose` 등 런타임 메서드 호출 불필요함.
- 스킨 적용 후 기존 런타임이 Graphic 색·Sprite를 덮어쓰는지 부모가 해당 코드 범위 확인 필요함. 발견 시 필수 최소 수정만 별도 확정함.

## 저해상도·텍스트·클릭 위험

- DISCORD 1280×720은 1920×1080 기준 1.5배 확대라 픽셀 경계 흐림 가능함. 실제 root 영역·Sprite rect 비율부터 비교함. importer·필터·전역 해상도 변경은 이번 기본 범위에 포함하지 않음.
- 원본에 구워진 이름·질문·버튼 글자와 기존 Text 중첩 위험 있음. 동적 텍스트를 일괄 숨기지 않음. 중첩 시 확인된 무문자 Sprite가 있으면 우선 사용하고, 없으면 해당 요소 적용 보류·부모 보고함.
- 통합 DISCORD 아트에 기존 참가자·패널 프레임까지 포함되면 이중 배경 가능함. 확인된 장식 Image만 제한적으로 투명화하고 동적 자식·전체 부모 비활성화 금지함.
- 배경·장식·새 `SkinVisual`·라벨은 `raycastTarget=false` 설정함. 기존 버튼 `targetGraphic`의 hit 영역과 `raycastTarget=true` 유지함.
- Button Sprite 교체 시 `SpriteSwap` transition의 hover/pressed/disabled Sprite가 외형을 덮어쓰는지 확인 필요함. 상태 Sprite 미확정이면 해당 버튼 스킨 적용 보류함.
- 기존 `GraphicRaycaster`, EventSystem, CanvasGroup의 `blocksRaycasts`, 패널 hit 영역 유지함. 새 전체 화면 투명 Graphic으로 클릭 차단하지 않음.

## 명시적 적용·저장·idempotency

- 자동 훅·`InitializeOnLoad`·`OnValidate` 금지함. 열린 Editor에서 명시적 메뉴·연결 명령으로만 적용함.
- EditMode·컴파일/임포트 유휴 상태 확인 후 필수 객체·아트·컴포넌트를 모두 해석함. 검증 실패 시 부분 변경 시작하지 않음.
- 기존 객체는 `Undo.RecordObject`, 신규 장식은 Undo 생성 등록 사용함. Prefab instance 수정은 해당 객체의 override 기록 필요함.
- 적용 메뉴는 씬 dirty만 표시함. 별도 저장 메뉴는 대상 씬만 `EditorSceneManager.SaveScene` 호출하고 반환값 확인함.
- Editor 메모리의 미저장 사용자 편집이 있으면 적용·Undo는 가능함. 자동 저장 경로는 수정 전에 중단하고 명시적 저장 요구함. Git 수정 상태만으로 중단하거나 파일을 초기화하지 않음.
- 다른 dirty 씬 저장·전체 `SaveOpenScenes`·전역 `SaveAssets` 금지함.
- 재적용 시 객체·Sprite 참조 재사용함. 중복 자식·이벤트 생성 금지함. 변경값만 기록·dirty 처리함.
- 예외 시 해당 적용 Undo 그룹만 되돌리고 자동 저장하지 않음. 저장 실패는 실패 상태로 보고하고 사용자 변경을 폐기하지 않음.

## 적용 시 확인·제약

- 열린 `Assets/Scenes/InterviewScene.unity`와 직렬화된 `_discordViewRoot`를 사용함.
- 초기 Sprite 배정 회귀를 복원함. 최종 Image 11곳 연결, 중복 Graphic 9개 비활성화, 참가자 타일 3개 원복함.
- `DiscordCallSpriteInventory.Run()`은 씬 변경 없이 native Sprite 정보와 검토용 미리보기를 기록함.
- 질문 5개는 별도 `OptionsRoot` 소속임. `_questions` 참조·동일 대상 씬·Button identity로 검증하며 Discord 루트 하위 조건을 추가하지 않음.
- `Team_List`와 다운로드 패널 전체 프레임은 기존 툴바·행·닫기 배치를 유지함. 이번 적용은 통화방과 버튼·아이콘 외형에 한정함.
- Unity Editor는 열린 상태임. sandbox 프로세스 조회로 종료를 판단하지 않음. Pipeline 7800에서 명시적 적용함.
- 새 런타임 컴포넌트·전체 Bake 없이 Editor 적용기와 기존 씬의 참조로 구성함. 실제 적용·저장 및 제약은 적용 과정 문서에 기록함.
