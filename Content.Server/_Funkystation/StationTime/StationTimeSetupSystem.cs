using Content.Server.Station.Systems;
using Content.Shared._Funkystation.StationTime.Components;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.StationTime;

public sealed partial class StationTimeSetupSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    private void OnStationInitialized(StationInitializedEvent ev)
    {
        var comp = EnsureComp<StationTimeComponent>(ev.Station);
        comp.RealUtcTicksAtSync = DateTime.UtcNow.Ticks;
        comp.CurTimeAtSync = _timing.CurTime;
        Dirty(ev.Station, comp);
    }
}
