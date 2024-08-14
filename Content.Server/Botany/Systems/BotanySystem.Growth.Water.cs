using Content.Server.Botany.Components;
using Content.Shared.FixedPoint;
using Microsoft.EntityFrameworkCore.Update;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool WaterConsumption(EntityUid uid, PlantHolderComponent component, EntityUid seedUid)
    {
        if (component == null || component.Seed == null)
            return true;

        //NOTE: i want components on the seed, but it may or may not have them?
        //var waterComp = CompOrNull<WaterConsumptionComponent>(seedUid);
        //WaterConsumptionComponent? waterComp = component.Seed.Components.FirstOrDefault(c => c.GetType() == typeof(WaterConsumptionComponent)) as WaterConsumptionComponent;
        var waterComp = component.Seed.WaterConsumption;
        if (waterComp == null || waterComp.Amount <= 0)
            return true;

        if (waterComp.Amount > 0 && component.WaterLevel > 0 && _random.Prob(0.75f))
        {
            component.WaterLevel -= MathF.Max(0f,
                waterComp.Amount * PlantHolderSystem.HydroponicsConsumptionMultiplier * PlantHolderSystem.HydroponicsSpeedMultiplier);
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
            return true;
        }

        return false;
    }
}
