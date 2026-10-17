using UnityEngine;
namespace Ohabjizol.Interview
{
    [DefaultExecutionOrder(-10000)]
    public sealed class InterviewDataInstaller : MonoBehaviour
    {
        public ApplicantCatalogAsset applicantCatalog;
        public StageStatsCatalogAsset stageStatsCatalog;
        void Awake()
        {
            if (applicantCatalog != null && stageStatsCatalog != null)
                StageCatalog.Configure(new ScriptableStageDefinitionProvider(applicantCatalog, stageStatsCatalog));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetProvider() { StageCatalog.Configure(null); }
    }
}