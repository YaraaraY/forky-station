using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared._Funkystation.Documents;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Shared._Funkystation.Documents.Components.DocumentPrinterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<Shared._Funkystation.Documents.Components.DocumentPrinterComponent, DocumentPrinterPrintMessage>(OnPrintRequested);
    }

    private void OnUiOpened(EntityUid uid, Shared._Funkystation.Documents.Components.DocumentPrinterComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, comp, args.Actor);
    }

    /// <summary>
    /// Rebuilds and pushes the full document list for this printer
    /// </summary>
    private void UpdateUiState(EntityUid uid, Shared._Funkystation.Documents.Components.DocumentPrinterComponent comp, EntityUid actor)
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

    private void OnPrintRequested(EntityUid uid, Shared._Funkystation.Documents.Components.DocumentPrinterComponent comp, DocumentPrinterPrintMessage msg)
    {
        var actor = msg.Actor;

        if (!comp.AvailableDocuments.Contains(msg.DocumentId))
            return;

        if (!_proto.TryIndex<DocumentPrototype>(msg.DocumentId, out var doc))
            return;

        if (!IsDocAccessible(actor, doc))
            return;

        var coords = Transform(uid).Coordinates;
        var paper = Spawn(doc.PaperPrototype, coords);

        _paper.SetContent(paper, Loc.GetString(doc.Content));
        _audio.PlayPvs(comp.PrintSound, uid);

        UpdateUiState(uid, comp, actor);
    }
}
