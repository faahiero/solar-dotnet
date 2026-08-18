using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class Exam
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? Duration { get; set; } // Duração em minutos
    public string? StartHour { get; set; }
    public string? EndHour { get; set; }

    public bool RandomQuestions { get; set; }
    public bool RaffleOrder { get; set; } // Embaralha as alternativas
    public bool AutoCorrection { get; set; } = true;
    public int? NumberQuestions { get; set; } // Quantidade sorteada de questões
    public int Attempts { get; set; } = 1;
    public AttemptsCalculationCriterion AttemptsCorrection { get; set; } = AttemptsCalculationCriterion.Greater;

    public bool BlockContent { get; set; } // Anti-fraude: trava outras telas durante a prova
    public bool Uninterrupted { get; set; }
    public bool Controlled { get; set; }
    public bool ImmediateResultRelease { get; set; }
    public DateTime? ResultRelease { get; set; }

    public bool Status { get; set; } = true;
    public bool CanPublish { get; set; } = true;
    public bool ResultEmail { get; set; }
    public int? ScheduleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = [];
}
