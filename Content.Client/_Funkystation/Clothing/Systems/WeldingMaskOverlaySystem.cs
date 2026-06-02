using Content.Shared._Funkystation.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;

namespace Content.Client._Funkystation.Clothing.Systems;

public sealed class WeldingMaskOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = null!;
    [Dependency] private readonly IOverlayManager _overlayMan = null!;
    [Dependency] private readonly IResourceCache _cache = null!;
    [Dependency] private readonly InventorySystem _inventory = null!;

    private WeldingMaskOverlay _overlay = null!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new WeldingMaskOverlay(_cache);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var localPlayer = _player.LocalSession?.AttachedEntity;

        if (localPlayer == null || !_inventory.TryGetSlotEntity(localPlayer.Value, "head", out var headItem) )
        {
            RemoveOverlay();
            return;
        }

        if (TryComp<WeldingMaskOverlayComponent>(headItem.Value, out var comp))
        {
            _overlay.CurrentTexturePath = comp.Texture;
            AddOverlay();
        }
        else
        {
            RemoveOverlay();
        }
    }

    private void AddOverlay()
    {
        if (!_overlayMan.HasOverlay<WeldingMaskOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlayMan.HasOverlay<WeldingMaskOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
    }
}
