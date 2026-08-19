using Solar.Application.Reports;
using Solar.Infrastructure.Reports;
using Xunit;

namespace Solar.WebApi.Tests;

public class AcademicPdfReportTests
{
    [Fact]
    public void GenerateGradesReportPdf_ShouldReturnValidNonEmptyPdfBytes()
    {
        // Arrange
        var service = new AcademicPdfReportService();
        var model = new ClassGradesReportModel
        {
            CourseName = "Licenciatura em Química",
            CurriculumUnitName = "Química Geral I",
            CurriculumUnitCode = "QM101",
            SemesterName = "2026.1",
            ClassCode = "TURMA-01",
            TeacherName = "Prof. Fabrício Silva",
            WorkingHours = 64,
            Students = new List<StudentGradeEntry>
            {
                new()
                {
                    StudentId = 1,
                    StudentName = "Aluno Exemplo 1",
                    Cpf = "123.456.789-00",
                    PartialGrade = 8.5,
                    FinalGrade = 8.5,
                    FrequencyHours = 60,
                    AttendancePercentage = 93.7,
                    Situation = "Aprovado por Média"
                },
                new()
                {
                    StudentId = 2,
                    StudentName = "Aluno Exemplo 2",
                    Cpf = "987.654.321-99",
                    PartialGrade = 5.0,
                    FinalExamGrade = 7.0,
                    FinalGrade = 5.8,
                    FrequencyHours = 56,
                    AttendancePercentage = 87.5,
                    Situation = "Aprovado com AF"
                }
            }
        };

        // Act
        var pdfBytes = service.GenerateGradesReportPdf(model);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        // Valida cabeçalho mágico padrão de arquivo PDF (%PDF-)
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateAttendanceReportPdf_ShouldReturnValidPdfHeader()
    {
        // Arrange
        var service = new AcademicPdfReportService();
        var model = new ClassAttendanceReportModel
        {
            CurriculumUnitName = "Química Geral I",
            SemesterName = "2026.1",
            ClassCode = "TURMA-01",
            TeacherName = "Prof. Fabrício Silva",
            TotalHours = 64,
            Students = new List<StudentAttendanceEntry>
            {
                new()
                {
                    StudentId = 1,
                    StudentName = "Aluno 1",
                    AttendedHours = 60,
                    AttendancePercentage = 93.7,
                    Status = "Frequência Regular"
                }
            }
        };

        // Act
        var pdfBytes = service.GenerateAttendanceReportPdf(model);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }
}
