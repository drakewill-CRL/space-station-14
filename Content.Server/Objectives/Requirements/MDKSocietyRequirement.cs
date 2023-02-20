using Content.Server.GameTicking;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class MDKSocietyRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            return mind.HasRole<MDKMemberRole>();
        }
    }
}
