# StageManager 중심 책임 분리 설계안
작성일: 2026-10-14
상태: 사용자 승인 후 코드 적용됨. 현행 구현은 [아키텍처](./아키텍처.md)와 [책임 계약](./리팩토링/00_책임분리_계약.md)을 기준으로 함.
근거: [구조 검토](./Graphify_구조검토_2026-10-14.md).

## 결론
StageManager가 실행 문맥과 스테이지 수명을 소유하고, InterviewScreen을 UI 입력 이벤트 연결 지점으로 축소하는 방향 권장함.
질문·문서·지원자 표시·알림의 구현은 각 기능 클래스가 담당함. 기존 InterviewSession과 view/workspace를 활용함.

## 책임 계약
- StageContext: 진입 시 RunId·StageId·StageDefinition 확정함. 지원자 문서키도 이 문맥과 지원자 식별 정보로 생성함. 스테이지 도중 저장소·전역 선택값 재조회로 실행 문맥을 변경하지 않음.
- StageManager: 스테이지당 하나, 면접 씬 수명으로 생성함. StageContext·InterviewSession 보유, 시작·판정·지원자 전환·완료 조율 담당함. CurrentApplicant·Result는 세션 상태를 읽는 조회값으로 노출함.
- InterviewSession: 현재 지원자·커서·팀·채용 판정·점수 계산 담당함. 기존 도메인 규칙 유지함.
- QuestionController: 지원자별 미사용/응답 중/완료 상태와 질문 처리 담당함. QuestionPresenter/기존 통화 view가 3초 모션·말풍선·대사 표시 담당함.
- DocumentWorkspace/문서 연결 클래스: 다운로드·문서키·작업표시줄·PDF 창 수명 담당함. 지원자 전환 이벤트에서 기존 문서 정리함.
- ApplicantPresenter·NotificationPresenter: 기존 통화/질문/다운로드 view를 연결하여 지원자 표시·새 서류 알림·읽음 상태 담당함. 상태가 없는 전달만을 위한 클래스를 추가하지 않음.
- InterviewScreen: Inspector의 입력 참조 연결, 질문/합격/불합격/문서/패널 요청 이벤트 담당함. 세션 생성·질문 상태·문서 정리·응답 연출 구현은 담당 클래스에 둠.
- StageCompletionService: 기존 진행 저장소를 활용해 완료 기록·중복 방지·재시도 조율 담당함. StageManager 완료 흐름이 호출하며, 결과 씬은 확정된 결과를 표시함.

## 이벤트 방향
InterviewScreen 입력 요청 → StageManager/기능 처리 → 상태 소유자 이벤트 → 기능 presenter/view 갱신
- 질문 요청: Screen → QuestionController; 상태 변경·답변 준비: QuestionController → presenter.
- 합격/불합격 요청: Screen → StageManager → InterviewSession.
- 지원자 전환: StageManager가 ApplicantChanged 발행함. 질문 상태 초기화·문서 정리·지원자 표시·알림이 이를 구독함.
- 스테이지 완료: StageManager가 진행 중 응답·문서를 정리하고 완료 기록을 요청함. 저장 성공 후 결과 전달·씬 전환함.
- 이벤트는 명시적인 타입과 직접 참조로 연결함. 발행 주체는 해당 상태 소유자로 둠.
- 구독·해제 대칭 보장함. 화면 재활성화 시 최신 상태를 다시 표시하며 이벤트 구독만으로 초기 표시를 가정하지 않음.
- 종료·판정 진입을 한 번만 허용함. 저장의 중복 방지는 RunId·StageId 기준으로 정의함. 순차 저장 집계만으로 원자성이 확보되었다고 간주하지 않음.

## 질문 취소 정책
권장 기본값: 화면 표시 중단 시 응답을 취소하고 해당 질문을 미사용으로 복구함.
- 3초 응답이 완료되었을 때 질문 소진을 확정함.
- 합격/불합격·지원자 교체·스테이지 종료는 이전 응답을 취소하고 이전 지원자의 답변이 새 화면에 표시되지 않도록 식별값을 확인함.
- 질문 상태는 UI 활성 상태와 분리함. 창 숨김·컴포넌트 비활성화·면접 종료의 취소 동작을 명시함.
- 응답 재개 방식으로 변경할 경우 동일 질문의 남은 시간·최종 답변 상태까지 별도 계약이 필요함.

## 적용 순서
1. StageContext·StageManager 도입, 세션 생성과 전역 문맥 접근 이동함.
2. QuestionController로 질문 상태·취소 정책 분리함.
3. 기존 document/view에 지원자 전환 이벤트 연결함. 정리 순서가 필요한 작업은 StageManager 전환 절차로 명시함.
4. 완료 저장을 결과 씬 Start에서 완료 서비스로 이동함.
5. InterviewScreen을 입력 이벤트 연결 지점으로 축소하고 기존 Bake/Inspector 참조 유지함.

## 별도 개선
- StageManager 도입만으로 저장 배열 길이 문제는 해결되지 않음. StageProgressStore의 StageId 저장·스키마 변환 별도 필요함.
- DesktopView 앱 목록의 직렬화와 기존 아키텍처 문서 갱신도 별도 작업임.
- 클래스 경계·이벤트명은 제안임. 후속 구현에서 적용한 상세 책임과 검증 결과는 현행 아키텍처 문서에 기록함.

