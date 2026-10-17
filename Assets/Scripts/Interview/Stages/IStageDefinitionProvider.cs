using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public interface IStageDefinitionProvider
    {
        StageDefinition Get(StageId id);
        IReadOnlyList<StageDefinition> All { get; }
    }
}