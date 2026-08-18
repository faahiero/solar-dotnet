# ☀️ Relatório de Continuidade & Contexto do Projeto Solar LMS (.NET 10 + React 19)

> **Este documento contém todo o contexto, decisões de arquitetura, histórico de migração e instruções para continuar o desenvolvimento em qualquer máquina com o Antigravity (AGY).**

---

## 📌 1. Visão Geral e Repositórios

* **Repositório Oficial (.NET 10 + React 19):** [`https://github.com/faahiero/solar-dotnet`](https://github.com/faahiero/solar-dotnet)
* **Conta do GitHub:** `faahiero` (`fabriciosilvalp@outlook.com`)
* **Deploy na Nuvem:** Configurado via Render Blueprint (`render.yaml`) conectado ao PostgreSQL na nuvem.
* **Repositório de Referência Original (Ruby on Rails):** Localizado na máquina de trabalho em `Ruby/solar/` (permaneceu 100% intacto, zero commits).

---

## 🏛️ 2. Arquitetura e O Que Já Foi Implementado

1. **Back-end (.NET 10 Minimal APIs & Entity Framework Core 10):**
   * **Domínio:** [`src/Solar.Domain/`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/src/Solar.Domain) com regras acadêmicas críticas (cálculo de notas com Avaliação Final, ponderada/aritmética, árvore de discussão de fórum em 7 níveis, clonagem de turmas com ajuste de datas).
   * **Persistência:** [`src/Solar.Infrastructure/`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/src/Solar.Infrastructure) mapeando as 89 tabelas reais do PostgreSQL (`solar_development`).
   * **Autenticação:** Compatibilidade transparente com hashes legados Devise SHA-1 e rehash automático para PBKDF2/HMAC-SHA512.
   * **Trava Anti-Fraude:** [`ExamLockoutMiddleware.cs`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/src/Solar.WebApi/Middlewares/ExamLockoutMiddleware.cs) bloqueando acesso a outras aulas durante a realização de provas.
   * **Upload Real:** Endpoint multipart para submissão de arquivos de trabalho (`.pdf`, `.zip`, `.docx`).
   * **Correção Automática de Provas:** [`ExamScoringService.cs`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/src/Solar.Domain/Assessments/ExamScoringService.cs) com cálculo de nota instantâneo.
   * **Mensageria & Chat:** Correio interno e WebSocket em tempo real via SignalR.

2. **Front-end (React 19 + TypeScript + Vite):**
   * Localizado em [`src/Solar.Client/`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/src/Solar.Client).
   * Design System 100% fiel à identidade visual oficial da UFC Virtual (paleta `#003E7A`, `#204882`, `#F0CF65`, layout fluido responsivo até 1600px).
   * **11 Telas Oficiais Implementadas e Validadas:**
     * Login oficial com card de abas e atalhos rápidos.
     * Meu Solar (Dashboard) com tabela de disciplinas e portlet de Agenda com calendário interativo.
     * Home da Turma com abas dinâmicas closeáveis e menu lateral categorizado.
     * Aulas e Módulos Didáticos.
     * Material de Apoio (Download).
     * Fórum de Discussões com árvore de respostas.
     * Trabalhos com seletor e upload real de arquivos.
     * Player de Prova Online 🔒 interativo com cronômetro regressivo.
     * Acompanhamento de Rendimento / Diário de Notas.
     * Participantes da Turma.
     * Correio Eletrônico e Chat SignalR.

3. **DevOps, CI/CD e Containers:**
   * [`Dockerfile`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/Dockerfile) multi-stage unificado de produção (~110MB).
   * [`docker-compose.yml`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/docker-compose.yml) com App + PostgreSQL (porta 5433) + Redis (porta 6380).
   * [`.github/workflows/ci.yml`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/.github/workflows/ci.yml) rodando com sucesso no GitHub Actions (`✓ PASS`).
   * [`render.yaml`](file:///home/fabricio-silva/Documentos/Workspace/dotnet/solar/render.yaml) configurado para deploy gratuito no Render.com.

4. **Testes Automatizados:**
   * **48 testes aprovados com 100% de sucesso** (`Solar.Domain.Tests` e `Solar.WebApi.Tests`).

---

## 🚀 3. Como Continuar em Casa no Antigravity (AGY)

Ao abrir o terminal no seu computador de casa:

### **Passo 1: Clonar o Repositório**
```bash
git clone https://github.com/faahiero/solar-dotnet.git
cd solar-dotnet
```

### **Passo 2: Iniciar o Antigravity (AGY)**
No terminal, dentro da pasta do projeto, execute:
```bash
agy
```

### **Passo 3: Prompt Inicial para o AGY em Casa**
Copie e cole este comando na primeira mensagem para o AGY:
```text
Olá! Estou continuando o projeto Solar LMS (.NET 10 + React 19).
Por favor, leia o arquivo docs/HANDOFF_PROJETO_SOLAR.md para carregar todo o contexto e histórico do projeto antes de prosseguir.
```

O AGY lerá este documento imediatamente e terá **100% de conhecimento** de tudo o que foi feito, das decisões de arquitetura e do estado atual do código.
