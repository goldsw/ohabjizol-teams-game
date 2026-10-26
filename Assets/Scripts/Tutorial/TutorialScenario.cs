namespace Ohabjizol.Tutorial
{
    internal enum TutorialAction
    {
        None, OpenTeam, CloseTeam, OpenDownloads, DownloadApplication, CloseApplication,
        DownloadPortfolio, CloseDownloads, ClosePortfolio, OpenQuestions, AskPortfolio,
        OpenLibrary, OpenPortfolioFile, CloseLibrary, AskSchedule, OpenCalendar,
        CloseCalendar, Accept, OpenNotepad, CloseNotepad, Complete
    }

    internal enum TutorialFocus { None, Screen, Applicant, Colleague, Team, Application, Portfolio, Calendar, Requirements, Notepad, ApplicantSpawn }

    internal readonly struct TutorialStep
    {
        public readonly string Text;
        public readonly TutorialAction Action;
        public readonly TutorialFocus Focus;
        public readonly bool Supervisor;
        public readonly bool Greeting;
        public TutorialStep(string text, TutorialAction action = TutorialAction.None,
            TutorialFocus focus = TutorialFocus.None, bool supervisor = false, bool greeting = false)
        { Text = text; Action = action; Focus = focus; Supervisor = supervisor; Greeting = greeting; }
    }

    internal static class TutorialScenario
    {
        public static readonly TutorialStep[] Steps =
        {
            new TutorialStep("곧 게임잼 면접일이네", supervisor: true),
            new TutorialStep("항상 수고하고 있어", supervisor: true),
            new TutorialStep("최근에 포트폴리오를 허위로 작성해서 면접에 뽑히는 사람들이 많다고 하는데.", supervisor: true),
            new TutorialStep("조심하는 게 좋을 거야.", supervisor: true),
            new TutorialStep("지원자를 잘 뽑는 팁이 있냐고?", supervisor: true),
            new TutorialStep("당연히 있지!", supervisor: true),
            new TutorialStep("노련한 이 선배님이 알려주도록 할게", supervisor: true),
            new TutorialStep("자 기본적으로 면접을 볼 때는 청강코드에서 보게 돼.", focus: TutorialFocus.Screen),
            new TutorialStep("게임 주제에 가장 알맞는 지원자를 선별해서 뽑는게 우리 면접관들의 일이지"),
            new TutorialStep("좀 더 자세히 말하면 포트폴리오와 지원자 질문간의 대조를 통해 비교하는건데"),
            new TutorialStep("천천히 알려줄게."),
            new TutorialStep("이건 우리가 뽑은 지원자들을 표시해줘", TutorialAction.OpenTeam),
            new TutorialStep("총 6명의 지원자를 뽑을 수 있지", focus: TutorialFocus.Team),
            new TutorialStep("현재 팀 창을 닫아보자.", TutorialAction.CloseTeam),
            new TutorialStep("이 화면에는 현재 지원자가 표시 돼", focus: TutorialFocus.ApplicantSpawn),
            new TutorialStep("안녕하세요~ 라고 해야지 굼뜨긴", focus: TutorialFocus.ApplicantSpawn),
            new TutorialStep("농담이야."),
            new TutorialStep("우측 면접관은 Mr.Kim 씨야. 새 면접자가 올때마다 자료를 보내주지. 안녕하세요 김씨!", focus: TutorialFocus.Colleague),
            new TutorialStep("Mr.Kim: 예 안녕하십니까!", focus: TutorialFocus.Colleague, greeting: true),
            new TutorialStep("면접자 지원서와 포트폴리오는 이 곳에서 다운받을 수 있어.", TutorialAction.OpenDownloads),
            new TutorialStep("지원서를 다운로드해보자.", TutorialAction.DownloadApplication),
            new TutorialStep("이건 지원서야. 지원자의 역량과 어떤 파트의 지원자인지 나와있지.", focus: TutorialFocus.Application),
            new TutorialStep("아래쪽에 있는 파트별 역량에서 별이 5개면 10점 0개면 0점으로 이해하면 돼.", focus: TutorialFocus.Application),
            new TutorialStep("포트폴리오가 처참해도 역량이 높을 수 있으니 자세히 보는게 좋을거야", focus: TutorialFocus.Application),
            new TutorialStep("지원서를 닫아보자.", TutorialAction.CloseApplication),
            new TutorialStep("포트폴리오를 다운로드해보자.", TutorialAction.DownloadPortfolio),
            new TutorialStep("다운로드 창을 닫아보자.", TutorialAction.CloseDownloads),
            new TutorialStep("이건 포트폴리오야 지원자가 낸 자신있는 작품이 이 곳에 나와있지", focus: TutorialFocus.Portfolio),
            new TutorialStep("진짜일수도 있고 가짜일수도 있고", focus: TutorialFocus.Portfolio),
            new TutorialStep("어떻게 알 수 있냐고?", focus: TutorialFocus.Portfolio),
            new TutorialStep("질문하기 전에 포트폴리오를 닫아보자.", TutorialAction.ClosePortfolio),
            new TutorialStep("오른쪽 아래 질문 버튼 보여? 그걸 눌러봐", TutorialAction.OpenQuestions),
            new TutorialStep("\"포트폴리오\"와 \"일정표\" 질문이 뜰거야 \"포트폴리오\" 질문을 클릭해볼까?", TutorialAction.AskPortfolio),
            new TutorialStep("잘했어. 이 면접자는 포트폴리오 설명을 보통 수준으로 하고 있네?", focus: TutorialFocus.Applicant),
            new TutorialStep("아까 봤던 포트폴리오가 문서함에 남아있을거야 문서함을 클릭해봐.", TutorialAction.OpenLibrary),
            new TutorialStep("문서함에서 다운로드한 포트폴리오 파일을 열어봐.", TutorialAction.OpenPortfolioFile),
            new TutorialStep("포트폴리오도 보통 수준인거 같고.. 이 면접자는 거짓말은 하지 않았어.", focus: TutorialFocus.Portfolio),
            new TutorialStep("적어도 실력은 입증됐단 말이지", focus: TutorialFocus.Portfolio),
            new TutorialStep("자 그러면 포트폴리오를 닫고", TutorialAction.ClosePortfolio),
            new TutorialStep("문서함도 닫아보자", TutorialAction.CloseLibrary),
            new TutorialStep("프로젝트에서 중요한건 서로 일정을 조율하는 일이기도 해."),
            new TutorialStep("정해진 프로젝트 작업일에 참석을 못하면 로스가 생기거든"),
            new TutorialStep("그럼 일정표 질문을 해볼까?", TutorialAction.OpenQuestions),
            new TutorialStep("일정표 질문을 눌러봐.", TutorialAction.AskSchedule),
            new TutorialStep("음 면접자가 저 날들은 다른 일정이 있나봐.", focus: TutorialFocus.Applicant),
            new TutorialStep("하단에 있는 우리 일정표를 열어보자", TutorialAction.OpenCalendar),
            new TutorialStep("이곳에 빨간색으로 칠해진 부분들이 우리의 작업일이야", focus: TutorialFocus.Calendar),
            new TutorialStep("아까 면접자가 얘기했던 일들과 비교했을때 겹치는 부분이 있어? 없어?", focus: TutorialFocus.Calendar),
            new TutorialStep("그렇지?", focus: TutorialFocus.Calendar),
            new TutorialStep("일정에 차질이 생기면 사소하지만 프로젝트 성과가 떨어지니 주의하라고", focus: TutorialFocus.Calendar),
            new TutorialStep("일정표 하단에는 우리 프로젝트의 주제와 작업 요구량이 있어. 잘 기억해 둬", focus: TutorialFocus.Requirements),
            new TutorialStep("이제 일정표를 닫고,", TutorialAction.CloseCalendar),
            new TutorialStep("모든 질문을 다 던지면 최종 합격을 고를 수 있어."),
            new TutorialStep("한번 뽑아볼까? 합격 버튼을 눌러봐.", TutorialAction.Accept),
            new TutorialStep("현재 팀 창을 열어보자.", TutorialAction.OpenTeam),
            new TutorialStep("합격 버튼을 누르면 현재 팀 창에 지원자가 추가 돼.", focus: TutorialFocus.Team),
            new TutorialStep("여기에 나오는 지원자들의 스텟과 프로젝트 주제의 작업 요구량을 최대한 비슷하게 해야 해.", focus: TutorialFocus.Team),
            new TutorialStep("너무 많이 넘겨도 점수가 깎이고, 너무 적어도 깎이니 주의하라고.", focus: TutorialFocus.Team),
            new TutorialStep("잊지 마 면접관의 목표는 상황에 따라 가장 알맞는 면접자를 뽑는거야.", focus: TutorialFocus.Team),
            new TutorialStep("현재 팀 창을 닫아보자.", TutorialAction.CloseTeam),
            new TutorialStep("메모장을 열어보자.", TutorialAction.OpenNotepad),
            new TutorialStep("추가로 이건 메모장이야. 질문하고 까먹을 수 있으니 자주자주 메모해두라고", focus: TutorialFocus.Notepad),
            new TutorialStep("메모장을 닫아보자.", TutorialAction.CloseNotepad),
            new TutorialStep("설명은 이정도로 됐고 다 듣느라 수고했어.", supervisor: true),
            new TutorialStep("다 설명하느라 고생한 나도 수고했고.", supervisor: true),
            new TutorialStep("그런 의미로 저녁은 너가 사는걸로 하자. 마라탕 어때?", supervisor: true),
            new TutorialStep("튜토리얼이 종료되었습니다. 아무 키나 눌러 시작하세요", TutorialAction.Complete)
        };
    }
}
