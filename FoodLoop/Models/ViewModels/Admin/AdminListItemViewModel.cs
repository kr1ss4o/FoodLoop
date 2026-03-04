namespace FoodLoop.ViewModels.Admin;

public class AdminListItemViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string Subtitle { get; set; } = "";

    public string? Third { get; set; }

    public string? Fourth { get; set; }

    public string? Fifth { get; set; }
}