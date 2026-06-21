using Robust.Client.Graphics;

namespace Content.Client._Funkystation.WallStains.Systems;

public sealed class WallStainOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = null!;

    private WallStainOverlay _overlay = null!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new WallStainOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<WallStainOverlay>();
    }
}
