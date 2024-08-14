using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class WaterConsumptionComponent : Component
{
    [DataField("amount"), ViewVariables(VVAccess.ReadWrite)]
    public float Amount = 0.5f;
}
