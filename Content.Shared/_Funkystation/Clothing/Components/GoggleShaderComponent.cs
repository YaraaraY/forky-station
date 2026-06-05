using Robust.Shared.GameStates;

namespace Content.Shared._Funkystation.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GoggleShaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public string Shader = "Goggles";

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#5AB43CCC");
}
