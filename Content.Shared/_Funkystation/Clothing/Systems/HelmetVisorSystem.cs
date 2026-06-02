using Content.Shared.Actions;
using Content.Shared._Funkystation.Clothing.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Funkystation.Clothing.Systems;

public sealed class HardsuitVisorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = null!;
    [Dependency] private readonly SharedItemSystem _item = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = null!;
    [Dependency] private readonly INetManager _net = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HardsuitVisorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HardsuitVisorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HardsuitVisorComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HardsuitVisorComponent, ToggleHardsuitVisorEvent>(OnToggle);
        SubscribeLocalEvent<HardsuitVisorComponent, GetEquipmentVisualsEvent>(OnGetVisuals, after: [typeof(ClothingSystem)]);
    }

    private void OnMapInit(Entity<HardsuitVisorComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        UpdateAppearance(ent);

        if (_net.IsServer && !ent.Comp.IsActive)
            EntityManager.AddComponents(ent.Owner, ent.Comp.Components);
    }

    private void OnShutdown(Entity<HardsuitVisorComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnGetActions(Entity<HardsuitVisorComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ActionEntity != null)
            args.AddAction(ent.Comp.ActionEntity.Value);
    }

    private void OnToggle(Entity<HardsuitVisorComponent> ent, ref ToggleHardsuitVisorEvent args)
    {
        args.Handled = true;

        ent.Comp.IsActive = !ent.Comp.IsActive;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);

        UpdateAppearance(ent);
        _item.VisualsChanged(ent.Owner);

        if (_net.IsServer)
        {
            if (!ent.Comp.IsActive)
                EntityManager.AddComponents(ent.Owner, ent.Comp.Components);
            else
                EntityManager.RemoveComponents(ent.Owner, ent.Comp.Components);
        }
    }

    private void OnGetVisuals(Entity<HardsuitVisorComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        var state = ent.Comp.IsActive ? ent.Comp.StateUp : ent.Comp.StateDown;

        if (string.IsNullOrEmpty(state))
            return;

        if (!ent.Comp.IsActive)
        {
            var baseState = "equipped-" + args.Slot;

            foreach (var (key, layerData) in args.Layers)
            {
                if (key.StartsWith(args.Slot) && !string.IsNullOrEmpty(layerData.State))
                {
                    if (layerData.State.StartsWith(baseState))
                    {
                        var suffix = layerData.State.Substring(baseState.Length);

                        if (suffix.Contains("-unshaded"))
                            continue;

                        state += suffix;
                        break;
                    }
                }
            }
        }

        var layer = new PrototypeLayerData()
        {
            State = state,
            Visible = true
        };

        var insertIndex = args.Layers.Count;
        for (var i = 0; i < args.Layers.Count; i++)
        {
            if (args.Layers[i].Item1 == "light")
            {
                insertIndex = i;
                break;
            }
        }

        args.Layers.Insert(insertIndex, (ent.Comp.VisualLayer, layer));
    }

    private void UpdateAppearance(Entity<HardsuitVisorComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            _appearance.SetData(ent, HardsuitVisorVisuals.IsDown, !ent.Comp.IsActive, appearance);
        }
    }
}
