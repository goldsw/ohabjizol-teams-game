# ECC `search-first` 스킬 조사

조사일: 2026-10-19. GitHub `main`의 디렉터리와 원문 `SKILL.md`를 확인함.

## 요약

- **목적:** 새 기능·의존성·통합·유틸리티 구현 전에 기존 해법을 조사해 바퀴 재발명을 줄이는 에이전트 지침임. 실행 스크립트나 패키지가 아님.
- **작동 순서:** 관련 도구 가용성 확인 → 요구 기능·언어·프레임워크 제약 정의 → npm/PyPI, MCP·스킬, GitHub/웹 병렬 조사(복잡한 건 researcher agent 위임) → 기능·유지보수·커뮤니티·문서·라이선스·의존성 평가 → 채택/확장/조합/자체 구현 결정. 검색 채널이 없으면 범위를 밝혀야 함.
- **도구 전제:** 저장소 `rg`, 패키지 관리자(예: npm/pip), 선택적으로 `gh`, MCP/문서 도구와 로컬 스킬 카탈로그. 없는 채널은 웹·공식 문서 등으로 대체하며 조사 범위를 정직하게 보고함. 필수 특정 패키지나 MCP는 없음.
- **Claude/Codex 종속성:** 본문에 `~/.claude/settings.json`, `~/.claude/skills/`, `Claude Code skills`, `Agent(subagent_type="general-purpose")` 예시가 있어 Claude 관례가 섞여 있음. 동시에 `~/.codex/skills`도 언급하고, 구형 `Task(...)` 대신 현재 하네스가 제공하는 agent/subagent 도구명을 쓰라고 명시함. 따라서 핵심 절차는 이식 가능하나 예시 경로·호출은 Codex 도구에 맞춰 치환해야 함. Codex 전용 기능이나 설치 의존성은 원문에 명시되지 않음.
- **Graphify와의 관계:** 상호 보완적임. Graphify(현재 Codex 스킬)는 코드베이스 내부 구조·파일 관계를 그래프화해 질의/경로/설명을 돕고, `search-first`는 저장소 밖의 재사용 가능한 패키지·MCP·스킬·GitHub 구현을 찾아 채택 여부를 결정함. 먼저 Graphify/저장소 검색으로 내부 현황을 파악하고, 부족한 해법을 외부에서 탐색하는 연계가 자연스러움. 이는 두 스킬에 기술된 범위를 비교한 판단임.

## 출처

- [ECC GitHub 스킬 디렉터리](https://github.com/affaan-m/ECC/tree/main/skills/search-first)
- [원문 SKILL.md](https://raw.githubusercontent.com/affaan-m/ECC/main/skills/search-first/SKILL.md)
- [현재 Codex Graphify 스킬](C:/Users/eeseo/.agents/skills/graphify/SKILL.md)
