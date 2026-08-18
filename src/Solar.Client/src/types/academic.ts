export interface CurriculumUnit {
  id: number;
  code: string;
  name: string;
  courseCode: string;
  courseName: string;
  semester: string;
  type: string;
  typeLabel: string;
  classCode: string;
  description: string;
  hours: number;
}

export interface StaffResponsible {
  role: string;
  name: string;
  email: string;
}

export interface CurriculumUnitDetails {
  id: number;
  code: string;
  name: string;
  courseName: string;
  semester: string;
  classCode: string;
  description: string;
  hours: number;
  staff: StaffResponsible[];
}

export interface LessonItem {
  id: number;
  title: string;
  type: string;
  viewed: boolean;
  notesCount: number;
}

export interface LessonModule {
  moduleId: number;
  moduleName: string;
  lessons: LessonItem[];
}

export interface DiscussionTopic {
  id: number;
  title: string;
  description: string;
  period: string;
  postsCount: number;
  status: string;
  isEvaluative: boolean;
  isFrequency: boolean;
  studentGrade?: number;
}

export interface AssignmentItem {
  id: number;
  title: string;
  type: string;
  maxGroupMembers: number;
  groupName?: string;
  deadline: string;
  status: string;
  submittedFile?: string;
  grade?: number;
  feedback?: string;
}

export interface EvaluativeActivity {
  name: string;
  weight: number;
  finalWeight: string;
  grade: number;
  frequency: string;
}

export interface AccessLog {
  date: string;
  time: string;
}

export interface ScoreReport {
  studentName: string;
  workingHours: string;
  staffResponsibles: string;
  finalExamGrade?: number;
  finalGrade: number;
  frequencyHours: number;
  attendancePercentage: number;
  situation: string;
  evaluativeActivities: EvaluativeActivity[];
  accessHistory: AccessLog[];
}

export interface Participant {
  id: number;
  name: string;
  role: string;
  email: string;
  location: string;
}

export interface InternalMailMessage {
  id: number;
  subject: string;
  sender?: string;
  recipient?: string;
  date: string;
  read: boolean;
  body: string;
}

export interface AgendaEvent {
  day: number;
  title: string;
}

export interface AgendaData {
  month: string;
  currentDay: number;
  activeDays: number[];
  events: AgendaEvent[];
}
