# Graphify 확인·갱신

- 설치 버전: 0.9.89. 기존 graph.json은 2026-10-14 생성되어 이후 UI 수정이 포함되지 않았음.
- 기존 그래프 조회 후 실제 코드 참조를 확인함. Graphify만으로 질문 버튼의 레이캐스트 원인을 확정하지 않음.
- 2026-10-21 `Assets/Scripts/**/*.cs`, `Assets/Editor/**/*.cs` 202개를 로컬 AST로 재분석함.
- 갱신 결과: 노드 2,065개·관계 5,788개. 네트워크·LLM·문서·이미지 의미 추출 미사용함.
- 기존 그래프 보존: `Logs/TutorialRefresh/backup/graph-20261021T040807.json`.
- 구조 자료: `graphify-out/graph.json`. 실행 기록: `Logs/TutorialRefresh/graphify-refresh.json`.
- 구현 후 재갱신: C# 217개·노드 2,273개·관계 6,450개. 새 Tutorial/Host/Target/Title 연결 포함함.
- 최종 실행 기록: `Logs/TutorialRefresh/graphify-final.json`.
- 질문 참조는 현재 씬의 Screen/Radial 버튼 5개가 모두 일치함. 부모 CanvasGroup·Mask 차단 없음, GraphicRaycaster 정상, HasAuthoredUi=true 확인함.
- 질문 클릭 대응: 회전 제거·열 때 전면 배치·화면 숨김 후 사용 가능 상태 소실 방지함.
- 공용 프리팹 감사: InterviewScreen 인스턴스 override 104개·추가 오브젝트 19개·외부 씬 오브젝트 참조 0개. 별도 Stage 정보는 병합 대상에서 제외함.
- 전역 graphify SKILL.md는 큰 문서이므로 진입 안내와 상세 주제를 분리하는 방식 권장함. 이번 작업에서는 전역 스킬 파일을 변경하지 않음.
