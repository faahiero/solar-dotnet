import type { TranslationKey } from './pt_BR';

export const en_US: Record<TranslationKey, string> = {
  // Barra Brasil
  gov_brasil: "BRAZIL",
  gov_simplifique: "Simplify!",
  gov_comunica_br: "Communicate BR",
  gov_participe: "Participate",
  gov_acesso_informacao: "Access to Information",
  gov_legislacao: "Legislation",
  gov_canais: "Channels",

  // Cabeçalho e Subtítulo
  solar_subtitle: "Virtual Learning Environment - Federal University of Ceará",
  
  // Login & Autenticação
  login_tab: "Sign In",
  register_tab: "Sign Up",
  user_placeholder: "Enter your username or CPF",
  password_placeholder: "Enter your password",
  btn_access: "Sign In",
  btn_accessing: "Signing in...",
  forgot_password: "Forgot your password?",
  quick_test_users: "Quick test users:",
  login_error_required: "Please provide your username and password.",
  login_error_invalid: "Invalid username or password. Please verify your credentials.",
  logout_success: "Successfully logged out.",

  // Autocadastro / Verificação de CPF
  cpf_verify_title: "Academic Enrollment Verification (SIGAA)",
  cpf_verify_instructions: "Enter your CPF to verify your institutional record and activate your access to Solar LMS.",
  cpf_placeholder: "Enter your CPF (numbers only or formatted)",
  btn_verify_cpf: "Verify Record in SIGAA",
  btn_verifying: "Verifying...",
  cpf_required: "CPF is required.",

  // Topbar Autenticada
  topbar_accessibility: "Accessibility",
  topbar_help: "Help",
  topbar_logout: "Sign Out",
  topbar_accessibility_active: "Accessibility Mode Active (WCAG 2.1 AA)",

  // Abas e Navegação Principal
  nav_home: "My Solar",
  nav_messages: "Messages",
  nav_logs: "Logs & Audit",
  nav_enrollment: "Enrollment",

  // Meu Solar (Dashboard)
  home_welcome: "Welcome, {name}!",
  home_academic_calendar: "Academic Calendar",
  home_my_courses: "My Courses and Classes",
  home_no_courses: "No ongoing courses found.",
  home_open_course: "Open Classroom",
  home_today_activities: "Today's Schedule",
  home_no_activities: "No activities scheduled for today.",
  home_notices: "Notice Board",
  home_no_notices: "No recent institutional announcements.",
  home_system_status: "System Status",
  home_connected: "Connected (.NET 10 Core)",

  // Ambiente de Disciplina (Curriculum Unit)
  cu_tab_classes: "Lessons",
  cu_tab_materials: "Materials",
  cu_tab_forums: "Forums",
  cu_tab_exams: "Assessments & Exams",
  cu_tab_grades: "Gradebook",
  cu_tab_participants: "Participants",
  cu_tab_videoconference: "Web Conference (BBB)",
  cu_join_meeting: "Join Virtual Room",
  cu_progress: "Overall Progress:",
  cu_status_ongoing: "In progress",
  cu_teacher: "Instructor:",

  // Mensagens & Chat
  msg_title: "Messages & Communication Center",
  msg_contacts: "Contacts",
  msg_search_contacts: "Search contacts by name or email...",
  msg_select_chat: "Select a conversation to view message history.",
  msg_type_placeholder: "Type your message and press Enter...",
  msg_btn_send: "Send",
  msg_online: "Online",
  msg_offline: "Offline",

  // Rodapé (Official Footer)
  footer_portals: "Portals ▲",
  footer_development: "Development ▲",
  footer_privacy_policy: "Privacy Policy",
  footer_help: "Help ▲",
  footer_language: "Language ▲",
  footer_portal_virtual: "UFC Virtual Institute",
  footer_portal_ufc: "Federal University of Ceará",
  footer_dev_code: "Source Code",
  footer_dev_team: "Development Team",
  footer_dev_license: "License Terms (GPLv3)",
  footer_help_faq: "Frequently Asked Questions (FAQ)",
  footer_help_videos: "Video Tutorials",
  footer_help_manuals: "Solar LMS User Manuals",
  footer_lang_pt: "Português (BR)",
  footer_lang_en: "English (USA)",

  // Modal de Privacidade
  privacy_title: "Privacy Policy - Solar LMS",
  privacy_sec1_title: "1. About",
  privacy_sec1_body: "We, the Solar LMS 2.0 team (UFC Virtual Institute), value your privacy and thank you for trusting us with your academic information. This document transparently explains what data is collected and how it is used across educational platforms at the Federal University of Ceará.",
  privacy_sec2_title: "2. Information Collected",
  privacy_sec2_item1: "Voluntary interactions: Practical assignments, exams, forum posts, lecture notes, messages, and chats.",
  privacy_sec2_item2: "Institutional System Synchronization (SIGAA): Registration data including CPF, full name, student ID, university email, and class/course affiliations.",
  privacy_sec2_item3: "Integrated Systems: Connected web conferencing tools (BigBlueButton) and external assessment modules.",
  privacy_sec2_item4: "Audit & Security Logs: Client IP address, browser type, operating system, and authentication timestamps to ensure evaluation integrity.",
  privacy_sec3_title: "3. Purpose of Data Processing",
  privacy_sec3_body: "Collected data is utilized exclusively to support academic management, grade recording, attendance tracking, official certificate generation, and continuous improvement of UFC digital learning environments.",
  privacy_sec4_title: "4. Compliance & Security (LGPD)",
  privacy_sec4_body: "In full compliance with Brazilian Data Protection Law (LGPD - Law No. 13,709/2018), no personal or academic data is sold or shared with third parties for commercial purposes. Confidentiality and integrity of credentials are safeguarded through end-to-end encryption.",
  privacy_btn_close: "Understood & Close",

  // Acessibilidade
  vlibras_title: "Accessible with",
  vlibras_sub: "VLibras"
};
