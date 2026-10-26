# UI 효과음

- 클릭음 원본: `C:/Users/eeseo/Downloads/button_click.wav`.
- 대사음 원본: `C:/Users/eeseo/Downloads/Text blip(대사 출력용.wav`.
- 프로젝트 클립: `Assets/Audio/Sfx/button_click.wav`, `TextBlip.wav`.
- 원본과 복사본 SHA256 일치 확인함. 짧은 효과음은 PCM·DecompressOnLoad·사전 로딩 설정함.
- 공용 플레이어: `Assets/Scripts/Presentation/UiSoundPlayer.cs`.
- 연결: `Assets/Resources/Audio/SceneBgmPlayer.prefab/InterfaceSounds`.
- 클릭·대사 전용 AudioSource 2개를 사용하며 BGM 소스·매핑·재생 조건은 유지함.
- 기존 `AudioListener.volume` 설정을 따름. 리스너 추가 없음.

## 재생

- 일반 Button은 씬 로드 및 동적 생성 확인 시 onClick을 한 번씩 연결함.
- 바탕화면 아이콘, 문서/작업표시줄 문서 항목, 메인 시작 화면에도 클릭음 연결함.
- 튜토리얼 대사창 진행 입력은 클릭음을 재생하지 않음. 안내된 실제 버튼을 누르는 경우의 클릭음은 유지함.
- 타자 효과의 새 비공백 글자 표시 시 대사음을 재생함. 전체 문장 즉시 표시에는 몰아서 재생하지 않음.
- 대사음은 전용 소스 한 개에서 0.06초 길이·최소 0.06초 간격으로 재생해 긴 원본이 중첩되지 않도록 함.
- 대사 전환·즉시 완성·오버레이 종료 시 남은 대사음을 정지함.
- 볼륨과 대사음 길이·간격은 공용 플레이어 Inspector에서 변경 가능함.

## 검증

- Unity 컴파일 오류·경고 0건.
- Resources 공용 프리팹의 두 클립·전용 소스·글로벌 음량 적용 설정 확인함.
- BGM 컴포넌트·AudioSource 직렬화 설정 불변 확인함.
- Play 재생·청음·자동 테스트·커밋 미실행함.
- 로그: `Logs/UiSoundEffects/`.
