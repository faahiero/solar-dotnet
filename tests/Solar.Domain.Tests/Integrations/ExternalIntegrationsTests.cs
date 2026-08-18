using FluentAssertions;
using Solar.Infrastructure.Integrations.BigBlueButton;
using Solar.Infrastructure.Integrations.Sigaa;
using Xunit;

namespace Solar.Domain.Tests.Integrations;

public class ExternalIntegrationsTests
{
    [Fact]
    public void BigBlueButtonClient_Should_Compute_Valid_Sha1_Checksum()
    {
        // Arrange (Teste de vetor conhecido com salt e parâmetros)
        string callName = "create";
        string queryString = "meetingID=sala101&name=Aula+Inaugural";
        string secret = "solar_secret_bbb_123";

        // Act
        string checksum = BigBlueButtonClient.ComputeChecksum(callName, queryString, secret);

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        checksum.Length.Should().Be(40); // SHA-1 hex
    }

    [Fact]
    public void BuildCreateMeetingUrl_Should_Generate_Complete_Url_With_Checksum()
    {
        // Arrange
        var config = new BigBlueButtonServerConfig
        {
            ServerUrl = "https://bbb.virtual.ufc.br/bigbluebutton",
            SharedSecret = "secret123"
        };
        var client = new BigBlueButtonClient(config);

        var request = new CreateMeetingRequest
        {
            MeetingId = "turma_calc1_g1",
            MeetingName = "Cálculo 1 - Turma 01",
            ModeratorPassword = "mod_password",
            AttendeePassword = "att_password",
            Record = true
        };

        // Act
        string url = client.BuildCreateMeetingUrl(request);

        // Assert
        url.Should().StartWith("https://bbb.virtual.ufc.br/bigbluebutton/api/create?");
        url.Should().Contain("meetingID=turma_calc1_g1");
        url.Should().Contain("record=true");
        url.Should().Contain("&checksum=");
    }

    [Fact]
    public async Task SigaaAcademicClient_Should_Find_User_By_Cpf()
    {
        // Arrange
        var client = new SigaaAcademicClient();

        // Act
        var user = await client.FindUserByCpfAsync("123.456.789-00");

        // Assert
        user.Should().NotBeNull();
        user!.Cpf.Should().Be("12345678900");
        user.Institution.Should().Contain("UFC");
    }
}
