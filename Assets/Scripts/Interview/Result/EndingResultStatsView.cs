using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class EndingResultStatsView : MonoBehaviour
    {
        [SerializeField] EndingBoard board;
        [SerializeField] Text totalScore;
        [SerializeField] Text directorScore;
        [SerializeField] Text programmerScore;
        [SerializeField] Text artistScore;
        public bool HasAuthoredUi => board && totalScore && directorScore && programmerScore && artistScore;

        void Start()
        {
            if (!HasAuthoredUi)
            {
                Debug.LogError("Ending RESULT requires an authored board and four score labels.", this);
                return;
            }
            Show(board.Result);
        }

        public void Show(InterviewResult result)
        {
            if (!HasAuthoredUi) return;
            var skills = result?.ActualSkills;
            totalScore.text = "TOTAL SCORE : " + (skills == null ? "—" : (skills.Planning + skills.Programming + skills.Graphics).ToString());
            directorScore.text = "DIRECTOR : " + (skills == null ? "—" : skills.Planning.ToString());
            programmerScore.text = "PROGRAMMER : " + (skills == null ? "—" : skills.Programming.ToString());
            artistScore.text = "ARTIST : " + (skills == null ? "—" : skills.Graphics.ToString());
        }

#if UNITY_EDITOR
        public void ConfigureEditor(EndingBoard owner, Text total, Text director, Text programmer, Text artist)
        {
            board = owner;
            totalScore = total;
            directorScore = director;
            programmerScore = programmer;
            artistScore = artist;
            if (!HasAuthoredUi) throw new InvalidOperationException("Ending RESULT references required.");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
