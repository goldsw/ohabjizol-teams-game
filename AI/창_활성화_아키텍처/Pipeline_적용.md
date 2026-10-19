# Pipeline 적용 및 최종 검증

## 결과
- 상태: 코드 적용·씬 초기 설정 저장·자동 검증 완료.
- com.unity.pipeline 0.8.0-exp.1 설치. manifest.json·packages-lock.json 반영.
- Unity Editor 6000.3.15f1, PID 26704, 포트 7800, Pipeline 상태 ready.
- 열린 Editor를 종료하지 않고 Unity CLI 연결 성공.
- 샌드박스 안에서는 프로세스·연결 조회가 제한됨. 샌드박스 밖 실행에서 연결·Git 조회 정상 확인.

## 실행과 검증
- Pipeline eval로 WindowInitialStateSetup.ApplyAndSave() 실행 성공.
- 메뉴·면접 씬에 초기 표시 설정 저장. 기존 설정값 보존.
- 첫 Validate 실패: preview 씬 host의 persistent ID 조회 가정 불일치.
- WindowInitialStateValidation.FindSavedHostBlock으로 수정: MonoScript asset GUID를 씬 YAML의 m_Script GUID와 비교하고 유일한 block의 씬 fileID 사용.
- 0개·복수·파싱 실패는 오류 처리. script asset fileID를 씬 fileID로 사용하지 않음.
- 원인 진단 gpt-6.1-sol/high, 단순 수정 gpt-6.1-sol/low 사용.
- Pipeline recompile_status: completed, failed=false, compilationFailed=false, errors=[].
- Pipeline eval로 WindowInitialStateValidation.Validate() 재실행: success=true, diagnostics=[].
- 검증 항목 통과: 창 최초 숨김·생성 1회, 초기 Opened/Refresh 1회, Close/Open/Toggle, 중복 초기화 무시, 초기 false 무이벤트, 부모 숨김 후 상태 유지.
- 메뉴·면접 씬의 bool 초기 설정과 YAML 저장값 일치 및 재열기 유지 검증 통과.
- GUI 드래그·실제 영상 표시는 이번 자동 검증 범위 밖.
- 커밋 미수행.

## 호출 형식
```powershell
$u = 'C:\Users\eeseo\AppData\Local\Unity\bin\unity.exe'
$p = 'D:\UnityProjects\GitHub\Games\ohabjizol-teams-game'
& $u --no-banner --no-pager --non-interactive pipeline list
$code = 'System.Type.GetType("WindowInitialStateSetup, Assembly-CSharp-Editor").GetMethod("ApplyAndSave").Invoke(null, null);'
& $u --no-banner --no-pager --non-interactive command --project-path $p eval --code $code --result-only
```
- eval C# 코드는 세미콜론 포함. CLI success 및 내부 eval success를 함께 확인.
- 검증 호출은 같은 형식에서 type을 WindowInitialStateValidation, method를 Validate로 변경.
- 샌드박스 환경에서는 로컬 Editor 연결·쓰기 작업에 실행 승인 필요.

## 사용 위치
- MainMenuController → Initial Windows: 청강코드·설정·튜토리얼 초기 표시.
- InterviewScreen → Discord Visible 및 Initial Documents: Discord와 서류별 초기 표시.
- Edit Mode에서 값을 정하고 씬 저장. 이후 실행에서 해당 설정 적용.

## 참조
- [Unity Pipeline 공식 설치·연결 안내](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package)
- [설계](../창_활성화_아키텍처.md)
- [구현·검증 이력](구현_검증.md)