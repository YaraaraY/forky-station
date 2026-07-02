using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared._Funkystation.Documents;
using Content.Shared._Funkystation.Documents.Components;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.Documents;

/// <summary>
/// Handles document printer UI state and printing
/// </summary>
public sealed partial class DocumentPrinterSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = null!;
    [Dependency] private PaperSystem _paper = null!;
    [Dependency] private AccessReaderSystem _accessReader = null!;
    [Dependency] private UserInterfaceSystem _ui = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedAppearanceSystem _appearance = null!;
    [Dependency] private IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DocumentPrinterComponent, DocumentPrinterPrintMessage>(OnPrintRequested);
    }

    private void OnUiOpened(EntityUid uid, DocumentPrinterComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, comp, args.Actor);
    }

    /// <summary>
    /// Rebuilds and pushes the full document list for this printer
    /// </summary>
    private void UpdateUiState(EntityUid uid, DocumentPrinterComponent comp, EntityUid actor)
    {
        var grouped = new Dictionary<string, List<DocumentEntry>>();

        foreach (var docId in comp.AvailableDocuments)
        {
            if (!_proto.TryIndex<DocumentPrototype>(docId, out var doc))
                continue;

            var accessible = IsDocAccessible(actor, doc);

            if (!grouped.TryGetValue(doc.Category, out var list))
                grouped[doc.Category] = list = new List<DocumentEntry>();

            list.Add(new DocumentEntry(
                doc.ID,
                Loc.GetString(doc.Name),
                Loc.GetString(doc.Description),
                accessible,
                doc.RequiredAccess ?? new List<string>()));
        }

        _ui.SetUiState(uid, DocumentPrinterUiKey.Key, new DocumentPrinterBoundUserInterfaceState(grouped));
    }

    /// <summary>
    /// Checks a document's RequiredAccess against the player's access
    /// </summary>
    private bool IsDocAccessible(EntityUid actor, DocumentPrototype doc)
    {
        if (doc.RequiredAccess is not { Count: > 0 })
            return true;

        var tags = _accessReader.FindAccessTags(actor);
        return doc.RequiredAccess.Any(s => tags.Contains(s));
    }

    private void OnPrintRequested(EntityUid uid, DocumentPrinterComponent comp, DocumentPrinterPrintMessage msg)
    {
        var actor = msg.Actor;

        if (_timing.CurTime < comp.NextPrintTime)
            return;

        if (!comp.AvailableDocuments.Contains(msg.DocumentId))
            return;

        if (!_proto.TryIndex<DocumentPrototype>(msg.DocumentId, out var doc))
            return;

        if (!IsDocAccessible(actor, doc))
            return;

        comp.NextPrintTime = _timing.CurTime + comp.PrintCooldown;

        _appearance.SetData(uid, DocumentPrinterVisuals.VisualState, DocumentPrinterVisualState.Printing);
        _audio.PlayPvs(comp.PrintSound, uid);

        var coords = Transform(uid).Coordinates;
        var printDelay = comp.PrintDelay;

        Timer.Spawn(printDelay,
            () =>
        {
            if (!Exists(uid))
                return;

            var paper = Spawn(doc.PaperPrototype, coords);
            _paper.SetContent(paper, Loc.GetString(doc.Content));

            _appearance.SetData(uid, DocumentPrinterVisuals.VisualState, DocumentPrinterVisualState.Normal);
        });

        UpdateUiState(uid, comp, actor);
    }
}
