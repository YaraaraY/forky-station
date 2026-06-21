using Content.Client._Starfall.Particles;
using Content.Shared._Funkystation.ReagentFires;
using Content.Shared._Funkystation.WallStains.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Funkystation.WallStains.Systems;

public sealed class WallStainFireVisualsSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = null!;
    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private readonly Dictionary<EntityUid, (ActiveEmitter? Glow, ActiveEmitter? Fire, ActiveEmitter? Embers, ActiveEmitter? Slag, ActiveEmitter? Sparks, ActiveEmitter? Fumes)> _emitters = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WallStainFireVisualsComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<WallStainFireVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<WallStainFireVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnCompStartup(EntityUid uid, WallStainFireVisualsComponent component, ref ComponentStartup args)
    {
        UpdateVisuals(uid);
    }

    private void OnShutdown(EntityUid uid, WallStainFireVisualsComponent component, ref ComponentShutdown args)
    {
        if (_emitters.Remove(uid, out var pair))
        {
            _particles.RemoveParticle(pair.Fire);
            _particles.RemoveParticle(pair.Embers);
            _particles.RemoveParticle(pair.Slag);
            _particles.RemoveParticle(pair.Sparks);
            _particles.RemoveParticle(pair.Fumes);
        }
    }

    private void OnAppearanceChange(EntityUid uid, WallStainFireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        UpdateVisuals(uid);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        foreach (var (uid, pair) in _emitters)
        {
            if (Deleted(uid))
                continue;
            var coords = _transform.GetMapCoordinates(uid);
            if (pair.Fire is { Exhausted: false })
                pair.Fire.MapCoords = coords;
            if (pair.Embers is { Exhausted: false })
                pair.Embers.MapCoords = coords;
            if (pair.Slag is { Exhausted: false })
                pair.Slag.MapCoords = coords;
            if (pair.Sparks is { Exhausted: false })
                pair.Sparks.MapCoords = coords;
            if (pair.Fumes is { Exhausted: false })
                pair.Fumes.MapCoords = coords;
        }
    }

    private void UpdateVisuals(EntityUid uid)
    {
        if (!_emitters.TryGetValue(uid, out var pair))
            pair = (null, null, null, null, null, null);

        var coords = _transform.GetMapCoordinates(uid);
        var updated = false;

        if (pair.Fire == null || pair.Fire.Exhausted)
        {
            pair.Fire = _particles.SpawnEffect("WallFire", coords, uid);
            updated = true;
        }

        if (pair.Embers == null || pair.Embers.Exhausted)
        {
            pair.Embers = _particles.SpawnEffect("WallFireEmbers", coords, uid);
            updated = true;
        }

        if (pair.Slag == null || pair.Slag.Exhausted)
        {
            pair.Slag = _particles.SpawnEffect("WallFireSlag", coords, uid);
            updated = true;
        }

        if (pair.Sparks == null || pair.Sparks.Exhausted)
        {
            pair.Sparks = _particles.SpawnEffect("WallFireSparks", coords, uid);
            updated = true;
        }

        if (pair.Fumes == null || pair.Fumes.Exhausted)
        {
            pair.Fumes = _particles.SpawnEffect("WallFireFumes", coords, uid);
            updated = true;
        }

        if (updated)
            _emitters[uid] = pair;

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            if (_appearance.TryGetData<int>(uid, ReagentPuddleFireVisuals.FireState, out var fireState))
            {
                sprite.LayerSetState(0, fireState.ToString());

                var baseIntensity = fireState == 6 ? 2.0f : fireState == 5 ? 1.5f : 1.0f;
                if (pair.Fire != null)
                    pair.Fire.Intensity = baseIntensity;
                if (pair.Embers != null)
                    pair.Embers.Intensity = baseIntensity;

                var metalFireIntensity = fireState >= 5 ? baseIntensity : 0f;
                if (pair.Slag != null)
                    pair.Slag.Intensity = metalFireIntensity;
                if (pair.Sparks != null)
                    pair.Sparks.Intensity = metalFireIntensity;
                if (pair.Fumes != null)
                    pair.Fumes.Intensity = metalFireIntensity;
            }

            if (_appearance.TryGetData<Color>(uid, ReagentPuddleFireVisuals.FireColor, out var color))
            {
                sprite.Color = color;

                if (pair.Fire != null)
                    pair.Fire.ColorOverride = color;
                if (pair.Embers != null)
                    pair.Embers.ColorOverride = color;
                if (pair.Slag != null)
                    pair.Slag.ColorOverride = color;
                if (pair.Sparks != null)
                    pair.Sparks.ColorOverride = color;
                if (pair.Fumes != null)
                    pair.Fumes.ColorOverride = color.WithAlpha(0.35f);
            }
        }
    }
}
