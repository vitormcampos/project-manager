using MudBlazor;

namespace ProjectHub.Web.Models;

public sealed record ProjectItem(
    string Name,
    string Description,
    int Progress,
    string Status,
    string Due,
    string Icon,
    Color Color,
    string? ImageDataUrl = null
);

public static class ProjectCatalog
{
    private static readonly List<ProjectItem> Projects =
    [
        new(
            "Redesign da plataforma",
            "Atualização da experiência principal do produto",
            68,
            "Em andamento",
            "30 jun",
            Icons.Material.Filled.AutoAwesome,
            Color.Primary
        ),
        new(
            "Aplicativo mobile",
            "Lançamento da experiência para iOS e Android",
            42,
            "Em andamento",
            "12 jul",
            Icons.Material.Filled.PhoneIphone,
            Color.Info
        ),
        new(
            "Portal do cliente",
            "Autoatendimento e área de suporte",
            85,
            "Quase concluído",
            "28 jun",
            Icons.Material.Filled.Dashboard,
            Color.Warning
        ),
    ];

    private static readonly IReadOnlyList<ProjectItem> ReadOnlyProjects = Projects.AsReadOnly();

    public static IReadOnlyList<ProjectItem> All => ReadOnlyProjects;
    public static event Action? Changed;

    public static void Add(ProjectItem project)
    {
        Projects.Add(project);
        Changed?.Invoke();
    }
}
