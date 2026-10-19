using System;

namespace Ohabjizol.Interview
{
    // Domain lifetime is independent of the screen's visibility.
    public sealed class StageManager
    {
        readonly Action<string, InterviewResult> _complete;
        bool _started;
        bool _deciding;
        bool _completing;

        public StageManager(StageContext context, Action<string, InterviewResult> complete)
            : this(context, complete, 15, null) { }

        public StageManager(StageContext context, Action<string, InterviewResult> complete, int productionDays, Random random = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _complete = complete ?? throw new ArgumentNullException(nameof(complete));
            Session = new InterviewSession(context.StageDefinition, productionDays, random);
        }

        public StageContext Context { get; }
        public InterviewSession Session { get; }
        public StageNotepad Notes { get; } = new StageNotepad();
        public bool IsDeciding => _deciding;
        public bool CompletionPending => Session.IsFinished && Result == null;
        public Exception CompletionError { get; private set; }
        // Only a successfully persisted result is visible to scene handoff.
        public InterviewResult Result { get; private set; }
        public event Action ApplicantChanging;
        public event Action ApplicantChanged;
        public event Action StateChanged;
        public event Action<Exception> CompletionFailed;

        public void Start()
        {
            if (_started) return;
            _started = true;
            ApplicantChanged?.Invoke();
            StateChanged?.Invoke();
            RetryCompletion();
        }

        public void Decide(bool accepted)
        {
            if (!_started || _deciding || _completing) return;
            if (Session.IsFinished) { RetryCompletion(); return; }
            if (Session.Current == null) return;
            _deciding = true;
            try
            {
                ApplicantChanging?.Invoke();
                if (accepted) Session.Accept(); else Session.Reject();
                ApplicantChanged?.Invoke();
                RetryCompletion();
            }
            finally
            {
                _deciding = false;
                StateChanged?.Invoke();
            }
        }

        public void RetryCompletion()
        {
            if (!_started || !CompletionPending || _completing) return;
            _completing = true;
            try
            {
                // Only persistence errors are completion failures; observers run outside this catch.
                try
                {
                    _complete(Context.RunId, Session.Result);
                }
                catch (Exception error)
                {
                    CompletionError = error;
                    CompletionFailed?.Invoke(error);
                    return;
                }
                Result = Session.Result;
                CompletionError = null;
                Notes.Clear();
            }
            finally
            {
                _completing = false;
                StateChanged?.Invoke();
            }
        }
    }
}
