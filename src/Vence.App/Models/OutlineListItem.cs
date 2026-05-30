namespace Vence.App.Models;

public sealed record OutlineListItem(int Level, string Title, int Index)
{
    public string HeadingLabel => $"H{Level}";
}
