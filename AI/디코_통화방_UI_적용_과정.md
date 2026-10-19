# 디코 통화방 UI 적용 과정

## 상태

- 2026-10-19: native Sprite 연결·InterviewScene 저장 완료함.
- 배경 회귀 복원 후 Image 11곳에 연결함. 완성 PNG와 질문·서류·다운로드·채팅 Sprite를 사용함.
- 최종 컴파일: 오류 0건·경고 0건임. PlayMode 실행·입력·애니메이션 동작 테스트는 수행하지 않음.
- 대상: `Assets/Scenes/InterviewScene.unity`의 기존 통화방 UI임.
- 설계: [디코 통화방 UI 적용 설계](디코_통화방_UI_적용_설계.md) 참고함.

## 1. 무엇을 연결하는지

- `.aseprite` 원본은 Unity Aseprite Importer가 읽음.
- `Animated Sprite`는 임포트 모드임. 가져온 최상위 자산은 `GameObject`이고 내부에 `Sprite`와 `Texture2D`가 있음.
- 통화방 UI는 내부 **Sprite를 기존 UI Image의 Source Image에 지정**하는 방식임.
- PNG 재변환이나 SpriteRenderer 프리팹 배치는 필요하지 않음.
- 런타임에는 저장된 Image의 Sprite 참조를 사용함. Editor의 AssetDatabase 호출을 게임 실행 코드에 넣지 않음.

## 2. 실제 자산 확인

- 공통 아트: `Assets/Art/Discord/Source/Aseprite/오합지졸-디코`임.
- 추가 통화방 아트: `Assets/Art/Discord/Source/Aseprite/디코 통화방 UI`임.
- 두 폴더의 Aseprite 자산 20개에서 실제 Sprite 하위 자산을 확인함.
- `DISCORD`의 Sprite 영역은 1280×720이고 Texture2D는 2048×1024임. 이 아트는 빈 프레임이므로 전체 배경으로 사용하지 않음.
- `Question`의 실제 Sprite 영역은 84×21, `File`은 85×23, `Team_List`는 48×90임. 원본 캔버스 크기와 투명 여백을 제거한 Sprite 크기는 다를 수 있음.
- RawImage에 전체 Texture2D를 넣으면 아틀라스의 빈 영역까지 표시될 수 있음. Image에 Sprite를 넣으면 해당 Sprite 영역을 사용함.
- 식별 결과: [sprite-inventory.json](../Logs/DiscordCallUI/sprite-inventory.json)임.
- PNG 미리보기는 `Logs/DiscordCallUI/Previews`에만 생성함. 최종 UI는 원본 Sprite를 참조함.

## 3. 기존 화면을 유지하는 이유

- 기존 `InterviewScreen`에는 질문, 판정, 문서, 알림, 캐릭터 표시 등의 참조가 저장되어 있음.
- 화면 전체를 새로 Bake하면 사용자가 조절한 좌표와 연결을 잃을 수 있음.
- 기존 `BakeAuthoredUi()`는 완성된 UI가 있으면 대부분의 화면 구성을 건너뜀. 새 아트 적용은 별도 Editor 명령으로 분리함.
- `ApplicantCenter`, 캐릭터 Portrait, RadialQuestions의 위치·회전 제어와 기존 버튼 이벤트는 유지함.
- 변경 대상은 Image의 Sprite·색, 기존 프레임의 외형, 명시적인 장식 이미지임.
- 질문 5개는 현재 별도 `OptionsRoot` 아래에 있음. 통화방 배경의 자식인지 추측하지 않고 `_questions`에 저장된 실제 View 참조를 사용함.

## 4. 아트 배정 기준

- `Discord_total` PNG Sprite → 채널·서버·통화 화면·도구가 포함된 전체 Background임.
- `Question` → 기존 질문 버튼의 배경임. 질문 문구와 질문 소진 처리는 기존 View가 담당함.
- `File`, `download` → 지원서·포트폴리오 다운로드 버튼의 외형임. 다운로드 진행 및 완료 처리는 기존 View가 담당함.
- 참가자 타일·Icon Text는 백업의 기존 시각 속성을 사용함. 신규 `profile_bg`·`profile` 덮어쓰기는 되돌림.
- `chat` → 기존 채팅 버튼의 아이콘임. 읽지 않음 표시와 버튼 동작은 기존 객체가 담당함.
- `sdf`, `profile_left_under`, `server`, `shap`, `soudn`, `LIVE_UI`의 추가 장식은 완성 배경과 중복되어 비활성화함.
- `Team_List`와 다운로드 패널 전체 프레임은 기존 툴바·행·닫기 배치를 유지함. 이번에는 통화방 및 버튼·아이콘 외형을 적용함.
- 합격·불합격은 기존 버튼과 색을 사용함. `Check`를 의미 확인 없이 합격 버튼에 배정하지 않음.

## 5. Editor 적용기의 처리 순서

1. EditMode이며 컴파일·임포트 중이 아닌지 확인함.
2. 정확한 InterviewScene에서 기존 InterviewScreen과 직렬화된 UI 참조를 찾음.
3. 각 Aseprite 경로의 하위 자산을 `AssetDatabase.LoadAllAssetsAtPath()`로 읽음.
4. 필요한 Sprite를 정확한 이름으로 선택함. 필수 자산이 없거나 모호하면 변경 전에 중단함.
5. 기존 Image에 Sprite를 할당하고, 명시적인 장식 Image는 고정된 이름으로 추가·재사용함.
6. 변경은 Undo에 기록함. 예외 발생 시 해당 작업의 변경만 되돌림.
7. 적용만 하는 메뉴는 씬을 수정 상태로 남김. 적용·저장 메뉴는 대상 InterviewScene만 저장함.
8. 저장 메뉴는 기존 미저장 편집이 있으면 변경 전에 중단함. 다른 씬을 일괄 저장하지 않음.
9. Sprite 원본 경로·이름·GUID·localFileID와 적용 대상을 보고서에 기록함.

### 적용 메뉴

- Unity 최상단 메뉴의 `Ohabjizol → Interview → Apply Aseprite Call UI`는 외형만 적용하고 씬을 수정 상태로 남김.
- `Apply Aseprite Call UI And Save`는 깨끗한 대상 씬에 적용한 뒤 해당 씬만 저장함. 미저장 편집이 있으면 선행 검사에서 중단함.
- 기존 사용자 편집이 있던 이번 실행은 메모리 상태를 별도 복사한 뒤 Apply 메뉴를 사용함. 이후 대상 InterviewScene만 명시적으로 저장함.
- 이 메뉴는 Editor용임. 게임 실행 중 UI를 새로 만드는 기능이 아님.

## 6. Unity에서 직접 편집하는 방법

1. Project에서 위 Aseprite 폴더로 이동함.
2. `.aseprite` 자산 왼쪽 화살표를 펼침.
3. 하위 자산 중 Sprite 아이콘이 있는 항목을 선택함. 최상위 GameObject와 구별함.
4. Hierarchy에서 수정할 배경이나 버튼을 선택함.
5. Inspector의 `Image → Source Image`로 Sprite를 드래그함.
6. 원본 색으로 표시하려면 Image Color를 흰색으로 설정함. 실제 버튼의 강조·비활성 색은 별도임.
7. 배경·장식은 Raycast Target을 꺼서 아래 버튼 클릭을 막지 않도록 함.
8. 버튼은 기존 Button 컴포넌트와 이벤트 연결을 유지함. 버튼 GameObject를 새 Sprite 프리팹으로 교체하지 않음.
9. 크기는 RectTransform에서 조절함. Set Native Size는 현재 Canvas의 배치가 달라질 수 있으므로 의도할 때만 사용함.
10. 수정 완료 후 InterviewScene을 저장함.

## 7. 애니메이션과 원본 관리

- 이번 20개 자산은 각각 Sprite 1개를 제공함. 정적 통화방 UI로 연결함.
- Animated Sprite 모드라고 해서 Image가 자동으로 프레임을 순환하지는 않음. 다중 프레임 UI 재생은 별도 프레임 제어가 필요함.
- 지원자 Idle·Talk·합격·불합격 애니메이션은 기존 캐릭터 표시 경로를 사용함.
- `.aseprite`와 `.meta`를 함께 보존함. 재임포트 후 Sprite 식별자가 바뀌면 다시 연결할 수 있음.
- 공유 G: 원본은 이번 UI 연결 작업의 변경 대상이 아님.

## 실행 기록

- 열린 Editor: Unity 6000.3.15f1, InterviewScene, PID 34872임.
- Pipeline: 포트 7800, 조회 시 ready / Play stopped / 컴파일 없음 확인함.
- sandbox에서 프로세스가 보이지 않은 결과를 종료로 판단한 설명은 정정함. 승인된 읽기 전용 조회에서 열린 Editor를 확인함.
- 새 Unity 배치 인스턴스 실행·Editor 종료는 수행하지 않음.
- 기존 씬 백업과 DISCORD 원본 SHA: `Logs/DiscordCallUI/Before`임.
- 적용 직전 미저장 편집이 확인되어 현재 메모리의 씬도 `SaveScene(..., saveAsCopy: true)`로 별도 보관함. 원래 씬 경로와 미저장 상태를 유지한 복사임.
- 초기 Image 23곳 적용에서 빈 프레임을 배경으로 오판한 회귀 발생함. 배경·참가자 시각 복원 후 최종 Image 연결은 11곳임.
- `InterviewScene` 저장 성공, 저장 직후 `dirty=false` 확인함. 기존 미저장 내용과 새 외형을 함께 보존한 저장임.
- 원본 Aseprite 20개는 연결 전후 SHA256이 동일함. 원본 임포트 모드·파일 내용 변경 없음.
- 최신 결과: [적용 보고서](../Logs/DiscordCallUI/background-repair-apply.json), [저장 결과](../Logs/DiscordCallUI/background-repair-save.json), [컴파일 결과](../Logs/DiscordCallUI/background-repair-recompile.json)임.
- 적용 보고서의 `applied-dirty`는 Apply 메뉴 직후 상태임. 최종 저장 상태는 별도 저장 결과의 `saved=true`, `dirty=false`가 근거임.
- [회귀 원인·복구 기록](디코_배경_복구.md)에 중복 장식 9개 비활성화와 참가자 백업 복원 근거를 기록함.
