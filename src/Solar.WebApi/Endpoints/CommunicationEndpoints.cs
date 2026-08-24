using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Caching;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Hubs;

namespace Solar.WebApi.Endpoints;

public record SendMessageRequest(string? Recipient, string Subject, string Body, long? SenderId = null, long? RecipientId = null, List<long>? RecipientIds = null, List<string>? Attachments = null);
public record UpdateMessageStatusRequest(List<long> MessageIds, string NewStatus, long? UserId = null);

public static class CommunicationEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        // Correio Eletrônico Interno (Espelha 03_mensagens_correio.png)
        app.MapGet("/api/v1/messages", async (
            string? folder,
            long? userId,
            string? filter,
            string? subject,
            string? user,
            SolarDbContext db) =>
        {
            var folderTarget = folder?.ToLower() ?? "inbox";
            long currentUserId = userId ?? 7; // Aluno 1 por padrão

            try
            {
                int targetStatus = folderTarget switch
                {
                    "outbox" or "sent" => 3,
                    "trash" => 7,
                    _ => 0
                };

                var query = db.UserInternalMessages
                    .Include(um => um.Message)
                    .Where(um => um.UserId == currentUserId);

                if (folderTarget == "inbox")
                {
                    if (filter == "unread") query = query.Where(um => um.Status == 0);
                    else if (filter == "read") query = query.Where(um => um.Status == 1);
                    else query = query.Where(um => um.Status == 0 || um.Status == 1);
                }
                else
                {
                    query = query.Where(um => um.Status == targetStatus);
                }

                if (!string.IsNullOrWhiteSpace(subject))
                {
                    var s = subject.Trim().ToLower();
                    query = query.Where(um => um.Message != null && um.Message.Subject.ToLower().Contains(s));
                }

                var dbMessages = await query
                    .OrderByDescending(um => um.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                int unreadCount = await db.UserInternalMessages
                    .CountAsync(um => um.UserId == currentUserId && um.Status == 0);

                var allUsers = await db.Users.ToDictionaryAsync(u => u.Id, u => u.Name ?? u.Username);

                var list = dbMessages.Select(um =>
                {
                    var otherUserMsg = db.UserInternalMessages
                        .FirstOrDefault(o => o.MessageId == um.MessageId && o.UserId != currentUserId);
                    string otherUserName = otherUserMsg != null && allUsers.TryGetValue(otherUserMsg.UserId, out var n)
                        ? n
                        : (folderTarget == "outbox" ? "Professor Titular" : "Professor Titular");

                    string currentUserName = allUsers.TryGetValue(currentUserId, out var cName) ? cName : "Você";

                    return new
                    {
                        Id = (int)um.Id,
                        MessageId = (int)um.MessageId,
                        Subject = um.Message?.Subject ?? "Sem Assunto",
                        Sender = folderTarget == "outbox" ? currentUserName : otherUserName,
                        Recipient = folderTarget == "outbox" ? otherUserName : currentUserName,
                        Date = um.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        Read = um.Status != 0,
                        Status = um.Status,
                        Body = um.Message?.Body ?? ""
                    };
                }).ToList();

                if (list.Any())
                {
                    return Results.Ok(new
                    {
                        UnreadCount = unreadCount,
                        Messages = list
                    });
                }
            }
            catch { }

            return Results.Ok(new
            {
                UnreadCount = 0,
                Messages = Array.Empty<object>()
            });
        })
        .WithName("GetMessages")
        .WithSummary("Retorna mensagens do correio interno com contagem de não lidas e filtros");

        // Alteração de Status de Mensagens em Lote (Lida, Não Lida, Lixeira, Restaurar)
        app.MapPut("/api/v1/messages/status", async (UpdateMessageStatusRequest req, SolarDbContext db) =>
        {
            if (req.MessageIds == null || !req.MessageIds.Any())
            {
                return Results.BadRequest(new { success = false, message = "Nenhuma mensagem especificada." });
            }

            int newStatus = req.NewStatus?.ToLower() switch
            {
                "read" => 1,
                "unread" => 0,
                "trash" => 7,
                "restore" => 0,
                _ => 1
            };

            var userMessages = await db.UserInternalMessages
                .Where(um => req.MessageIds.Contains(um.Id))
                .ToListAsync();

            foreach (var um in userMessages)
            {
                um.Status = newStatus;
                um.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                updatedCount = userMessages.Count,
                newStatus = req.NewStatus,
                message = $"Status de {userMessages.Count} mensagem(ns) atualizado com sucesso!"
            });
        })
        .WithName("UpdateMessageStatus")
        .WithSummary("Altera o status de mensagens (lida, não lida, lixeira, restaurar) em lote");

        // Envio de Mensagem Direta
        app.MapPost("/api/v1/messages", async (SendMessageRequest req, SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Body))
            {
                return Results.BadRequest(new { success = false, message = "Assunto e conteúdo da mensagem são obrigatórios." });
            }

            long senderId = req.SenderId is > 0 ? req.SenderId.Value : 7; // Aluno 1 padrão

            var recipientList = new List<long>();
            if (req.RecipientIds != null && req.RecipientIds.Any())
            {
                recipientList.AddRange(req.RecipientIds.Where(id => id > 0 && id != senderId));
            }
            else if (req.RecipientId is > 0)
            {
                recipientList.Add(req.RecipientId.Value);
            }
            else
            {
                recipientList.Add(6); // Professor padrão
            }

            var message = new InternalMessage
            {
                Subject = req.Subject.Trim(),
                Body = req.Body.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.InternalMessages.Add(message);
            await db.SaveChangesAsync();

            // 1. Cópia na pasta 'Enviados' do remetente (status = 3)
            db.UserInternalMessages.Add(new UserInternalMessage
            {
                MessageId = message.Id,
                UserId = senderId,
                Status = 3, // Sent
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // 2. Cópia na pasta 'Entrada' de cada destinatário (status = 0 - Unread Inbox)
            foreach (var recId in recipientList.Distinct())
            {
                db.UserInternalMessages.Add(new UserInternalMessage
                {
                    MessageId = message.Id,
                    UserId = recId,
                    Status = 0, // Unread Inbox
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                messageId = message.Id,
                senderId = senderId,
                recipientCount = recipientList.Count,
                subject = message.Subject,
                body = message.Body,
                sentAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                message = "Mensagem transmitida com sucesso para o(s) destinatário(s)!"
            });
        })
        .WithName("SendMessage")
        .WithSummary("Envia uma nova mensagem no correio interno");

        // Catálogo / Seleção de Contatos para o Modal de Mensagens
        app.MapGet("/api/v1/messages/contacts", async (
            int? contactsType,
            int? roleType,
            long? userId,
            long? curriculumUnitId,
            string? course,
            string? discipline,
            string? semester,
            string? search,
            SolarDbContext db,
            ISolarCacheService cache) =>
        {
            var cacheKey = $"contacts_{contactsType}_{roleType}_{userId}_{curriculumUnitId}_{search?.Trim().ToLower()}";
            return await cache.GetOrCreateAsync(cacheKey, async () =>
            {
                long currentUserId = userId ?? 0;
                var usersQuery = db.Users.AsQueryable();

                if (contactsType == 2)
                {
                    var myDirectContactIds = new HashSet<long> { 7, 8, 9, 6, 5, 10, 11, 12 };
                    usersQuery = usersQuery.Where(u => myDirectContactIds.Contains(u.Id));
                    if (currentUserId > 0)
                    {
                        usersQuery = usersQuery.Where(u => u.Id != currentUserId);
                    }
                }
                else
                {
                    if (currentUserId > 0)
                    {
                        usersQuery = usersQuery.Where(u => u.Id != currentUserId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    usersQuery = usersQuery.Where(u => (u.Name != null && u.Name.ToLower().Contains(s)) || u.Username.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
                }

                var users = await usersQuery
                    .OrderBy(u => u.Name)
                    .Take(50)
                    .ToListAsync();

                var userIds = users.Select(u => u.Id).ToList();

                var userAllocations = await db.Allocations
                    .Where(a => userIds.Contains(a.UserId) && a.ProfileId != 12)
                    .Include(a => a.Profile)
                    .ToListAsync();

                var profileMap = userAllocations
                    .GroupBy(a => a.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => a.Profile).FirstOrDefault(p => p != null)
                    );

                var contacts = users.Select(u =>
                {
                    profileMap.TryGetValue(u.Id, out var prof);

                    string profileName = prof?.Name ?? "Aluno";
                    int profileTypes = (int?)prof?.Types ?? 4;

                    string roleName;
                    int typeMask;

                    if (profileTypes == 16 || profileName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        roleName = "Administrador";
                        typeMask = 16;
                    }
                    else if (profileTypes == 8 || profileName.Contains("Editor", StringComparison.OrdinalIgnoreCase))
                    {
                        roleName = "Editor / Coordenador";
                        typeMask = 8;
                    }
                    else if (profileName.Contains("Tutor Presencial", StringComparison.OrdinalIgnoreCase))
                    {
                        roleName = "Tutor Presencial";
                        typeMask = 32;
                    }
                    else if (profileName.Contains("Tutor", StringComparison.OrdinalIgnoreCase))
                    {
                        roleName = "Tutor a Distância";
                        typeMask = 2;
                    }
                    else if (profileTypes == 2 || profileName.Contains("Prof", StringComparison.OrdinalIgnoreCase))
                    {
                        roleName = "Docente / Professor";
                        typeMask = 4;
                    }
                    else
                    {
                        roleName = "Aluno";
                        typeMask = 1;
                    }

                    return new
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email ?? $"{u.Username}@solar.ufc.br",
                        Username = u.Username,
                        Role = roleName,
                        TypeMask = typeMask,
                        Resume = $"{u.Name} <{u.Email ?? u.Username + "@solar.ufc.br"}> ({roleName})"
                    };
                }).ToList();

                if (roleType.HasValue && roleType.Value > 0)
                {
                    contacts = contacts.Where(c => c.TypeMask == roleType.Value).ToList();
                }

                return Results.Ok(contacts);
            }, slidingExpiration: TimeSpan.FromMinutes(2));
        })
        .WithName("GetMessageContacts")
        .WithSummary("Retorna os contatos do sistema com filtros por papel e disciplina para o modal de seleção");

        // Mapeamento do Hub SignalR de Chat
        app.MapHub<ChatHub>("/hubs/chat");

        return app;
    }
}
