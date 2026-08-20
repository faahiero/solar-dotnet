export const pt_BR = {
  // Barra Brasil
  gov_brasil: "BRASIL",
  gov_simplifique: "Simplifique!",
  gov_comunica_br: "Comunica BR",
  gov_participe: "Participe",
  gov_acesso_informacao: "Acesso à informação",
  gov_legislacao: "Legislação",
  gov_canais: "Canais",

  // Cabeçalho e Subtítulo
  solar_subtitle: "Ambiente Virtual de Aprendizagem da Universidade Federal do Ceará",
  
  // Login & Autenticação
  login_tab: "Login",
  register_tab: "Cadastrar",
  user_placeholder: "Digite seu login",
  password_placeholder: "Digite sua senha",
  btn_access: "Acessar",
  btn_accessing: "Acessando...",
  forgot_password: "Esqueceu a sua senha?",
  quick_test_users: "Usuários de teste rápido:",
  login_error_required: "Por favor, informe seu login e sua senha.",
  login_error_invalid: "Usuário ou senha inválidos. Verifique suas credenciais.",
  logout_success: "Logout efetuado com sucesso.",

  // Autocadastro / Verificação de CPF
  cpf_verify_title: "Verificação de Vínculo Acadêmico (SIGAA)",
  cpf_verify_instructions: "Informe seu CPF para verificar seu vínculo institucional e ativar seu acesso no Solar LMS.",
  cpf_placeholder: "Digite seu CPF (apenas números ou formatado)",
  btn_verify_cpf: "Verificar Cadastro no SIGAA",
  btn_verifying: "Verificando...",
  cpf_required: "CPF é obrigatório.",

  // Topbar Autenticada
  topbar_accessibility: "Acessibilidade",
  topbar_help: "Ajuda",
  topbar_logout: "Sair",
  topbar_accessibility_active: "Modo de Acessibilidade Ativo (WCAG 2.1 AA)",

  // Abas e Navegação Principal
  nav_home: "Meu Solar",
  nav_messages: "Mensagens",
  nav_logs: "Logs & Auditoria",
  nav_enrollment: "Matrícula",

  // Meu Solar (Dashboard)
  home_welcome: "Bem-vindo(a), {name}!",
  home_academic_calendar: "Calendário Acadêmico",
  home_my_courses: "Minhas Disciplinas e Turmas",
  home_no_courses: "Nenhuma disciplina em andamento encontrada.",
  home_open_course: "Acessar Ambiente",
  home_today_activities: "Atividades de Hoje",
  home_no_activities: "Nenhuma atividade agendada para hoje.",
  home_notices: "Quadro de Avisos",
  home_no_notices: "Nenhum aviso institucional recente.",
  home_system_status: "Status do Sistema",
  home_connected: "Conectado (.NET 10 Core)",

  // Ambiente de Disciplina (Curriculum Unit)
  cu_tab_classes: "Aulas",
  cu_tab_materials: "Materiais",
  cu_tab_forums: "Fóruns",
  cu_tab_exams: "Avaliações & Provas",
  cu_tab_grades: "Quadro de Notas",
  cu_tab_participants: "Participantes",
  cu_tab_videoconference: "Webconferência (BBB)",
  cu_join_meeting: "Entrar na Sala Virtual",
  cu_progress: "Progresso Geral:",
  cu_status_ongoing: "Em andamento",
  cu_teacher: "Professor(a):",

  // Mensagens & Chat
  msg_title: "Central de Mensagens & Comunicação",
  msg_contacts: "Contatos",
  msg_search_contacts: "Buscar contatos por nome ou e-mail...",
  msg_select_chat: "Selecione uma conversa ao lado para visualizar o histórico de mensagens.",
  msg_type_placeholder: "Digite sua mensagem e pressione Enter...",
  msg_btn_send: "Enviar",
  msg_online: "Online",
  msg_offline: "Offline",

  // Rodapé (Official Footer)
  footer_portals: "Portais ▲",
  footer_development: "Desenvolvimento ▲",
  footer_privacy_policy: "Política de privacidade",
  footer_help: "Ajuda ▲",
  footer_language: "Idioma ▲",
  footer_portal_virtual: "Instituto UFC Virtual",
  footer_portal_ufc: "Universidade Federal do Ceará",
  footer_dev_code: "Código-Fonte",
  footer_dev_team: "Equipe de Desenvolvimento",
  footer_dev_license: "Termos de licença (GPLv3)",
  footer_help_faq: "Dúvidas Frequentes (FAQ)",
  footer_help_videos: "Tutoriais em Vídeo",
  footer_help_manuals: "Manuais do Solar LMS",
  footer_lang_pt: "Português (BR)",
  footer_lang_en: "English (USA)",

  // Modal de Privacidade
  privacy_title: "Política de Privacidade - Solar LMS",
  privacy_sec1_title: "1. Sobre",
  privacy_sec1_body: "Nós, da equipe do Solar LMS 2.0 (Instituto UFC Virtual), sabemos que você preza pela sua privacidade e nos confia suas informações acadêmicas. Este documento explica com transparência quais dados são coletados e como são utilizados no ambiente educacional da Universidade Federal do Ceará.",
  privacy_sec2_title: "2. Informações Coletadas",
  privacy_sec2_item1: "Interações voluntárias: Trabalhos práticos, provas, respostas de fóruns, anotações de aulas, mensagens e bate-papo.",
  privacy_sec2_item2: "Sincronização com Sistemas Institucionais (SIGAA): Dados cadastrais como CPF, nome completo, matrícula, e-mail institucional e vínculos em turmas/disciplinas.",
  privacy_sec2_item3: "Sistemas Integrados: Ferramentas conectadas de Web Conferência (BigBlueButton) e avaliações.",
  privacy_sec2_item4: "Logs de Auditoria e Segurança: Endereço IP de acesso, navegador, sistema operacional e registros de autenticação para garantia de integridade nas avaliações.",
  privacy_sec3_title: "3. Finalidade do Tratamento de Dados",
  privacy_sec3_body: "As informações coletadas são utilizadas estritamente para viabilizar a gestão acadêmica, lançamento de notas e frequências, emissão de certificados oficiais e aperfeiçoamento contínuo das plataformas de ensino da UFC.",
  privacy_sec4_title: "4. Conformidade e Segurança (LGPD)",
  privacy_sec4_body: "Em conformidade com a Lei Geral de Proteção de Dados (Lei nº 13.709/2018), nenhum dado pessoal ou acadêmico é comercializado ou compartilhado com terceiros para fins comerciais. A confidencialidade e integridade das credenciais são asseguradas por criptografia de ponta a ponta.",
  privacy_btn_close: "Compreendido e Fechar",

  // Acessibilidade
  vlibras_title: "Acessível com",
  vlibras_sub: "VLibras"
};

export type TranslationKey = keyof typeof pt_BR;
