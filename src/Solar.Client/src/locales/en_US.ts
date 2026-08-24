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

  // Modal de Política de Privacidade (Integral do Solar UFC Virtual)
  privacy_title: "Privacy Policy - Solar LMS (UFC Virtual)",
  privacy_sec1_title: "1. About",
  privacy_sec1_p1: "We, members of the Solar LMS 2.0 team (Instituto UFC Virtual), know that you care about your privacy and trust us with your information. For that reason, we prepared this document with our Privacy Policy to explain which information we collect and how it is used.",
  privacy_sec1_p2: "We may update this document over time. When changes occur, users will be notified via the Solar announcement system at least 7 business days following the update.",
  privacy_sec1_p3: "We ask that you read this document carefully.",

  privacy_sec2_title: "2. Information Collected",
  privacy_sec2_intro: "By using Solar, we may collect user data in four ways:",
  privacy_sec2_item1: "1. Voluntary interactions: We obtain information submitted through forms or text fields filled by the user, including messages, assignments, discussions, lesson notes, chat messages, etc.",
  privacy_sec2_item2: "2. Consultation with other institutional applications: Data retrieved from institutional systems (such as SIGAA) based on data stored in Solar. During or after registration, CPF, email, or username are verified, and matching student/teacher records are replicated in Solar, including course enrollments.",
  privacy_sec2_item3: "3. Writing by the user or others in integrated third-party applications: Data submitted through applications integrated into Solar with write permissions to register users or record form submissions.",
  privacy_sec2_item4: "4. By usage: Data collected according to user activity, including navigational logs, IP address(1), browser(2), and operating system(3) versions, access timestamps, and functional cookies(4).",

  privacy_sec3_title: "3. Why We Collect and Use Information",
  privacy_sec3_p1: "We collect and use this information to optimize platform usability, ensuring that submitted academic content is safely stored and accessible to authorized participants.",
  privacy_sec3_p2: "We also utilize data to maintain accurate user records and continuously improve educational features.",
  privacy_sec3_p3: "Collected or transmitted information enables seamless integration with mobile apps and other academic tools.",

  privacy_sec4_title: "4. How We Use Information",
  privacy_sec4_p1: "Your email address may be used to receive notifications sent via the Solar messaging tool or automated alerts (e.g., exam grading updates).",
  privacy_sec4_p2: "Registration data and course allocations may be accessed by administrative staff to resolve access difficulties or user-reported inquiries.",
  privacy_sec4_p3: "Course interactions are accessible to classmates, instructors, and authorized academic secretaries.",
  privacy_sec4_p4: "Physical address data may be used by course coordinators for official certificate delivery.",
  privacy_sec4_p5: "The Solar support team may inspect technical accounts to troubleshoot reported errors without altering academic progress.",
  privacy_sec4_p6: "Quantitative and qualitative academic metrics may be analyzed for educational research with strict student anonymity.",
  privacy_sec4_p7: "Administrative inspections are strictly limited to troubleshooting and auditing, with complete confidentiality.",

  privacy_sec5_title: "5. Sharing Information with Third-Party Applications",
  privacy_sec5_body: "Other academic systems from Instituto UFC Virtual and UFC utilize selected data strictly for official educational purposes.",

  privacy_sec6_title: "6. Additional Information (WebConferencing & Cookies)",
  privacy_sec6_p1: "Solar may redirect users to external tabs for webconferences (BigBlueButton), video tutorials, and reference materials.",
  privacy_sec6_p2: "Users manage their own browser cookie preferences and private browsing modes(7). Note that blocking cookies may affect session persistence.",
  privacy_sec6_p3: "Webconferencing recordings are retained for at least 1 year in compliance with public document archive guidelines.",

  privacy_sec7_title: "7. Information Update and Removal",
  privacy_sec7_p1: "Participants have full freedom to submit, modify, or remove data within activity deadline rules.",
  privacy_sec7_p2: "Users may update profile data at any time, except for UAB/SIGAA integrated profiles, which must be updated at the source system or by contacting atendimento@virtual.ufc.br.",
  privacy_sec7_p3: "Login, password, and email updates in integrated systems synchronize automatically with Solar upon account synchronization.",
  privacy_sec7_p4: "Support staff may assign temporary passwords upon user request, which can be immediately changed after login.",

  privacy_sec8_title: "8. Subtitles & Technical Glossary",
  privacy_sec8_item1: "(1) IP: Internet Protocol address identifying a device on a network.",
  privacy_sec8_item2: "(2) Browser: Software facilitating access to HTML web documents.",
  privacy_sec8_item3: "(3) OS: Operating System managing hardware and software resources.",
  privacy_sec8_item4: "(4) Cookies: Local browser data storing functional user preferences.",
  privacy_sec8_item5: "(5) API: Application Programming Interface allowing external system integrations.",
  privacy_sec8_item6: "(6) Cache: Temporary storage accelerating subsequent page loads.",
  privacy_sec8_item7: "(7) Anonymous Navigation: Private browsing mode preventing local history storage.",

  privacy_sec9_title: "9. LGPD & Data Protection Compliance",
  privacy_sec9_body: "In strict compliance with Brazilian Data Protection Law (LGPD - Law No. 13,709/2018), all data processing is grounded in public educational policy execution. No personal or academic data is ever commercialized.",

  privacy_search_placeholder: "🔍 Search in privacy policy (e.g., SIGAA, IP, cookies, password)...",
  privacy_no_results: "No topics found matching your search terms.",
  privacy_btn_close: "Understood & Close",

  // Acessibilidade
  vlibras_title: "Accessible with",
  vlibras_sub: "VLibras"
};
