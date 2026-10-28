# O Casa Grande

Plataforma SaaS para monitorar o seu negócio. Esta é a base do projeto: a página inicial pública e a área de administração, onde os módulos da plataforma são ativados ou desativados. Os próximos módulos serão construídos sobre esta estrutura.

## 1. O que existe nesta versão

- **Página inicial** (`/`): barra de navegação com ícones (Início, Sobre, Ideias, Contato), botão **Acessar plataforma**, seções "Sobre" e "Ideias" e rodapé com os contatos.
- **Acessar plataforma** (`/entrar`): página "em breve", até o login dos clientes ser construído.
- **Administração** (`/admin/entrar` e `/admin`): acesso restrito ao administrador, com a lista de módulos, um botão para ativar ou desativar cada um e a atividade recente (logins e alterações).

Os contatos do rodapé estão como "a definir" em `src/OCasaGrande.Web/html/index.html`.

## 2. Tecnologias

| Camada | Tecnologia |
|---|---|
| Back-end e API | C# com .NET 10 (LTS) e ASP.NET Core Minimal APIs |
| Acesso a dados | Entity Framework Core 10 com migrations, provider Npgsql |
| Banco de dados | **PostgreSQL, e só PostgreSQL**: Neon no Vercel e PostgreSQL 17 local |
| Front-end | HTML, CSS e JavaScript (módulos ES), sem framework nem etapa de build |
| Testes | xUnit e `Microsoft.AspNetCore.Mvc.Testing` |
| Publicação | Vercel, como contêiner (`Dockerfile.vercel`) |

A versão do SDK está fixada em `global.json`. As regras permanentes do projeto (PostgreSQL como único banco e segurança como prioridade) estão em `.cursor/rules/postgres-e-seguranca.mdc`.

### O que fica no PostgreSQL

| Tabela | Conteúdo | Usada por |
|---|---|---|
| `Modules` | Módulos da plataforma e se estão ativos | `Api/Modules/ModuleEndpoints.cs` |
| `SecurityEvents` | Auditoria: logins aceitos e recusados, saídas e alterações de módulos, com data, IP e usuário. Nunca guarda senha, hash, token nem o e-mail digitado num login recusado. | `Security/SecurityAudit.cs`, `Api/SecurityEvents/` |
| `RateLimitWindows` | Contador de tentativas de login por IP, compartilhado por todas as instâncias | `Security/SharedRateLimiter.cs` |
| `DataProtectionKeys` | Chaves que criptografam os cookies de sessão e CSRF, compartilhadas por todas as instâncias | ASP.NET Core Data Protection (`Program.cs`) |

O e-mail e o hash da senha do administrador não ficam no banco: são segredos e ficam nas variáveis de ambiente.

## 3. Estrutura das pastas

Cada tipo de arquivo tem a sua pasta. O design (HTML e CSS) fica separado da lógica (JavaScript e C#).

```
├── src/OCasaGrande.Web/
│   ├── Program.cs        # Só a configuração: registra serviços e liga os módulos abaixo
│   ├── Api/              # Endpoints da API, uma pasta por recurso (Api/Modules/, Api/SecurityEvents/)
│   ├── Webhooks/         # Webhooks recebidos de serviços externos, separados da API
│   ├── Auth/             # Login do administrador, sessão e política de acesso
│   ├── Security/         # Cabeçalhos, rate limiting (memória e PostgreSQL), CSRF, auditoria, hash-password
│   ├── Site/             # Rotas que entregam as páginas HTML
│   ├── Domain/           # Entidades (PlatformModule, SecurityEvent, RateLimitWindow)
│   ├── Data/             # DbContext, conexão PostgreSQL, dados iniciais, inicialização e Migrations/
│   ├── html/             # Páginas HTML (admin/ para as páginas restritas)
│   └── wwwroot/
│       ├── css/          # tokens, base, components, layout, landing, admin
│       ├── js/           # Um script por página, mais http.js (chamadas à API)
│       └── assets/icons/ # Ícones SVG (sprite.svg) e favicon
├── tests/OCasaGrande.Tests/   # Testes de integração de segurança, login e API
├── vercel.json, Dockerfile.vercel, .dockerignore
├── global.json, dotnet-tools.json, OCasaGrande.slnx
├── README.md
└── LICENSE
```

**Regras da estrutura:**

- Um arquivo por responsabilidade. `Program.cs` não tem regra de negócio.
- Endpoints novos vão em `Api/<Recurso>/` (`<Recurso>Endpoints.cs` e `<Recurso>Contracts.cs`). Webhooks vão em `Webhooks/<Origem>/`, nunca dentro de `Api/`.
- As páginas ficam em `html/`, fora de `wwwroot`, e só podem ser acessadas pelas rotas de `Site/PageEndpoints.cs`. Assim, uma página restrita não é entregue como arquivo estático.
- Nada de `<script>` ou `style=""` dentro do HTML. Isso é exigido pela política de segurança de conteúdo (CSP).

## 4. Como executar

**Pré-requisitos:** [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) e [PostgreSQL 17](https://www.postgresql.org/download/windows/) (`winget install PostgreSQL.PostgreSQL.17`).

1. Crie um usuário e um banco só para o projeto, com uma senha forte. O usuário precisa de `CREATEDB` porque os testes criam e apagam bancos temporários:

   ```sql
   CREATE ROLE ocasagrande_dev WITH LOGIN CREATEDB PASSWORD '<senha forte>';
   CREATE DATABASE ocasagrande OWNER ocasagrande_dev;
   ```

   Guarde a conexão nos user-secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=ocasagrande;Username=ocasagrande_dev;Password=<senha forte>" --project src/OCasaGrande.Web
   ```

2. Gere o hash da senha do administrador. A senha precisa ter de 12 a 128 caracteres e não é exibida enquanto você digita:

   ```bash
   dotnet run --project src/OCasaGrande.Web -- hash-password
   ```

3. Guarde o e-mail e o hash nos *user-secrets*, que ficam fora do repositório:

   ```bash
   dotnet user-secrets set "Admin:Email" "voce@exemplo.com" --project src/OCasaGrande.Web
   dotnet user-secrets set "Admin:PasswordHash" "<hash gerado>" --project src/OCasaGrande.Web
   ```

4. Rode a aplicação e acesse http://localhost:5180:

   ```bash
   dotnet run --project src/OCasaGrande.Web
   ```

Ao iniciar, a aplicação aplica as migrations pendentes no PostgreSQL. O EF Core trava o banco durante a migração, então várias instâncias iniciando juntas não se atrapalham. Sem conexão PostgreSQL configurada, a aplicação não inicia. Sem `Admin:Email` e `Admin:PasswordHash`, a área de administração recusa qualquer login.

Para rodar os testes (cada classe de teste cria um banco PostgreSQL temporário e o apaga no fim):

```bash
dotnet test
```

**Conexão com o banco:** a aplicação usa `ConnectionStrings:Postgres` se estiver definida. Senão, usa `DATABASE_URL` (com pool de conexões) para as consultas e `DATABASE_URL_UNPOOLED` (conexão direta) para as migrations, que são as variáveis criadas pela integração Neon no Vercel. Conexões com servidores remotos sempre usam TLS com o certificado validado (`SslMode=VerifyFull`).

## 5. Segurança

| Proteção | Como é feita |
|---|---|
| SQL injection | Todas as consultas passam pelo LINQ do EF Core, que é parametrizado. O único SQL escrito à mão (o contador de login) usa SQL interpolado do EF Core, em que cada valor vira parâmetro. A rota de módulos só aceita identificadores `^[a-z0-9-]{1,50}$`. |
| XSS | Os dados vindos da API entram na página só por `textContent` e APIs do DOM, nunca por `innerHTML`. A CSP só permite scripts, estilos e fontes do próprio site. |
| CSRF | Toda requisição que altera dados (`POST`/`PUT`) exige o token antiforgery no cabeçalho `X-CSRF-TOKEN`. Os cookies usam `SameSite=Strict`. |
| Força bruta e sobrecarga | Rate limiting por IP em duas camadas: na memória de cada instância (300 req/min no geral, 60 req/min na API, 5 logins/min), que barra excessos antes de chegarem ao banco, e no PostgreSQL para o login (**5 tentativas por minuto somando todas as instâncias**). Excedido o limite, a resposta é `429`. |
| Auditoria | Logins aceitos e recusados, saídas e alterações de módulos ficam em `SecurityEvents` e aparecem na administração. |
| Banco | TLS com certificado validado em conexões remotas. Erros do banco não chegam ao usuário. |
| Senha | Guardada só como hash PBKDF2, pelo `PasswordHasher` do ASP.NET Core Identity. O hash é verificado mesmo quando o e-mail não confere, para que o tempo de resposta não revele o e-mail do administrador. |
| Sessão | Cookie `HttpOnly`, `Secure` (em produção) e `SameSite=Strict`, com 30 minutos de validade renovada a cada uso. Trocar o e-mail ou a senha na configuração encerra todas as sessões abertas. |
| Cabeçalhos | CSP, `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy` e HSTS em produção. `/api` e `/admin` respondem com `Cache-Control: no-store`. |
| Erros | Em produção, a resposta de erro não mostra detalhes internos (ProblemDetails, RFC 9457). |
| Segredos | Nenhum segredo no repositório. Localmente, use user-secrets. No Vercel, use variáveis de ambiente. |

Nenhum sistema é totalmente imune a ataques. Estas camadas seguem as práticas recomendadas e devem ser revisadas a cada novo módulo.

## 6. Endpoints

| Método e rota | Acesso | Descrição |
|---|---|---|
| `GET /api/auth/csrf` | Público | Devolve o token antiforgery da sessão atual |
| `POST /api/auth/login` | Público, com CSRF e limite de 5/min | `{ email, password }`. Responde `204`, `401` ou `429`. |
| `POST /api/auth/logout` | CSRF | Encerra a sessão |
| `GET /api/auth/session` | Administrador | E-mail do administrador conectado |
| `GET /api/admin/modules` | Administrador | Lista os módulos |
| `PUT /api/admin/modules/{key}` | Administrador, com CSRF | `{ isEnabled }`. Ativa ou desativa o módulo. |
| `GET /api/admin/security-events` | Administrador | Os 50 eventos de segurança mais recentes |
| `GET /health` | Público | Verificação de saúde, incluindo a conexão com o PostgreSQL |

## 7. Módulos e planos

Cada módulo é uma linha da tabela `Modules`, com um `Key` fixo (por exemplo, `monitoramento`). O botão na administração liga ou desliga o módulo para toda a plataforma.

Para adicionar um módulo:

1. Inclua o módulo em `Data/ModuleSeed.cs` e crie uma migration: `dotnet tool restore` e depois `dotnet ef migrations add <Nome> -p src/OCasaGrande.Web -o Data/Migrations`.
2. Crie o código dele nas pastas de sempre: endpoints em `Api/<Modulo>/`, páginas em `html/`, estilos em `wwwroot/css/` e scripts em `wwwroot/js/`.

Os planos vão referenciar os módulos pelo `Key`, numa tabela própria (plano × módulo). Essa tabela ainda não existe, porque os planos ainda não foram definidos.

## 8. Publicação no Vercel

O Vercel não tem runtime .NET próprio, mas executa a aplicação como contêiner, usando o `Dockerfile.vercel` e o `vercel.json` da raiz.

1. Importe o repositório no Vercel, mantendo a raiz como diretório do projeto e o *Framework Preset* como "Other".
2. Em *Storage → Create Database → Neon*, crie o banco e conecte-o ao projeto. A integração cria `DATABASE_URL` e `DATABASE_URL_UNPOOLED`.
3. Em *Settings → Environment Variables*, crie:
   - `PORT` = `8080` (o contêiner roda sem root e escuta na 8080);
   - `Admin__Email` e `Admin__PasswordHash` (o hash gerado com `hash-password`).
4. Em *Firewall*, crie uma regra de **rate limit** para `/api/auth/login`, como camada extra na borda. A proteção contra DDoS do Vercel já vem ativa.
5. Faça o deploy. As migrations são aplicadas quando o contêiner inicia.

## 9. Limitações conhecidas

- **Chaves de sessão sem criptografia extra no banco.** As chaves em `DataProtectionKeys` ficam protegidas pelo acesso ao banco (credenciais secretas e TLS), mas não são criptografadas dentro da tabela. Quem conseguir ler o banco poderia forjar uma sessão. O reforço recomendado é criptografar essas chaves com um certificado guardado fora do banco (`ProtectKeysWithCertificate`).
- **Migrations com o mesmo usuário da aplicação.** No Neon, a aplicação e as migrations usam o mesmo usuário do banco. O ideal é ter um usuário só com permissão de leitura e escrita para a aplicação e outro para alterar o esquema.
- **Um único administrador**, definido pela configuração. Os logins dos clientes e vários administradores ainda vão ser construídos.
- **Contatos do rodapé** estão como "a definir".
