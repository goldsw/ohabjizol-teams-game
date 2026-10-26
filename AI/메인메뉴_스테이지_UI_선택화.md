# 메인 메뉴 스테이지 UI 선택화

## 원인

- `DesktopView.ValidateBindings`의 0번 항목은 청강코드 창임.
- 창의 `HasAuthoredContent`가 모든 스테이지 슬롯과 장식용 Glyph·Pill·Badge·Caption을 필수로 검사했음.
- 현재 씬은 첫 슬롯 Glyph와 숨긴 1~3번 슬롯 Pill 참조가 없음.
- 청강코드 창 검사 실패 → 바탕화면 검사 실패 → `MainMenuController.HasAuthoredUi` 실패로 오류가 이어졌음.

## 수정

- [ChungkangCodeWindow.cs](../Assets/Scripts/Presentation/Desktop/ChungkangCodeWindow.cs)의 필수 검사를 제목·설명·상태·새 게임 버튼으로 제한함.
- 스테이지 슬롯 배열이 없거나 슬롯이 null이어도 새 게임 창을 사용할 수 있음.
- 비활성화된 슬롯은 Paint에서 건너뛰고, 누락된 장식 요소는 개별적으로 건너뜀.
- `DesktopView`와 `MainMenuController`의 필수 참조 검사 자체는 유지함.
- 씬·프리팹을 재생성하거나 숨긴 스테이지 UI를 활성화하지 않음.

## 확인

- Unity 컴파일 오류·경고 0건.
- 현재 씬의 바탕화면·청강코드 창·메인 메뉴 필수 참조 검사를 읽어 확인함.
- Play·테스트·커밋 미실행함.
- 원본 코드·컴파일 결과·참조 상태: `Logs/MainMenuOptionalStageUi/`.
