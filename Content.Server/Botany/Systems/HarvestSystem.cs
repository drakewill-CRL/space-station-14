using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Botany;

public sealed class HarvestSystem : EntitySystem
//public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public TimeSpan NextUpdate = TimeSpan.Zero;
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HarvestOnceComponent, OnHarvestEvent>(OnHarvestOnce);
        SubscribeLocalEvent<HarvestRepeatComponent, OnHarvestEvent>(OnHarvestRepeat);
        SubscribeLocalEvent<HarvestScreamComponent, OnHarvestEvent>(OnHarvestScream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (NextUpdate > _gameTiming.CurTime)
        {
            var query = EntityQueryEnumerator<HarvestAutoComponent>();
            while (query.MoveNext(out var uid, out var harvestAuto))
            {
                var args = new OnHarvestEvent();
                args.user = uid;
                DoHarvest(uid, args);
            }
            NextUpdate = _gameTiming.CurTime + UpdateDelay;
        }
    }

    private void OnHarvestOnce(EntityUid uid, HarvestOnceComponent component, OnHarvestEvent args)
    {
        DoHarvest(uid, args);
    }

    private void OnHarvestRepeat(EntityUid uid, HarvestRepeatComponent component, OnHarvestEvent args)
    {
        DoHarvest(uid, args, removePlant: false);
    }

    private bool DoHarvest(EntityUid uid, OnHarvestEvent args, PlantHolderComponent? component = null, bool removePlant = true)
    {
        if (!Resolve<PlantHolderComponent>(uid, ref component))
            return false;

        if (component.Seed == null || component.Dead || Deleted(args.user))
            return false;

        if (component.Harvest)
        {
            if (TryComp<HandsComponent>(args.user, out var hands))
            {
                if (!CanHarvest(component.Seed, hands.ActiveHandEntity))
                    return false;
            }
            else if (!CanHarvest(component.Seed))
            {
                return false;
            }

            Harvest(component.Seed, args.user, component.YieldMod);
            _plantHolder.AfterHarvest(uid, component);
            return true;
        }

        _plantHolder.AfterHarvest(uid, component);
        return true;
    }

    public bool CanHarvest(SeedData proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    public IEnumerable<EntityUid> Harvest(SeedData proto, EntityUid user, int yieldMod = 1)
    {
        if (proto.ProductPrototypes.Count == 0 || proto.Yield <= 0)
        {
            _popup.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return Enumerable.Empty<EntityUid>();
        }

        var name = Loc.GetString(proto.DisplayName);
        _popup.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);
        return GenerateProduct(proto, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedData proto, EntityCoordinates position, int yieldMod = 1)
    {
        var totalYield = 0;
        if (proto.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = proto.Yield;
            else
                totalYield = proto.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        //if (totalYield > 1 || proto.HarvestRepeat != HarvestType.NoRepeat)
            //proto.Unique = false;

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            _randomHelper.RandomOffset(entity, 0.25f);
            products.Add(entity);

            var produce = EnsureComp<ProduceComponent>(entity);

            produce.Seed = proto;
            ProduceGrown(entity, produce);

            _appearance.SetData(entity, ProduceVisuals.Potency, proto.Potency);

            if (proto.Mysterious)
            {
                var metaData = MetaData(entity);
                _metaData.SetEntityName(entity, metaData.EntityName + "?", metaData);
                _metaData.SetEntityDescription(entity,
                    metaData.EntityDescription + " " + Loc.GetString("botany-mysterious-description-addon"), metaData);
            }
        }

        return products;
    }

    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!_botany.TryGetSeed(produce, out var seed))
            return;

        foreach (var mutation in seed.Mutations)
        {
            if (mutation.AppliesToProduce)
            {
                var args = new EntityEffectBaseArgs(uid, EntityManager);
                mutation.Effect.Effect(args);
            }
        }

        if (!_solutionContainerSystem.EnsureSolution(uid,
                produce.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
            return;

        solutionContainer.RemoveAllSolution();
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && seed.Potency > 0)
                amount += FixedPoint2.New(seed.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New(MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }


    private void OnHarvestScream(EntityUid uid, HarvestScreamComponent component, OnHarvestEvent args)
    {
        _audio.PlayPvs(component.ScreamSound, uid);
    }
}

public sealed class OnHarvestEvent : EntityEventArgs
{
    public EntityUid user;
}
