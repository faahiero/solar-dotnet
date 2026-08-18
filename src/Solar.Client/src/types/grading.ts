export interface GradingEvaluationInput {
  activityId: number;
  name: string;
  isEvaluative: boolean;
  isFrequency?: boolean;
  isFinalExam?: boolean;
  weight?: number;
  finalWeight?: number;
  studentGrade?: number;
  studentWorkingHours?: number;
  equivalentActivityId?: number;
}

export interface GradingCourseCriteria {
  passingGrade: number;
  minGradeToFinalExam: number;
  finalExamPassingGrade: number;
  totalWorkingHours: number;
  minHoursPercentage: number;
  hasFinalExamInOffering?: boolean;
}

export interface CalculateStudentGradesCommand {
  userId: number;
  allocationId: number;
  criteria: GradingCourseCriteria;
  activities: GradingEvaluationInput[];
}

export interface GradingCalculationResult {
  partialGrade?: number;
  finalGrade?: number;
  totalFrequencyHours: number;
  attendancePercentage: number;
  situation: number;
  situationDescription: string;
  isApproved: boolean;
}
