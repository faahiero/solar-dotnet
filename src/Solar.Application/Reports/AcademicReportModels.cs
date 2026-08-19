namespace Solar.Application.Reports;

public class ClassGradesReportModel
{
    public string CourseName { get; set; } = string.Empty;
    public string CurriculumUnitName { get; set; } = string.Empty;
    public string CurriculumUnitCode { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int WorkingHours { get; set; } = 64;
    public DateTime EmissionDate { get; set; } = DateTime.UtcNow;
    public List<StudentGradeEntry> Students { get; set; } = new();
}

public class StudentGradeEntry
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public double PartialGrade { get; set; }
    public double? FinalExamGrade { get; set; }
    public double FinalGrade { get; set; }
    public int FrequencyHours { get; set; }
    public double AttendancePercentage { get; set; }
    public string Situation { get; set; } = string.Empty;
}

public class ClassAttendanceReportModel
{
    public string CourseName { get; set; } = string.Empty;
    public string CurriculumUnitName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int TotalHours { get; set; } = 64;
    public DateTime EmissionDate { get; set; } = DateTime.UtcNow;
    public List<StudentAttendanceEntry> Students { get; set; } = new();
}

public class StudentAttendanceEntry
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AttendedHours { get; set; }
    public double AttendancePercentage { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface IAcademicReportService
{
    byte[] GenerateGradesReportPdf(ClassGradesReportModel model);
    byte[] GenerateAttendanceReportPdf(ClassAttendanceReportModel model);
}
