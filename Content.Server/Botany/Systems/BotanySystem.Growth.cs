using Content.Server.Botany.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public bool OnGrowth(EntityUid uid, PlantHolderComponent plantholder)
    {
        //var seed = GetEntity(plantholder.Seed);
        bool isGrowing = true;

        isGrowing = isGrowing && WaterConsumption(uid, plantholder, uid);

        return isGrowing;
    }
}
