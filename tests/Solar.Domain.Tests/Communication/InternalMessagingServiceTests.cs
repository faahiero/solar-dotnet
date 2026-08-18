using FluentAssertions;
using Solar.Domain.Communication;
using Xunit;

namespace Solar.Domain.Tests.Communication;

public class InternalMessagingServiceTests
{
    private readonly InternalMessagingService _messagingService = new();

    [Fact]
    public void DispatchMessage_Should_Create_Sent_Copy_And_Inbox_Copies_For_Recipients()
    {
        // Arrange
        long senderId = 1;
        var recipientIds = new List<long> { 2, 3, 4 };
        string subject = "Aviso importante de prova";
        string body = "Prezados alunos, a prova será realizada na próxima segunda-feira.";

        // Act
        var message = _messagingService.DispatchMessage(senderId, recipientIds, subject, body);

        // Assert
        message.Subject.Should().Be(subject);
        message.UserId.Should().Be(senderId);

        // Deve conter 1 registro no Sent (remetente) + 3 registros no Inbox (destinatários) = 4 no total
        message.UserMessages.Should().HaveCount(4);

        var sentCopy = message.UserMessages.First(m => m.UserId == senderId);
        sentCopy.Folder.Should().Be(InternalMessagingService.FolderSent);
        sentCopy.Read.Should().BeTrue();

        var inboxCopies = message.UserMessages.Where(m => m.UserId != senderId).ToList();
        inboxCopies.Should().HaveCount(3);
        inboxCopies.Should().AllSatisfy(m =>
        {
            m.Folder.Should().Be(InternalMessagingService.FolderInbox);
            m.Read.Should().BeFalse();
        });
    }
}
