namespace Vence.App.Models;

public sealed record OutlineListItem(int Level, string Title)
{
    public string HeadingLabel => $"H{Level}";
}
