# Aseprite → Unity UI 연결 검증

## 결론·환경

- 결과: `PASS_STATIC_SPRITE_UI_ONLY` — 두 원본의 imported Sprite를 UI Image에 연결·정적 표시 검증 통과함.
- 환경: Unity `6000.3.15f1`, `com.unity.2d.aseprite@3.0.2`, URP 사용함.
- 실제 공개 속성 `importMode.public=AnimatedSprite` 확인함. serialized `fileImportMode=UNKNOWN`은 공개 속성 검증으로 보완함.
- 두 자산 모두 main asset `UnityEngine.GameObject`, 하위 Sprite 각 1개임.

## 연결법

1. Project에서 `.aseprite` 자산을 펼침.
2. 하위 **Sprite**를 `UnityEngine.UI.Image`의 **Source Image**에 지정함.
3. 최상위 GameObject는 Source Image가 요구하는 Sprite 타입이 아님.

- 현재 importMode 전환·PNG 재변환 불필요함.
- 검증 원본: `Assets/Art/Discord/Source/Aseprite/오합지졸-디코/discord_icon.aseprite`, `Assets/Art/Discord/Source/Aseprite/message_icon.aseprite`임.

## 실행 근거

- 기준 실행: `2026-10-19T09:38:57Z`, [결과 JSON](../../Logs/ArtUIValidation/aseprite-ui/result-20261019T093857064.json)임.
- 두 Sprite 모두 `referenceEqual=True`, `serializedEqual=True` 확인함.
- blank 대비 픽셀 차이: magenta 양성 대조군 `160000`, discord_icon `156250`, message_icon `93239`임.
- 두 Sprite 모두 `VISIBLE`; 저장된 두 PNG를 직접 열어 실제 모양 확인함.
- 렌더: 임시 unsaved additive scene + Game Camera + URP `RenderPipeline.StandardRequest` 사용함.
- 초기 PreviewRenderUtility 경로는 magenta 대조군까지 blank였음. 해당 하네스 결과를 Aseprite 실패로 해석하지 않음.
- [blank PNG](../../Logs/ArtUIValidation/aseprite-ui/ui-blank.png), [양성 대조군 PNG](../../Logs/ArtUIValidation/aseprite-ui/ui-positive.png)임.
- [discord_icon PNG](../../Logs/ArtUIValidation/aseprite-ui/ui-discord_icon.png), [message_icon PNG](../../Logs/ArtUIValidation/aseprite-ui/ui-message_icon.png)임.
- [실행 하네스](../../Logs/ArtUIValidation/aseprite-ui/ProbeBody.cs.txt) — Unity CLI CSharp eval 단일 script body임.

## 보존 확인

- JSON: `sceneSnapshotUnchanged=True`, `pipelineSnapshotUnchanged=True` 확인함.
- 후속 확인: MainMenuScene, handle `-1336`, dirty `False`, sceneCount `1`, selection `55976:discord_icon`, play `False`, MainStage임.
- SHA256 재검증: 두 `.aseprite` + 각 `.meta` + MainMenuScene, 총 5개 파일의 재검증 전후 해시 동일함.
- 최초 해시 측정은 preview 시도 1회 이후·일반 scene 테스트 이전임. 전체 작업 시작 전 baseline을 의미하지 않음.
- 임시 scene·GameObject 정리 완료, 제품 Assets·설정 불변, Play 시작 안 함(실행 기록·후속 확인 근거).

## 검증 범위·한계

- 현재 두 원본은 각 1프레임이며 AnimationClip 하위 자산 없음.
- 검증 범위: 실제 imported Sprite의 Image 참조 연결·직렬화 참조 일치·정적 렌더 표시임.
- 자동 애니메이션·다중 프레임 재생은 미검증·이번 원본에는 해당 없음. 두 Sprite 교체를 Aseprite 애니메이션 성공으로 취급하지 않음.
- 다중 프레임 UI에는 `Image.sprite`를 대상으로 하는 animation 또는 프레임 재생기 별도 필요함.
- SpriteRenderer 대상 클립을 UI Image에 그대로 적용하는 방식은 사용할 수 없음. 이번 자산에는 해당 클립 자체가 없음.
- 성공 상태는 현재 두 원본·현재 환경에 한정함. 향후 다중 프레임 자산의 동작을 보장하지 않음.

## 공식 참고

- [Aseprite Importer Features](https://docs.unity3d.com/Packages/com.unity.2d.aseprite@3.0/manual/ImporterFeatures.html)
- [UnityEngine.UI.Image.sprite](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.html#UnityEngine_UI_Image_sprite)
