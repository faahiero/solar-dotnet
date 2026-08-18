using Solar.Domain.Entities;

namespace Solar.Domain.Communication;

/// <summary>
/// Serviço de correio eletrônico interno do Solar LMS.
/// Mapeado a partir de app/models/message.rb, user_message.rb e app/controllers/messages_controller.rb.
/// </summary>
public class InternalMessagingService
{
    public const int FolderInbox = 0;
    public const int FolderSent = 1;
    public const int FolderTrash = 2;

    /// <summary>
    /// Despacha uma nova mensagem para a caixa de saída do remetente e caixa de entrada dos destinatários.
    /// </summary>
    public InternalMessage DispatchMessage(
        long senderUserId,
        IEnumerable<long> recipientUserIds,
        string subject,
        string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var message = new InternalMessage
        {
            UserId = senderUserId,
            Subject = subject.Trim(),
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Cópia na pasta 'Enviados' (Sent) do remetente
        message.UserMessages.Add(new UserInternalMessage
        {
            UserId = senderUserId,
            Folder = FolderSent,
            Read = true
        });

        // Cópias na pasta 'Entrada' (Inbox) de cada destinatário
        foreach (var recipientId in recipientUserIds.Distinct().Where(id => id != senderUserId))
        {
            message.UserMessages.Add(new UserInternalMessage
            {
                UserId = recipientId,
                Folder = FolderInbox,
                Read = false
            });
        }

        return message;
    }
}
