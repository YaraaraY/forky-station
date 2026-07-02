using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._Funkystation.Documents.Components;

/// <summary>
/// Marks an entity as a document printer. The set of documents available is per prototype.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DocumentPrinterComponent : Component
{
    /// <summary>
    /// Document prototypes this specific printer entity can print
    /// </summary>
    [DataField("availableDocuments", customTypeSerializer: typeof(PrototypeIdListSerializer<DocumentPrototype>))]
    public List<string> AvailableDocuments = new();

    /// <summary>
    /// Delay between pressing print and the paper appearing
    /// </summary>
    [DataField("printDelay")]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Minimum time between separate print jobs on this printer
    /// </summary>
    [DataField("printCooldown")]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The game time before which further print requests on this printer are rejected
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPrintTime;

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
