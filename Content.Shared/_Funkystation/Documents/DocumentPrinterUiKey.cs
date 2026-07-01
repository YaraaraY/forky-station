using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Documents;

[Serializable, NetSerializable]
public enum DocumentPrinterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DocumentEntry(
    string id,
    string name,
    string description,
    bool accessible,
    List<string> requiredAccess)
{
    public readonly string Id = id;
    public readonly string Name = name;
    public readonly string Description = description;
    public readonly bool Accessible = accessible;
    public readonly List<string> RequiredAccess = requiredAccess;
}

[Serializable, NetSerializable]
public sealed class DocumentPrinterBoundUserInterfaceState(Dictionary<string, List<DocumentEntry>> documentsByCategory)
    : BoundUserInterfaceState
{
    public readonly Dictionary<string, List<DocumentEntry>> DocumentsByCategory = documentsByCategory;
}

[Serializable, NetSerializable]
public sealed class DocumentPrinterPrintMessage(string documentId) : BoundUserInterfaceMessage
{
    public readonly string DocumentId = documentId;
}
