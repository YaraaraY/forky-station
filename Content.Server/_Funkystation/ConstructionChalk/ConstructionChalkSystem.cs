using System.Linq;
using Content.Server.Construction;
using Content.Shared._Funkystation.ConstructionChalk;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Funkystation.ConstructionChalk;

public sealed partial class ConstructionChalkSystem : SharedConstructionChalkSystem
{
    [Dependency] private IPrototypeManager _proto = null!;
    [Dependency] private ConstructionSystem _construction = null!;
    [Dependency] private SharedInteractionSystem _interaction = null!;
    [Dependency] private SharedTransformSystem _transform = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedPhysicsSystem _physics = null!;
    [Dependency] private EntityLookupSystem _lookup = null!;

    private const string ChalkMarkPrototype = "ConstructionChalkMark";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChalkPlaceMarkEvent>(OnPlaceMark);
        SubscribeLocalEvent<ConstructionChalkMarkComponent, InteractUsingEvent>(OnMarkInteractUsing);
        SubscribeLocalEvent<ConstructionChalkComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<ConstructionChalkComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
    }

    private void OnPlaceMark(ChalkPlaceMarkEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!TryComp<ConstructionChalkComponent>(GetEntity(ev.Chalk), out var chalkComp))
            return;

        var coords = GetCoordinates(ev.Coordinates);
        var mapPos = _transform.ToMapCoordinates(coords);
        if (!TryResolveFamily(ev.ConstructionPrototype, out var familyRoot, out var layerIndex, chalkComp.Mode) ||
            !_proto.TryIndex<ConstructionPrototype>(ev.ConstructionPrototype, out var recipe))
        {
            return;
        }

        if (MarkAlreadyPresent(mapPos, familyRoot, layerIndex))
            return;

        bool IgnoreOccupied(EntityUid e)
        {
            if (!TryComp<FixturesComponent>(e, out var fixtures))
                return false;

            var aabb = _physics.GetWorldAABB(e, fixtures);
            return aabb.Contains(mapPos.Position);
        }

        if (!_interaction.InRangeUnobstructed(user, mapPos, predicate: IgnoreOccupied))
            return;

        if (recipe.Conditions.Any(condition => !condition.Condition(user, coords, ev.Rotation.GetCardinalDir())))
        {
            return;
        }

        var mark = Spawn(ChalkMarkPrototype, coords);
        var comp = EnsureComp<ConstructionChalkMarkComponent>(mark);
        comp.ConstructionPrototype = ev.ConstructionPrototype;
        comp.Rotation = recipe.CanRotate ? ev.Rotation : Angle.Zero;
        Dirty(mark, comp);
        _transform.SetLocalRotation(mark, comp.Rotation);

        _audio.PlayPvs(chalkComp.PlaceSound, mark);
    }

    private void OnMarkInteractUsing(Entity<ConstructionChalkMarkComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryBuildFromMark(ent, args.User);
    }

    private async void TryBuildFromMark(Entity<ConstructionChalkMarkComponent> mark, EntityUid user)
    {
        if (mark.Comp.IsBuilding)
            return;

        if (mark.Comp.ConstructionPrototype is not { } protoId)
        {
            QueueDel(mark.Owner);
            return;
        }

        mark.Comp.IsBuilding = true;

        try
        {
            if (!_proto.TryIndex(protoId, out var recipe))
            {
                QueueDel(mark.Owner);
                return;
            }

            var coords = Transform(mark.Owner).Coordinates;
            var rotation = mark.Comp.Rotation;

            if (Deleted(mark.Owner))
                return;

            var structure = await _construction.TryStartStructureConstructionAt(user, recipe, coords, rotation);

            if (structure is not { Valid: true })
            {
                if (!Deleted(mark.Owner))
                    mark.Comp.IsBuilding = false;
                return;
            }

            if (!Deleted(mark.Owner))
                QueueDel(mark.Owner);
        }
        catch (Exception e)
        {
            Log.Error($"Error while building from chalk mark: {e}");
            if (!Deleted(mark.Owner))
                mark.Comp.IsBuilding = false;
        }
    }

    private bool TryResolveFamily(string constructionPrototype, out string familyRoot, out int layerIndex, ChalkMode mode)
    {
        familyRoot = constructionPrototype;
        layerIndex = 0;

        foreach (var category in _proto.EnumeratePrototypes<ChalkCategoryPrototype>())
        {
            if (category.Mode != mode)
                continue;

            foreach (var entry in category.Entries)
            {
                if (entry.ConstructionPrototype.Id == constructionPrototype)
                {
                    familyRoot = entry.ConstructionPrototype.Id;
                    layerIndex = 0;
                    return true;
                }

                if (!_proto.TryIndex<ConstructionPrototype>(entry.ConstructionPrototype.Id, out var recipe))
                    continue;

                for (var i = 0; i < recipe.AlternativePrototypes.Length; i++)
                {
                    if (recipe.AlternativePrototypes[i] != constructionPrototype)
                        continue;

                    familyRoot = entry.ConstructionPrototype.Id;
                    layerIndex = i;
                    return true;
                }
            }
        }

        return false;
    }

    private (string familyRoot, int layerIndex) ResolveFamilyUnscoped(string constructionPrototype)
    {
        foreach (var mode in new[] { ChalkMode.Construction, ChalkMode.Piping })
        {
            if (TryResolveFamily(constructionPrototype, out var familyRoot, out var layerIndex, mode))
                return (familyRoot, layerIndex);
        }

        return (constructionPrototype, 0);
    }

    private bool MarkAlreadyPresent(MapCoordinates mapPos, string familyRoot, int layerIndex)
    {
        foreach (var existing in _lookup.GetEntitiesInRange<ConstructionChalkMarkComponent>(mapPos, 0.1f))
        {
            if (existing.Comp.ConstructionPrototype is not { } existingProtoId)
                continue;

            var (existingFamily, existingLayer) = ResolveFamilyUnscoped(existingProtoId);

            if (existingFamily == familyRoot && existingLayer == layerIndex)
                return true;
        }

        return false;
    }
}
