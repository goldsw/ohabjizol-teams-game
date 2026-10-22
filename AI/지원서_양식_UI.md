# 지원서 양식 UI

- 공통 창: [PdfViewer.prefab](../Assets/Prefabs/InterviewRefresh/Documents/PdfViewer.prefab)
- 문서함 배경: `Assets/Art/Background/UI/Documents/PDF.aseprite`
- 지원서 종이: `Assets/Art/Background/UI/Documents/지원서.aseprite`의 `portfolio` 스프라이트
- 지원서 창은 지원서 종이만 표시함. PDF 배경을 사용하지 않음.
- 포트폴리오 배경: `PdfDocumentViewer.portfolioBackgroundSprite`에 별도 연결 가능함. 현재 미지정 상태이며 기존 포트폴리오 내용 유지함.
- 인적사항: 지원자의 이름과 직군만 표시함.
- 오른쪽 위 사진: 지원자의 `CharacterId`로 기존 `ApplicantVisualCatalog`의 Idle 프로필을 조회함. 지원자 변경 시 교체하며, 식별자·이미지가 없으면 빈 사진 칸으로 표시함.
- 경력·프로젝트 경험: 읽을 수 없는 장식용 선으로 표시함.
- 예상능력: 기존 `ApplicantStatsView`의 표시 능력치·별점·퍼센트 유지함.
- 지원서 창은 종이 비율에 맞게 표시하며, 문서함 목록은 PDF 배경의 오른쪽 영역에 배치함.
- 포트폴리오 카드·다운로드·닫기·최소화·작업표시줄 연결 유지함.

## 관리 위치

- `PdfDocumentViewer`: 문서 종류별 표시 전환과 지원자 데이터 연결
- `ApplicationFormView`: 종이 크기와 이름·직군 표시
- `ApplicantPresentationFactory` → `ApplicantDocumentPacket.CharacterId` → `ApplicationFormView`: 사진의 지원자 식별자 전달
- `DocumentScribbleGraphic`: 경력·프로젝트 칸의 장식용 선
- `ApplicationDocumentBake.Apply()`: 기존 공통 프리팹에 양식 적용
- `ApplicationDocumentBake.EnsurePrefab()`: 양식이 없는 프리팹에만 적용; 기존 수동 배치 보존
- `DocumentLibrarySpriteBake`: 문서함 PDF 스프라이트 적용; 기존 목록·닫기·최소화 연결 유지

## 확인

- 적용 전 공통 프리팹 백업: `Logs/ApplicationForm/Before/`
- Unity 컴파일: 오류 0건·경고 0건.
- 편집 모드 프리팹 미리보기: 이름·직군, 경력·프로젝트 장식 선, 반쪽 별점·퍼센트 배치 확인함.
- 저장된 양식·별점·닫기 버튼 참조 정상; 미리보기 기록은 `Logs/ApplicationForm/`에 저장함.
- 실행 검증·테스트·커밋: 이번 요청 범위에 없음.

## 스프라이트 용도 정정

- 지원서: `PdfViewer.prefab`의 `ApplicationForm/Paper` 이미지
- 문서함: `DocumentLibrary.prefab`의 루트 이미지
- 포트폴리오: `PdfDocumentViewer`의 `Portfolio Background Sprite` 필드
- 정정 적용 백업·컴파일·미리보기: `Logs/DocumentSpriteCorrection/`
- 인게임 `InterviewScreen.prefab`의 문서함에는 이전 `680×580` 크기 오버라이드가 있었음. 해당 크기 오버라이드만 해제하여 원본의 `1100×618.45` 비율을 상속하게 수정함.
- 지원서 전체 종이와 문서함 PDF 배경을 편집 모드 미리보기로 확인함. 컴파일 오류·경고 0건, Play 미실행함.
- 오른쪽 위 프로필 사진 추가: 프리팹 저장·편집 미리보기 확인함. 컴파일 오류·경고 0건; 기록은 `Logs/ApplicationPortrait/`에 저장함.
