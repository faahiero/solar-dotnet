using Solar.Domain.Entities;

namespace Solar.Domain.Discussions;

public record DiscussionTreeNode
{
    public DiscussionPost Post { get; init; } = default!;
    public IReadOnlyList<DiscussionTreeNode> Replies { get; init; } = [];
}

/// <summary>
/// Serviço de domínio para gestão da árvore hierárquica de postagens em Fóruns de Discussão.
/// Mapeado a partir de app/models/discussion.rb e app/models/post.rb.
/// </summary>
public class DiscussionTreeService
{
    public const int ResponsibleExtraDays = 7;

    /// <summary>
    /// Calcula o nível hierárquico do novo post, respeitando o limite máximo de 7 níveis (Discussion_Post_Max_Indent_Level).
    /// </summary>
    public int CalculatePostLevel(DiscussionPost? parent)
    {
        if (parent == null)
        {
            return 1; // Post raiz
        }

        // Se o pai já estiver no nível 7, a resposta permanece no nível 7
        return Math.Min(parent.Level + 1, DiscussionPost.MaxIndentLevel);
    }

    /// <summary>
    /// Valida se um post pode ser excluído. Não é permitida a exclusão de post que possui respostas publicadas.
    /// </summary>
    public bool CanDeletePost(DiscussionPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        // Se tiver filhos que não sejam rascunhos, não pode deletar
        bool hasPublishedChildren = post.Children.Any(c => !c.Draft);
        return !hasPublishedChildren;
    }

    /// <summary>
    /// Constrói a estrutura em árvore a partir de uma lista plana de posts.
    /// </summary>
    public IReadOnlyList<DiscussionTreeNode> BuildTree(IEnumerable<DiscussionPost> posts)
    {
        ArgumentNullException.ThrowIfNull(posts);

        var postsList = posts.ToList();
        var lookup = postsList.ToLookup(p => p.ParentId);

        return BuildNodes(null, lookup);
    }

    private static List<DiscussionTreeNode> BuildNodes(long? parentId, ILookup<long?, DiscussionPost> lookup)
    {
        var result = new List<DiscussionTreeNode>();

        foreach (var post in lookup[parentId].OrderBy(p => p.CreatedAt))
        {
            var replies = BuildNodes(post.Id, lookup);
            result.Add(new DiscussionTreeNode
            {
                Post = post,
                Replies = replies
            });
        }

        return result;
    }

    /// <summary>
    /// Valida se o usuário pode interagir no fórum considerando a janela de datas e o prazo extra do professor.
    /// </summary>
    public bool CanUserInteract(
        Discussion discussion,
        bool isResponsible,
        DateOnly currentDate)
    {
        ArgumentNullException.ThrowIfNull(discussion);

        if (discussion.Schedule == null || !discussion.Schedule.StartDate.HasValue)
        {
            return false;
        }

        var startDate = discussion.Schedule.StartDate.Value;
        if (currentDate < startDate)
        {
            return false; // Ainda não abriu
        }

        if (!discussion.Schedule.EndDate.HasValue)
        {
            return true; // Aberto sem data de término
        }

        var endDate = discussion.Schedule.EndDate.Value;
        if (currentDate <= endDate)
        {
            return true; // Dentro do período regular
        }

        // Se passou do prazo final, apenas professores/responsáveis possuem prazo extra (+7 dias)
        if (isResponsible)
        {
            var extraLimit = endDate.AddDays(ResponsibleExtraDays);
            return currentDate <= extraLimit;
        }

        return false;
    }
}
