using Robust.Shared.GameStates;

namespace Content.Shared._Funkystation.StationTime.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationTimeComponent : Component
{
    // server's DateTime.UtcNow.Ticks at the moment of sync
    [DataField, AutoNetworkedField]
    public long RealUtcTicksAtSync;

    // shared simulation clock (IGameTiming.CurTime) at that same moment
    [DataField, AutoNetworkedField]
    public TimeSpan CurTimeAtSync;
}
