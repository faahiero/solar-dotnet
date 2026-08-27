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

export interface SharedMaterialItem {
  id: number;
  title: string;
  author: string;
  uploadedAt: string;
  size: string;
  type: string;
  downloadUrl: string;
  category: string;
}

export interface DigitalClassItem {
  id: number;
  title: string;
  duration: string;
  format: string;
  status: string;
  scormUrl: string;
}

export interface ProgramUnitTopic {
  unit: string;
  hours: number;
  topics: string[];
}

export interface SyllabusInfo {
  curriculumUnitId: number;
  code: string;
  name: string;
  workingHours: number;
  credits: number;
  syllabus: string;
  objectives: string[];
  programContent: ProgramUnitTopic[];
  methodology: string;
  gradingCriteria: string;
}

export interface BibliographyBook {
  id: number;
  title: string;
  authors: string;
  edition: string;
  year: number;
  publisher: string;
  availableOnline: boolean;
  link?: string | null;
}

export interface BibliographyInfo {
  curriculumUnitId: number;
  basic: BibliographyBook[];
  complementary: BibliographyBook[];
}

export interface AcademicEventItem {
  id: number;
  title: string;
  date: string;
  time: string;
  location: string;
  type: string;
  instructor: string;
}
