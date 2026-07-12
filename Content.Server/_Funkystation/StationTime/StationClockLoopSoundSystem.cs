using Content.Shared._Funkystation.StationTime;
using Content.Shared._Funkystation.StationTime.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Funkystation.StationTime;

public sealed partial class StationClockLoopSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationClockComponent, StationClockToggledEvent>(OnToggled);
        SubscribeLocalEvent<StationClockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationClockComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, StationClockComponent comp, MapInitEvent args)
    {
        UpdateLoop(uid, comp);
    }

    private void OnToggled(EntityUid uid, StationClockComponent comp, ref StationClockToggledEvent args)
    {
        UpdateLoop(uid, comp);
    }

    private void OnShutdown(EntityUid uid, StationClockComponent comp, ComponentShutdown args)
    {
        if (comp.LoopSoundStream is { } stream)
            _audio.Stop(stream);
    }

    private void UpdateLoop(EntityUid uid, StationClockComponent comp)
    {
        if (comp.LoopSoundStream is { } existing && !Exists(existing))
            comp.LoopSoundStream = null;

        if (comp is { Enabled: true, LoopSound: not null })
        {
            comp.LoopSoundStream ??= _audio.PlayPvs(comp.LoopSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
        }
        else if (comp.LoopSoundStream is { } stream)
        {
            _audio.Stop(stream);
            comp.LoopSoundStream = null;
        }
    }
}
