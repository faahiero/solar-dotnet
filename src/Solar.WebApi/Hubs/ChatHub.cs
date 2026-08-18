using Microsoft.AspNetCore.SignalR;

namespace Solar.WebApi.Hubs;

/// <summary>
/// Hub SignalR de tempo real para o Solar LMS.
/// Substitui o servidor independente EventMachine WebSockets (lib/websockets/websocket_server.rb).
/// Fornece agrupamento nativo por salas/turmas, reconexão automática e escalabilidade horizontal via Redis Backplane.
/// </summary>
public class ChatHub : Hub
{
    public async Task JoinRoom(string roomId)
    {
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoined", new
            {
                ConnectionId = Context.ConnectionId,
                RoomId = roomId,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", new
            {
                ConnectionId = Context.ConnectionId,
                RoomId = roomId,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task SendMessage(string roomId, string senderName, string message)
    {
        if (!string.IsNullOrWhiteSpace(roomId) && !string.IsNullOrWhiteSpace(message))
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                RoomId = roomId,
                SenderName = senderName,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task NotifyTyping(string roomId, string userName)
    {
        if (!string.IsNullOrWhiteSpace(roomId) && !string.IsNullOrWhiteSpace(userName))
        {
            await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new
            {
                RoomId = roomId,
                UserName = userName
            });
        }
    }
}
