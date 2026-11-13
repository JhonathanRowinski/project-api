# Project API

Backend da plataforma, desenvolvido em .NET 10 e organizado como um monólito modular, com separação de responsabilidades entre API, Application, Domain e Infrastructure.

## Tecnologias

- **.NET 10 / ASP.NET Core** — construção da API.
- **Entity Framework Core** — acesso e persistência de dados.
- **SQL Server** — banco de dados relacional.
- **DbUp** — execução e controle de scripts de banco de dados.
- **JWT** — autenticação baseada em tokens.
- **DeviceDetector.NET** — identificação de navegador, sistema operacional e dispositivo.
- **MaxMind GeoLite2 City** — identificação aproximada da localização a partir do endereço IP.

## Arquitetura

O projeto segue uma organização em camadas, mantendo as regras de negócio independentes dos detalhes de infraestrutura.

```text
Project/
├── Project.Api/
│   ├── Controllers/
│   ├── Exceptions/
│   └── ...
│
├── Project.Application/
│   ├── Auth/
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   └── Options/
│   └── Exceptions/
│
├── Project.Domain/
│   └── Entities/
│       ├── Accounts/
│       └── Auth/
│
└── Project.Infrastructure/
    ├── Accounts/
    ├── Auth/
    ├── BackgroundServices/
    ├── Database/
    │   └── Scripts/
    └── ...
```

### Responsabilidades

| Camada | Responsabilidade |
|---|---|
| API | Endpoints HTTP, configuração da aplicação e tratamento de exceções. |
| Application | Serviços de aplicação, contratos, modelos e opções de configuração. |
| Domain | Entidades e regras centrais do domínio. |
| Infrastructure | Persistência, repositórios, geração de tokens, serviços externos e tarefas em segundo plano. |

## Autenticação e sessões

A autenticação utiliza JWT para access tokens e refresh tokens aleatórios para renovação das sessões.

### Login

Durante o login, a aplicação:

1. Valida as credenciais da conta.
2. Identifica o endereço IP do cliente.
3. Obtém informações do navegador, sistema operacional e dispositivo a partir do User-Agent.
4. Consulta a localização aproximada associada ao IP.
5. Cria uma nova sessão de autenticação.
6. Gera o access token e o refresh token.
7. Persiste a sessão no SQL Server.

### Access token

O access token é um JWT assinado com HMAC-SHA256.

Suas claims incluem:

- `sub`: identificador da conta.
- `email`: endereço de e-mail da conta.
- `jti`: identificador único do token.
- `sid`: identificador da sessão de autenticação.

A validade do access token é limitada tanto pela configuração de duração do token quanto pela expiração absoluta da sessão.

### Refresh token

O refresh token é gerado utilizando um gerador criptograficamente seguro, com 64 bytes aleatórios, e codificado em Base64 URL-safe.

Por segurança:

- O valor original não é armazenado no banco.
- Apenas o hash SHA-256 do token é persistido.
- A cada renovação, o refresh token anterior é substituído por um novo.
- A renovação atualiza sua validade, respeitando o limite absoluto da sessão.

### Duração das sessões

As durações são configuráveis:

| Configuração | Limite |
|---|---|
| Refresh token | Até 3 dias |
| Sessão de autenticação | Até 7 dias |
| Access token | Configurável, limitado a 60 minutos |

A sessão possui uma data de expiração absoluta, que não é estendida durante as renovações.

### Logout e revogação

O logout revoga a sessão de autenticação correspondente.

A sessão também é verificada durante a validação do access token, permitindo invalidar tokens associados a sessões revogadas antes de sua expiração natural.

### Limpeza de sessões

Um serviço em segundo plano executa periodicamente a limpeza de sessões inativas.

São removidas sessões que:

- Foram revogadas.
- Ultrapassaram a validade do refresh token.
- Ultrapassaram a expiração absoluta da sessão.

A limpeza é executada a cada 7 dias.

## Informações de origem do acesso

Durante o login, a aplicação registra informações aproximadas sobre a origem do acesso na entidade `AuthSession`.

### Endereço IP

O endereço IP é identificado considerando a existência de proxies confiáveis.

Cabeçalhos encaminhados, como `X-Forwarded-For`, somente são considerados quando o endereço remoto da conexão pertence à lista de proxies confiáveis configurada na aplicação.

A configuração aceita endereços IP e intervalos CIDR.

Quando não há um proxy confiável, o endereço é obtido diretamente de `HttpContext.Connection.RemoteIpAddress`.

### Geolocalização

A localização é obtida a partir do endereço IP utilizando o banco local MaxMind GeoLite2 City.

As informações armazenadas incluem:

- País e código do país.
- Estado ou região e seu código.
- Cidade.

Os resultados são aproximados e dependem da precisão da base de geolocalização.

### Navegador e dispositivo

O User-Agent recebido na requisição é processado pelo DeviceDetector.NET.

São extraídas informações como:

- Navegador e versão.
- Sistema operacional e versão.
- Tipo de dispositivo.

Requisições identificadas como bots ou valores que não podem ser processados não geram essas informações.

## Banco de dados

A aplicação utiliza SQL Server com Entity Framework Core.

O gerenciamento do esquema é realizado pelo DbUp, que executa scripts SQL incorporados ao assembly da infraestrutura.

Os scripts ficam em:

```text
Project.Infrastructure/
└── Database/
    └── Scripts/
```

O controle de execução é mantido na tabela:

```text
dbo.SchemaVersions
```

Cada script é executado dentro de sua própria transação.

A criação do banco e a aplicação dos scripts são realizadas durante a inicialização da aplicação.

## Configuração

As configurações são carregadas pelo sistema de configuração do ASP.NET Core, utilizando arquivos `appsettings.json`, variáveis de ambiente e provedores de segredos.

### Connection string

Configure a conexão com o SQL Server utilizando o nome `ProjectDb`.

Exemplo de variável de ambiente:

```text
ConnectionStrings__ProjectDb
```

### JWT

As opções de JWT são configuradas na seção correspondente a `JwtOptions`.

Entre as configurações estão:

- `Issuer`: emissor dos tokens.
- `Audience`: público-alvo dos tokens.
- `SigningKey`: chave utilizada para assinar os JWTs.
- `AccessTokenLifetimeMinutes`: duração do access token em minutos.

A chave de assinatura deve possuir pelo menos 32 bytes e não deve ser armazenada no repositório.

Em desenvolvimento, utilize o Secret Manager:

```powershell
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<chave aleatória com pelo menos 32 bytes>" --project Project.Api
```

Em outros ambientes, forneça a chave por um provedor de segredos ou variável de ambiente:

```text
Authentication__Jwt__SigningKey
```

### Sessões e refresh tokens

As opções de duração são configuradas pela seção `AuthTokenOptions`.

- `RefreshTokenLifetimeDays`: validade do refresh token.
- `SessionLifetimeDays`: validade absoluta da sessão.

Os valores são validados durante a inicialização da aplicação.

### Proxies confiáveis

A configuração de `ClientIpOptions` define os endereços IP e intervalos CIDR autorizados a encaminhar informações do cliente.

Não confie indiscriminadamente em cabeçalhos de IP encaminhado. Configure somente proxies efetivamente controlados ou confiáveis.

### Geolocalização

A configuração de `IpGeolocationOptions` define o caminho do banco de dados GeoLite2 City (`.mmdb`).

O arquivo deve estar disponível no ambiente em que a aplicação será executada.

## Executando localmente

### Pré-requisitos

- .NET 10 SDK.
- SQL Server.
- Banco de dados GeoLite2 City, quando a geolocalização estiver habilitada.

### Inicialização

1. Configure a connection string `ProjectDb`.
2. Configure a chave de assinatura JWT.
3. Configure as opções de autenticação.
4. Configure os proxies confiáveis, quando aplicável.
5. Configure o caminho do banco GeoLite2 City.
6. Execute a API.

Exemplo:

```powershell
dotnet restore
dotnet build
dotnet run --project Project.Api
```

Na inicialização, a aplicação verifica a existência do banco e executa os scripts SQL pendentes.

## Tratamento de exceções

As exceções da aplicação são centralizadas pelo `AppExceptionHandler`, que converte erros conhecidos em respostas HTTP padronizadas utilizando `ProblemDetails`.

Exceções inesperadas são registradas nos logs e retornam uma resposta genérica de erro interno, sem expor detalhes sensíveis da implementação.

## Considerações de segurança

- Não armazene chaves de assinatura ou outras credenciais no repositório.
- Armazene somente hashes dos refresh tokens.
- Utilize HTTPS nos ambientes de execução.
- Configure corretamente a lista de proxies confiáveis.
- Não exponha detalhes internos das exceções aos clientes.
- Mantenha o banco GeoLite2 City atualizado.
- Utilize um banco de dados compartilhado caso a aplicação seja executada em múltiplas instâncias.
