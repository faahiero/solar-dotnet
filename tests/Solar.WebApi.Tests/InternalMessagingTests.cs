using Solar.Domain.Communication;
using Xunit;

namespace Solar.WebApi.Tests;

public class InternalMessagingTests
{
    [Fact]
    public void DispatchMessage_FromStudentToTeacher_ShouldCreateSenderAndRecipientCopies()
    {
        // Arrange
        var service = new InternalMessagingService();
        long studentId = 7;
        long teacherId = 6;
        string subject = "Dúvida sobre o Experimento 2 de Química";
        string body = "Professor, gostaria de saber se o prazo para o envio do relatório experimental do Módulo 2 pode ser estendido até sexta-feira.";

        // Act
        var msg = service.DispatchMessage(studentId, new[] { teacherId }, subject, body);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal(subject, msg.Subject);
        Assert.Equal(body, msg.Body);
        Assert.Equal(studentId, msg.UserId);

        // Verifica as 2 cópias geradas
        Assert.Equal(2, msg.UserMessages.Count);

        // Cópia do aluno remetente: Pasta Enviados (StatusSent = 3), Marcada como Lida (Read = true)
        var senderCopy = msg.UserMessages.FirstOrDefault(um => um.UserId == studentId);
        Assert.NotNull(senderCopy);
        Assert.Equal(InternalMessagingService.StatusSent, senderCopy.Status);
        Assert.True(senderCopy.Read);

        // Cópia do professor destinatário: Pasta Entrada (StatusInboxUnread = 0), Não Lida (Read = false)
        var recipientCopy = msg.UserMessages.FirstOrDefault(um => um.UserId == teacherId);
        Assert.NotNull(recipientCopy);
        Assert.Equal(InternalMessagingService.StatusInboxUnread, recipientCopy.Status);
        Assert.False(recipientCopy.Read);
    }
}
