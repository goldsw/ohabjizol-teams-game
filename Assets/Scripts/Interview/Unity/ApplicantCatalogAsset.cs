using System.Collections.Generic;
using UnityEngine;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Empty shell. With no authored applicants, resolve uses <see cref="PrototypeCatalog.Applicants"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "ApplicantCatalog", menuName = "Ohabjizol/Applicant Catalog")]
    public sealed class ApplicantCatalogAsset : ScriptableObject
    {
        public bool IsEmpty
        {
            get { return true; }
        }

        public IReadOnlyList<Applicant> Resolve()
        {
            return PrototypeCatalog.Applicants;
        }
    }
}
