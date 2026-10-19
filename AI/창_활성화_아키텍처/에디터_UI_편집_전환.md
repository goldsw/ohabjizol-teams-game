# 에디터에서 편집 가능한 UI로 전환

## 현재 범위와 문제
- 현재 완료한 변경은 초기 표시 설정과 런타임 활성 상태 계약임.
- UI 구조·위치·문구·색·이미지를 씬에서 편집하는 전환은 아직 미구현.
- MainMenuController는 생성 루트에 DesktopView를 추가하고 Build를 호출함.
- DesktopView.Build와 InterviewScreen.BuildOverlay가 UI hierarchy를 실행 중 생성함.
- WindowToolbar.Build·InfoWindow.BuildInfo도 창 외형과 콘텐츠를 코드로 구성함.
- InterviewScreen.ApplyLayout·OnValidate 등 위치 재적용은 에디터에서 편집한 RectTransform을 덮어쓸 수 있음.
- Play Mode 생성물을 수정해도 Edit Mode로 돌아오면 저장되지 않으며 다음 실행에서 다시 생성됨.
- 이 문서는 위 확인된 생성 흐름을 바탕으로 한 전환 설계임. 세부 참조·전환 순서는 구현 전 확정 필요.

## 권장 방식
- UI의 편집 원본을 scene/prefab으로 저장.
- 런타임 스크립트는 저장된 UI 참조를 받아 데이터 갱신·버튼 이벤트·창 열기/닫기 처리.
- 위치·크기·앵커·색·고정 문구·이미지는 RectTransform, Text, Image, Button, CanvasScaler에서 편집.
- 반복 목록·동적 창은 준비된 prefab을 Instantiate. 구성요소를 new GameObject로 다시 만들지 않음.

## 자산과 참조
- MainMenuScene: 모니터 Canvas·Desktop prefab instance·아이콘·청강코드/설정/튜토리얼 창 배치.
- InterviewScene: Discord 화면·작업표시줄·문서 창 4개·스탯·합불 버튼·영상 참가자 영역 배치.
- 공통 WindowFrame prefab: 툴바·제목·닫기 버튼·정보 영역의 시각 구조.
- 각 창 prefab: 공통 프레임 구조와 각 콘텐츠 및 해당 InfoWindow 파생 스크립트.
- 공통 프레임에 base 스크립트를 붙이고 파생 스크립트를 중복 부착하지 않음. 각 완성 창 root에 필요한 파생 컴포넌트 하나 연결.
- 창·아이콘·문서 내용의 반복 요소는 공통 prefab/variant로 편집.
- MainMenuController: [SerializeField] DesktopView desktopView 연결. Awake에서 AddComponent·Build 호출 제거.
- DesktopView: 아이콘 Button·창·라벨 참조를 [SerializeField]로 연결. 이미 MonoBehaviour이므로 타입 변경은 필요 없음.
- InterviewScreen: 씬의 UI 참조 또는 별도 InterviewView 참조를 연결해 화면 데이터 갱신.
- 숨겨진 창도 명시적 참조로 연결. 활성 객체만 검색하는 Find 방식에 의존하지 않음.
- prefab에 저장 가능한 폰트·이미지 자산 참조 확인. 런타임 생성 자산의 임시 참조는 저장 원본으로 쓰지 않음.

## 스크립트 책임
- View: 직렬화된 UI 참조, 표시 갱신, 입력 이벤트, 툴바 드래그.
- Presenter/controller: 지원자·스테이지 데이터 바인딩, 청강코드 접속, 로딩·씬 전환, 선택 처리.
- 고정 제목·버튼 문구는 prefab에서 편집. 지원자 이름·스탯·로딩 상태 등 동적 데이터는 코드에서 갱신.
- WindowToolbar/InfoWindow의 상속 관계 및 가상 갱신 훅 유지. 내용은 저장된 자식 참조를 통해 구현.
- WindowFactory는 prefab 복제로 전환. 참조 누락 시 명확한 오류 처리, 자동 UI 재생성으로 누락을 숨기지 않음.

## 초기 상태와 좌표 원본
- 전환 후 초기 표시의 유일 원본은 scene/prefab GameObject 활성 체크(m_Active).
- 기존 DesktopInitialState/DocumentInitialState 및 discordVisible은 이관 때 한 번 반영한 뒤 중복 원본으로 사용하지 않음.
- 바인딩·이벤트 구독 후 저장된 activeSelf를 기준으로 최초 상태 초기화. 이후 Open/Close/Toggle은 런타임 동작.
- 부모 숨김과 개별 창 열림 상태는 분리해 유지.
- WorldCanvas 위치 계산은 Editor의 명시적 배치 명령으로 옮김. 실행 시 에디터 배치를 덮어쓰지 않음.
- ApplyLayout/OnValidate의 자동 RectTransform 덮어쓰기 제거. 레이아웃 초기값 적용은 명시적 Editor 명령에서만 수행.
- 실행 중 툴바 드래그는 그대로 유지. 바탕화면 아이콘은 기존 요구대로 이동하지 않음.

## 이관 순서
1. View 참조 계약과 공통 프레임 구조 확정. 기존 생성 코드는 이관에 필요한 동안 보존.
2. Editor 이관 도구에서 현재 임시 UI 모습을 한 번 생성하여 prefab·scene으로 저장하고 컴포넌트 참조 연결.
3. 런타임 controller를 저장된 UI 바인딩으로 전환. 그 뒤 런타임 UI 생성·위치 재적용 경로 제거.
4. Pipeline으로 컴파일·참조 저장·재열기·실행 동작 검증.
- 이관 도구 재실행 시 기존 자산을 식별해 중복 생성·사용자 편집값 덮어쓰기 방지.
- 닫기·재열기, 고정 아이콘 더블클릭, 청강코드→로딩→줌→씬 전환, 툴바 드래그, 1초 초상 순환, 스테이지 루프 유지.

## 구현 분담
- 공통 UI: WindowToolbar/InfoWindow/WindowFactory·공통 프레임 prefab·참조 초기화 계약.
- 메뉴: MainMenuController/DesktopView·메뉴 콘텐츠 prefab. 메뉴 씬 최종 저장은 이관 담당에게 모음.
- 면접: InterviewScreen/DocumentWindow·면접 콘텐츠 prefab. 면접 씬 최종 저장은 이관 담당에게 모음.
- 이관: Editor 도구·메뉴/면접 씬 참조 연결·설정 이관·최종 저장.
- 공통 계약 완료 후 메뉴·면접 병렬, 최종 이관·검증 순서. 파일별 단일 소유권 적용.
- 원인·설계 gpt-6.1-sol/high, 승인 후 단순 구현 gpt-6.1-sol/low.

## 현재 결정
- 권장: scene/prefab을 편집 원본으로 전환하고 런타임을 참조 바인딩으로 변경.
- 이 문서는 설계 기록임. UI 자산 이관·런타임 생성 제거는 아직 착수하지 않음.