using Microsoft.UI.Xaml;

namespace Vence.App.Models;

public sealed record DocumentListItem(
    string Title,
    string Kind,
    string Path,
    bool IsSelected = false,
    bool IsFolder = false,
    int Depth = 0)
{
    public Thickness TreeMargin => new(Depth * 14, 0, 0, 0);

    public string TreeGlyph => IsFolder ? "\uE8B7" : "\uE8A5";
}
