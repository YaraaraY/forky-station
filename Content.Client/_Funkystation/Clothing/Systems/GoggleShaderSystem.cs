using Content.Shared._Funkystation.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Funkystation.Clothing.Systems;

public sealed class GoggleShaderSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly InventorySystem _inventory = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;

    private GoggleShaderOverlay _overlay = null!;
    private bool _overlayAdded;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new GoggleShaderOverlay(_prototype);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            ClearOverlay();
            return;
        }

        _overlay.ActiveShaders.Clear();

        if (_inventory.TryGetSlotEntity(player.Value, "eyes", out var eyesEnt) &&
            TryComp<GoggleShaderComponent>(eyesEnt, out var eyesGoggles) &&
            eyesGoggles.Enabled)
        {
            _overlay.ActiveShaders.Add((eyesGoggles.Shader, eyesGoggles.Color));
        }

        if (_inventory.TryGetSlotEntity(player.Value, "head", out var headEnt) &&
            TryComp<GoggleShaderComponent>(headEnt, out var headGoggles) &&
            headGoggles.Enabled)
        {
            _overlay.ActiveShaders.Add((headGoggles.Shader, headGoggles.Color));
        }

        if (_overlay.ActiveShaders.Count > 0)
        {
            if (!_overlayAdded)
            {
                _overlayMan.AddOverlay(_overlay);
                _overlayAdded = true;
            }
        }
        else
        {
            ClearOverlay();
        }
    }

    private void ClearOverlay()
    {
        _overlay.ActiveShaders.Clear();
        if (_overlayAdded)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlayAdded = false;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ClearOverlay();
    }
}
