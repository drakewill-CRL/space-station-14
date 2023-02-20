using Content.Server.Containers;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Destructible;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class BreakCondition : IObjectiveCondition, ISerializationHooks
    {

        [Dependency] private readonly LightBulbSystem _lightBulbSystem = default!;
        private Mind.Mind? _mind;
        [DataField("prototype")] private string _prototypeId = string.Empty;

        /// <summary>
        /// Help newer players by saying e.g. "break the chief engineer's advanced magboots"
        /// instead of "steal advanced magboots. Should be a loc string.
        /// </summary>
        [ViewVariables]
        [DataField("countTarget", required: true)] private int countTarget = 1;
        [ViewVariables]
        [DataField("countCurrent", required: true)] private int countCurrent = 0;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            
            return new BreakCondition
            {
                _mind = mind,
                _prototypeId = _prototypeId,
                countCurrent = countCurrent,
                countTarget = countTarget
            };
        }

        private string PrototypeName =>
            IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(_prototypeId, out var prototype)
                ? prototype.Name
                : "[CANNOT FIND NAME]";

        public string Title => "Break " + countTarget + "things";

        public string Description => "Broken " + countCurrent + " / " + countTarget;

        public SpriteSpecifier Icon => new SpriteSpecifier.EntityPrototype(_prototypeId);

        public float Progress
        {
            get
            {
                if (_mind?.OwnedEntity is not { Valid: true } owned) return 0f;
                return countCurrent / countTarget;
            }
        }

        public float Difficulty => 1.0f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is BreakCondition breakCondition &&
                   Equals(_mind, breakCondition._mind) &&
                   _prototypeId == breakCondition._prototypeId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BreakCondition) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_mind, _prototypeId);
        }
    }
}
