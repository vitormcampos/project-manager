namespace ProjectHub.Web.Models;

public sealed record TaskItem(
    int Id,
    string Title,
    string Project,
    DateTime? DueDate,
    int Priority,
    bool IsCompleted,
    IReadOnlyList<string> Tags,
    string Description
);

public static class TaskCatalog
{
    private static readonly List<TaskItem> Tasks =
    [
        new(1, "Revisar os fluxos de onboarding", "Redesign da plataforma", DateTime.Today, 1, false, ["UX", "Onboarding"],
            "# Fluxo atual\n\nRevisar os passos do onboarding e anotar os pontos de fricção."),
        new(2, "Preparar pauta da reunião de produto", "Aplicativo mobile", DateTime.Today, 2, false, ["Reunião"], ""),
        new(3, "Validar protótipo da tela inicial", "Redesign da plataforma", DateTime.Today, 2, false, ["Design", "Revisão"], ""),
        new(4, "Definir escopo da próxima versão", "Portal do cliente", DateTime.Today.AddDays(1), 3, false, ["Planejamento"], ""),
        new(5, "Revisar documentação da API", "Aplicativo mobile", DateTime.Today.AddDays(2), 1, false, ["Documentação", "API"], ""),
        new(6, "Enviar relatório de pesquisa", "Redesign da plataforma", DateTime.Today.AddDays(-1), 3, true, ["Pesquisa"], ""),
    ];

    private static int _nextId = 7;

    public static IReadOnlyList<TaskItem> All => Tasks;
    public static event Action? Changed;

    public static void Add(string title, string project, DateTime? dueDate, IReadOnlyList<string> tags)
    {
        Tasks.Insert(0, new TaskItem(_nextId++, title, project, dueDate?.Date, 4, false, tags, ""));
        Changed?.Invoke();
    }

    public static void Update(TaskItem updatedTask)
    {
        var index = Tasks.FindIndex(task => task.Id == updatedTask.Id);
        if (index < 0)
            return;

        Tasks[index] = updatedTask with { DueDate = updatedTask.DueDate?.Date };
        Changed?.Invoke();
    }

    public static void Toggle(int id)
    {
        var index = Tasks.FindIndex(task => task.Id == id);
        if (index < 0)
            return;

        Tasks[index] = Tasks[index] with { IsCompleted = !Tasks[index].IsCompleted };
        Changed?.Invoke();
    }
}
