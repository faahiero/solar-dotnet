using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Solar.Application.Reports;

namespace Solar.Infrastructure.Reports;

public class AcademicPdfReportService : IAcademicReportService
{
    static AcademicPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateGradesReportPdf(ClassGradesReportModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("UNIVERSIDADE FEDERAL DO CEARÁ - UFC VIRTUAL").Bold().FontSize(14).FontColor("#003E7A");
                            c.Item().Text("SOLAR LMS 2.0 - DIÁRIO ELETRÔNICO / PAUTA OFICIAL DE NOTAS").Bold().FontSize(11).FontColor("#204882");
                        });
                        row.ConstantItem(150).AlignRight().Text($"Emitido em: {model.EmissionDate:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#666666");
                    });

                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#CCCCCC");

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Disciplina: {model.CurriculumUnitCode} - {model.CurriculumUnitName}").Bold();
                        row.RelativeItem().Text($"Curso: {model.CourseName}");
                        row.RelativeItem().Text($"Semestre: {model.SemesterName} | Turma: {model.ClassCode}");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Docente Responsável: {model.TeacherName}");
                        row.RelativeItem().Text($"Carga Horária: {model.WorkingHours} h/a");
                        row.RelativeItem().Text($"Total de Alunos: {model.Students.Count}");
                    });

                    col.Item().PaddingBottom(8).LineHorizontal(1).LineColor("#003E7A");
                });

                // Content / Table
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // Nº
                        columns.RelativeColumn(3);   // Nome do Aluno
                        columns.ConstantColumn(90);  // CPF
                        columns.ConstantColumn(60);  // M. Parcial
                        columns.ConstantColumn(50);  // Nota AF
                        columns.ConstantColumn(60);  // M. Final
                        columns.ConstantColumn(60);  // Freq (h)
                        columns.ConstantColumn(50);  // Freq (%)
                        columns.RelativeColumn(2);   // Situação
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#003E7A").Padding(4).Text("Nº").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("Aluno").FontColor("#FFFFFF").Bold();
                        header.Cell().Background("#003E7A").Padding(4).Text("CPF").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("M. Parcial").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("AF").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("M. Final").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("Horas").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("% Freq").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("Situação").FontColor("#FFFFFF").Bold();
                    });

                    // Rows
                    int index = 1;
                    foreach (var student in model.Students)
                    {
                        var bg = index % 2 == 0 ? "#F8F9FA" : "#FFFFFF";

                        table.Cell().Background(bg).Padding(4).Text(index.ToString()).AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.StudentName);
                        table.Cell().Background(bg).Padding(4).Text(student.Cpf).AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.PartialGrade.ToString("F1")).AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.FinalExamGrade.HasValue ? student.FinalExamGrade.Value.ToString("F1") : "-").AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.FinalGrade.ToString("F1")).Bold().AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text($"{student.FrequencyHours}h").AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text($"{student.AttendancePercentage:F0}%").AlignCenter();

                        var situationColor = student.Situation.Contains("Aprovado") ? "#006600" : student.Situation.Contains("Recuperação") ? "#B8860B" : "#CC0000";
                        table.Cell().Background(bg).Padding(4).Text(student.Situation).FontColor(situationColor).Bold();

                        index++;
                    }
                });

                // Footer
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Documento gerado eletronicamente pelo Sistema Solar LMS - UFC Virtual").FontSize(8).FontColor("#999999");
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateAttendanceReportPdf(ClassAttendanceReportModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Text("UNIVERSIDADE FEDERAL DO CEARÁ - UFC VIRTUAL").Bold().FontSize(13).FontColor("#003E7A");
                    col.Item().Text("SOLAR LMS 2.0 - PAUTA DE FREQUÊNCIA").Bold().FontSize(11).FontColor("#204882");
                    col.Item().PaddingTop(3).LineHorizontal(1).LineColor("#003E7A");
                    col.Item().PaddingTop(4).Text($"Disciplina: {model.CurriculumUnitName} | Semestre: {model.SemesterName} | C.H: {model.TotalHours}h");
                    col.Item().Text($"Docente: {model.TeacherName} | Turma: {model.ClassCode}");
                    col.Item().PaddingBottom(6).LineHorizontal(1).LineColor("#CCCCCC");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#003E7A").Padding(4).Text("Nº").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("Aluno").FontColor("#FFFFFF").Bold();
                        header.Cell().Background("#003E7A").Padding(4).Text("Horas").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("% Freq").FontColor("#FFFFFF").Bold().AlignCenter();
                        header.Cell().Background("#003E7A").Padding(4).Text("Status").FontColor("#FFFFFF").Bold();
                    });

                    int idx = 1;
                    foreach (var student in model.Students)
                    {
                        var bg = idx % 2 == 0 ? "#F8F9FA" : "#FFFFFF";
                        table.Cell().Background(bg).Padding(4).Text(idx.ToString()).AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.StudentName);
                        table.Cell().Background(bg).Padding(4).Text($"{student.AttendedHours} h").AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text($"{student.AttendancePercentage:F0}%").AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text(student.Status);
                        idx++;
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
