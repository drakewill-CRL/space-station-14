using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Upgrades a plant's harvest type.
/// </summary>
public sealed partial class PlantMutateHarvest : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        if (args.EntityManager.TryGetComponent<HarvestOnceComponent>(args.TargetEntity, out var _))
        {
            args.EntityManager.RemoveComponent<HarvestOnceComponent>(args.TargetEntity);
            args.EntityManager.AddComponent<HarvestRepeatComponent>(args.TargetEntity);
        }
        else if (args.EntityManager.TryGetComponent<HarvestRepeatComponent>(args.TargetEntity, out var _))
        {
            args.EntityManager.RemoveComponent<HarvestRepeatComponent>(args.TargetEntity);
            args.EntityManager.AddComponent<HarvestAutoComponent>(args.TargetEntity);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
