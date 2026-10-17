using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ohabjizol.Interview
{
    [CreateAssetMenu(fileName = "ApplicantCatalog", menuName = "Ohabjizol/Applicant Catalog")]
    public sealed class ApplicantCatalogAsset : ScriptableObject
    {
        public ApplicantProfileAsset[] profiles = new ApplicantProfileAsset[0];
        public bool IsEmpty => profiles == null || profiles.Length == 0;
        public IReadOnlyList<Applicant> Resolve()
        {
            if (IsEmpty) return PrototypeCatalog.Applicants;
            var random = new System.Random();
            var result = new Applicant[profiles.Length];
            for (int i = 0; i < result.Length; i++) result[i] = profiles[i].Sample(random);
            return Array.AsReadOnly(result);
        }
        public ApplicantProfileAsset Find(string id)
        {
            if (profiles != null) foreach (var profile in profiles) if (profile != null && profile.characterId == id) return profile;
            throw new InvalidOperationException("Unknown character ID: " + id);
        }
    }
}