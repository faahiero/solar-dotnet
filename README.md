# ☀️ Solar LMS 2.0 (.NET 10 + React 19)

Reimplementação completa do **Solar LMS** (Ambiente Virtual de Aprendizagem da Universidade Federal do Ceará - UFC Virtual) em **.NET 10 (C#)** e **React 19 (TypeScript + Vite)**.

---

## 🚀 Tecnologias Principais

* **Back-end:** .NET 10, ASP.NET Core Minimal APIs, Entity Framework Core 10, Npgsql (PostgreSQL), SignalR.
* **Front-end:** React 19, TypeScript, Vite, CSS moderno responsivo (Design System oficial UFC/Solar).
* **Banco de Dados:** PostgreSQL 16 (89 tabelas relacionais do Solar LMS).
* **DevOps & Containers:** Docker (Multi-stage build), Docker Compose, GitHub Actions CI/CD.
* **Testes:** xUnit, FluentAssertions, WebApplicationFactory (48 testes automatizados).

---

## 🏛️ Arquitetura da Solução

```
solar/
├── src/
│   ├── Solar.Domain/           # Entidades, Enums, Regras de Negócio e Serviços de Domínio
│   ├── Solar.Application/      # Casos de Uso (Auth, Carga Horária, Notas, Clonagem)
│   ├── Solar.Infrastructure/   # Entity Framework Core DbContext e Integrações
│   ├── Solar.WebApi/           # Minimal APIs, Middlewares (Anti-Fraude), SignalR Hubs
│   └── Solar.Client/           # SPA React 19 + TypeScript + Vite
├── tests/
│   ├── Solar.Domain.Tests/     # Testes Unitários de Regras de Negócio
│   └── Solar.WebApi.Tests/     # Testes de Integração de Endpoints e Middlewares
├── Dockerfile                  # Multi-stage build otimizado
└── docker-compose.yml          # Stack completa (App + PostgreSQL + Redis)
```

---

## 🏃 Como Executar

### 1. Execução Direta (.NET SDK)
```bash
dotnet run --project src/Solar.WebApi
```
Acesse no navegador: **`http://localhost:5142`**

### 2. Execução via Docker Compose
```bash
docker compose up --build
```
Acesse no navegador: **`http://localhost:5142`**

---

## 🧪 Execução dos Testes Automatizados

```bash
dotnet test Solar.sln
```

---

## 📸 Identidade Visual e Telas Implementadas

1. **Login Oficial:** Compatibilidade com senhas legadas Devise SHA1 e upgrade para PBKDF2/HMAC-SHA512.
2. **Meu Solar (Dashboard):** Visão geral de turmas ativas, calendário interativo e avisos.
3. **Home da Turma:** Abas dinâmicas, responsáveis e acesso rápido a conteúdos.
4. **Módulos Didáticos & Aulas:** Visualização de pacotes interativos e anotações.
5. **Material de Apoio:** Download de apostilas e planos de ensino.
6. **Fórum de Discussões:** Árvore hierárquica de discussões em até 7 níveis.
7. **Trabalhos e Portfólio:** Envio real de arquivos multipart (.pdf, .zip).
8. **Prova Online 🔒:** Player com cronômetro regressivo, bloqueio anti-fraude e auto-correção.
9. **Acompanhamento de Rendimento:** Diário de notas, frequência e histórico de acessos.
10. **Correio Eletrônico & Chat:** Mensageria interna e chat em tempo real via SignalR.
