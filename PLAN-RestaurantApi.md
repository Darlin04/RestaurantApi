# Plan de implementación: RestaurantApi

> **Para el agente que implementa:** lee este documento completo antes de escribir código. Es la única fuente de verdad. Ejecuta los PBIs en el orden de olas de la sección 12, respeta las reglas de la sección 11 y no inventes endpoints, campos ni reglas. Si algo no está especificado, aplica la sección 11.9.

| Dato | Valor |
|---|---|
| Repositorio | `RestaurantApi` (GitHub `Darlin04/RestaurantApi`) |
| Prefijo de IDs | `RST` |
| Namespace raíz | `Restaurant` |
| Arquitectura | Clean Architecture (4 proyectos) + DDD táctico, **sin MediatR** |
| Base de datos | PostgreSQL 17 (esquemas `restaurant` e `identity`, nombres en `snake_case`) |
| API | Minimal APIs, `/api/v1`, JWT, OpenAPI + Scalar |
| Autor | Darlin De La Nieve (proyecto individual) |

## 1. Contexto y alcance

**Problema de negocio.** Un restaurante necesita controlar sus mesas, su menú y las órdenes que toman los meseros. La versión anterior tenía la lógica en controladores y servicios, el subtotal lo enviaba el cliente, y cualquier usuario podía registrarse como administrador. Esta versión pone las reglas en el dominio:
- El subtotal lo calcula el sistema.
- Una mesa solo puede tener una orden activa.
- Las transiciones de estado de mesas y órdenes las controla el agregado.
- Solo un Admin crea usuarios.

**Dentro del alcance (solo backend):**
1. Autenticación JWT y gestión de usuarios con roles `Admin` y `Waiter`.
2. Ingredientes: crear, listar, consultar, renombrar y eliminar (si no están en uso).
3. Platos: crear, listar con filtros, consultar, actualizar, activar y desactivar.
4. Mesas: crear, listar con filtro de estado, consultar, actualizar detalles y liberar.
5. Órdenes: abrir una orden en una mesa, agregar y quitar platos, completarla, listarla con filtros y paginación, y consultarla.
6. Plataforma: CI, Docker, health check, OpenAPI/Scalar, README y archivo `.http`.

**Fuera del alcance:** frontend, pagos y facturación, inventario o stock de ingredientes, reservas, refresh tokens, envío de emails, subida de imágenes y multi-sucursal.

**Ciclo de vida:**
```
Mesa:   Available ──StartService()──► InService ──MarkServed()──► Served ──Release()──► Available
Orden:  (Open) ──► InProgress ──Complete()──► Completed
Abrir orden  ⇒ la mesa pasa a InService (solo si estaba Available)
Completar    ⇒ la mesa pasa a Served
Liberar mesa ⇒ un Waiter o Admin la devuelve a Available
```


## 2. Stack obligatorio

- **SDK:** .NET 10 (LTS). `global.json` en la raíz con `"version": "10.0.100"` y `"rollForward": "latestFeature"`. Todos los proyectos usan `net10.0`.
- **API:** ASP.NET Core **Minimal APIs**. Están prohibidos los Controllers y MVC.
- **Persistencia:** EF Core 10 con PostgreSQL 17 (proveedor Npgsql).
- **Documentación de la API:** OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) más Scalar, ambos solo en `Development`.
- **Formato de solución:** `.slnx` (el que crea `dotnet new sln` en .NET 10).
- **Paquetes:** se gestionan con Central Package Management (`Directory.Packages.props`). En la Ola 0 se fija la **última versión estable** de cada serie indicada, como versión exacta y sin comodines. Ningún PBI posterior agrega paquetes: si hace falta uno, se escala al Supervisor.

| Paquete | Serie | Proyecto |
|---|---|---|
| Microsoft.AspNetCore.OpenApi | 10.x | Api |
| Scalar.AspNetCore | 2.x | Api |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.x | Api |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.x | Infrastructure |
| Microsoft.EntityFrameworkCore | 10.x | Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 10.x | Api (`PrivateAssets=all`) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.x | Infrastructure |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.x | Infrastructure |
| FluentValidation y FluentValidation.DependencyInjectionExtensions | 12.x | Application |
| NUnit | 4.x | tests |
| NUnit3TestAdapter | 5.x | tests |
| Microsoft.NET.Test.Sdk | última estable | tests |
| Moq | 4.20.x (≥ 4.20.72) | tests |
| Shouldly | 4.x | tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.x | tests de integración |
| Testcontainers.PostgreSql | 4.x | tests de integración |
| coverlet.collector | 6.x | tests |
| EFCore.NamingConventions | 10.x | Infrastructure |
- **Paquetes prohibidos:**
  - AutoMapper, que tiene licencia comercial desde la v15. El mapeo se hace a mano.
  - MediatR 13 o superior, que también es de licencia comercial.
  - Swashbuckle, Newtonsoft.Json y Dapper.
  - Cualquier paquete que no esté en esta tabla.
- **SQL crudo prohibido** en el código de la aplicación (`FromSqlRaw`, `ExecuteSqlRaw` con texto interpolado). Solo LINQ, salvo las excepciones que el plan autoriza de forma explícita. En el código de **tests** se permite DDL con texto constante (por ejemplo, `CREATE DATABASE`).

## 3. Convenciones de código (obligatorias)

1. **Idioma:** el código (clases, métodos, variables, rutas) y los commits van en **inglés**. Los mensajes de error y validación que ve el usuario, el README y la carpeta `docs/` van en **español**.
2. **`Directory.Build.props` en la raíz:**
   - `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`.
   - En los proyectos de test agrega `<NoWarn>$(NoWarn);CA1707</NoWarn>`, porque los nombres de test llevan guion bajo.
3. **Namespaces:** file-scoped. El namespace es igual a la ruta de carpetas.
4. **Clases:** `sealed` por defecto, salvo las clases base.
5. **Records:** se usan `record` para DTOs, commands, queries y value objects.
6. **Inyección de dependencias:** con **constructor primario** (`public sealed class X(IFoo foo)`).
7. **Asincronía:**
   - Todo I/O es asíncrono, con sufijo `Async`.
   - `CancellationToken cancellationToken` va como último parámetro y se propaga hasta EF Core.
   - Están prohibidos `.Result`, `.Wait()` y `async void`.
8. **Endpoints sin lógica:**
   - El endpoint solo hace esto, en este orden: (a) recibir los parámetros, (b) construir el command o query, (c) invocar el handler, (d) devolver `TypedResults`.
   - Máximo 15 líneas por lambda o método de endpoint.
   - Cada endpoint declara `.WithName("<NombreÚnico>")`, `.WithTags("<Recurso>")`, `.WithSummary("<texto en español>")`, `.Produces...` o `.ProducesProblem(...)` para cada código de estado documentado, y su política de autorización.
9. **Agregados y entidades:**
   - Propiedades con `private set` y constructor privado sin parámetros para EF Core.
   - Factory estático `Create(...)`. Toda mutación pasa por un método con nombre de negocio.
   - Las invariantes se validan **dentro del dominio** y lanzan `DomainException(code, message)`.
   - Las colecciones se exponen como `IReadOnlyCollection<T>` sobre una `List<T>` privada.
10. **Value objects:** `sealed record` **no posicional**, con propiedades `{ get; }`, constructor **privado** y factory `Create(...)` que valida y lanza `DomainException`. Así nadie puede saltarse la validación. Los handlers convierten los primitivos del request con `Create` (por ejemplo, `Money.Create(command.Price)`) antes de llamar al agregado.
11. **Handlers:** solo orquestan. La única regla que puede vivir en un handler es la que depende del usuario autenticado (por ejemplo, "no puedes desactivarte a ti mismo"), porque el dominio no conoce la sesión. Cargan agregados, llaman métodos del dominio, verifican lo que requiere base de datos (unicidad, existencia) y persisten. **No contienen reglas de negocio.**
12. **Fechas:** `DateTimeOffset` en UTC, obtenido de `TimeProvider` inyectado. Nunca `DateTime.Now`.
13. **Identificadores:** `Guid` generado en el dominio con `Guid.CreateVersion7()`.
14. **Dinero:** `decimal`, con 2 decimales y columna `decimal(18,2)`. Nunca `double` ni `float`. La única excepción es un cálculo matemático intermedio que el plan autorice de forma explícita (por ejemplo, una potencia), cuyo resultado se convierte a `decimal` y se redondea como indica el plan.
15. **Value objects de un solo valor** (nombres, códigos, teléfonos): se mapean con `HasConversion(v => v.Value, s => <VO>.Create(s))` y el nombre de columna de la sección 9.
16. **Colecciones de agregados:** campo privado `_xxx` (`List<T>`) expuesto como `IReadOnlyCollection<T>`. En la configuración se usa `Navigation(x => x.Xxx).UsePropertyAccessMode(PropertyAccessMode.Field)`.
17. **Mapeo de `Money` en EF Core:** `ComplexProperty(x => x.Prop, m => m.Property(v => v.Amount).HasColumnName("<columna de la sección 9>").HasPrecision(18, 2))`. Siempre con `HasColumnName` explícito, para evitar nombres generados como `Prop_Amount`.
18. **Enums:** se persisten como texto (`HasConversion<string>()`, máximo 30 caracteres) y se serializan como texto en JSON (`JsonStringEnumConverter` global).
19. **Validación en dos capas:** el validator (400) revisa la forma y los rangos del request. El dominio (422) protege las invariantes, aunque repita algunos rangos. Cuando una regla está en las dos capas, el test de endpoint espera **400** y el 422 se prueba en el test de dominio.
20. **GET con filtros:** el endpoint recibe el query record con `[AsParameters]` (por ejemplo, `([AsParameters] ListOrdersQuery query, ...)`). Nunca se lee un body en un GET.
21. **Mapeo a respuestas:** con un método estático `From(...)` dentro del record de respuesta (`XResponse.From(entity)`). Sin librerías.
22. **Limpieza:** sin código comentado, sin `TODO` ni `FIXME`, y sin comentarios que repitan lo que dice el código.
23. **Formato:** `.editorconfig` en la raíz (Ola 0). `dotnet format --verify-no-changes` tiene que pasar sin cambios.
24. **Constantes de error:** cada agregado tiene su clase estática `<Agregado>Errors` en la misma carpeta, con un método por error que devuelve `DomainException`, `NotFoundException` o `ConflictException`. Estas tres excepciones viven en `Domain/Common`. Los mensajes se escriben **exactamente** como aparecen en este plan.

## 4. Seguridad (obligatoria; ningún PBI la puede relajar)

### 4.1 Secretos y configuración
- **Ningún secreto en el repositorio.** `appsettings.json` y `appsettings.Development.json` solo tienen valores no secretos (logging, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes`, opciones de negocio).
- **En desarrollo se usa `dotnet user-secrets`** en el proyecto Api, con `UserSecretsId` fijo: `3f2b6c1e-7a4d-4e8b-9c51-2d6f0a8b7e13`.
  - Todos los worktrees comparten el mismo `UserSecretsId`, así que los secretos se configuran una sola vez por máquina.
- **Claves de secretos obligatorias:**

| Clave | Valor de desarrollo |
|---|---|
| `ConnectionStrings:Default` | `Host=localhost;Port=5432;Database=restaurant;Username=restaurant_app;Password=<valor de POSTGRES_PASSWORD en tu .env>` |
| `Jwt:Key` | 64 bytes aleatorios en base64: `openssl rand -base64 64` |
| `Seed:AdminEmail` | correo del administrador inicial |
| `Seed:AdminPassword` | contraseña que cumpla la política de la sección 4.2 |

- **Comandos** (los ejecuta el humano o el Supervisor una vez por máquina; nunca se commitean):
```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=restaurant;Username=restaurant_app;Password=<valor de POSTGRES_PASSWORD en tu .env>" --project src/Restaurant.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)" --project src/Restaurant.Api
dotnet user-secrets set "Seed:AdminEmail" "admin@restaurant.local" --project src/Restaurant.Api
dotnet user-secrets set "Seed:AdminPassword" "<contraseña-fuerte>" --project src/Restaurant.Api
```
- **Docker** (`docker-compose.yml` es **solo para desarrollo local**; usa `ASPNETCORE_ENVIRONMENT=Development` y no se usa en producción):
  - `docker-compose.yml` lee los valores de un archivo `.env`, que está en `.gitignore`.
  - En el repo se commitea `.env.example`, con los mismos nombres de variables y valores `cambiar-esto`.
  - La API en el contenedor recibe `ConnectionStrings__Default`, `Jwt__Key`, `Seed__AdminEmail` y `Seed__AdminPassword` como variables de entorno.
- **Fallo rápido al arrancar:**
  - Si falta `ConnectionStrings:Default`, la API lanza `InvalidOperationException("Falta la configuración 'ConnectionStrings:Default'. Configúrala con dotnet user-secrets o variables de entorno.")`.
  - `JwtOptions` usa `ValidateDataAnnotations()` y `ValidateOnStart()`, con `Key` de longitud mínima 64.
  - Si faltan los valores de `Seed:*`, el seed lanza una excepción con un mensaje equivalente.
- **Tests:** usan Testcontainers, que genera su propia base y contraseña, y una `Jwt:Key` aleatoria generada en memoria por la factory de tests. Ningún secreto en el código de test.
- **`.gitignore`:** el de `dotnet new gitignore`, más `.env`, `*.user`, `storage/` y `appsettings.*.local.json`.

### 4.2 Autenticación y autorización
- **Usuarios:** `UserName` = email (Identity lo exige). Las tablas de Identity se mapean con `ToTable("<nombre>", "identity")` explícito para cada entidad de Identity, con los nombres de la sección 9 o del PBI PLT-03.
- **Errores de Identity:** no tienen clase `Errors`. Se lanzan directamente en el servicio, handler o slice que los usa, con el `code` y el mensaje exactos de la tabla del plan.
- **Identidad:** ASP.NET Core Identity con `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<RestaurantDbContext>().AddSignInManager()`.
- **Política de contraseñas:** mínimo 10 caracteres, con mayúscula, minúscula, dígito y carácter no alfanumérico. `RequireUniqueEmail = true`.
- **Bloqueo de cuenta:** `IdentityOptions.Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)` y `AllowedForNewUsers = true`. El login usa `lockoutOnFailure: true`.
- **Login:** `POST /api/v1/auth/login` es **propio**. No se usa `MapIdentityApi`.
  - Busca el usuario por email. Si no existe o tiene `IsActive == false`, responde 401. Si existe y está activo, valida con `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.
  - Cualquier fallo responde **401** con el mismo mensaje: `"Credenciales inválidas."`. Esto incluye usuario inexistente, contraseña incorrecta, cuenta bloqueada y usuario inactivo. No se revela cuál fue la causa.
- **JWT:**
  - HS256, firmado con `Jwt:Key`. Expira en 60 minutos y no hay refresh tokens (fuera de alcance).
  - Claims: `sub` (id del usuario), `email`, `jti` y un claim `role` por cada rol.
  - Se crea con `JsonWebTokenHandler`.
- **Validación JWT:**
  - Se validan issuer, audience, lifetime y firma. `ClockSkew` = 30 segundos.
  - `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`.
  - En `JwtBearerEvents.OnTokenValidated` se carga el usuario por `sub` y se rechaza el token (`context.Fail(...)`) si el usuario no existe o tiene `IsActive == false`. Así, desactivar un usuario corta su acceso de inmediato.
- **Autorización:**
  - `FallbackPolicy` = usuario autenticado. Todo endpoint sin `.AllowAnonymous()` explícito exige token.
  - Las políticas por rol se definen como constantes en la clase `Policies` y los roles en la clase `Roles`, ambas en Application.
- **Rate limiting:**
  - La partición es por `RemoteIpAddress`. Detrás de un proxy inverso hay que configurar `ForwardedHeaders`; esto queda en el README como limitación conocida.
  - `RateLimiterOptions.RejectionStatusCode = 429`; sin esto, ASP.NET Core responde 503. `OnRejected` escribe un ProblemDetails 429.
  - Política `"auth"`: ventana fija de 5 solicitudes por minuto por IP. Se aplica a login y, si el proyecto lo tiene, a registro. Responde **429**.


### 4.3 Otras reglas
- **Propiedad de los datos (IDOR):**
  - Todo recurso que pertenece a un usuario se consulta filtrando por el id del usuario autenticado (`ICurrentUser.UserId`).
  - Si el recurso existe pero es de otro usuario, se responde **404**, igual que si no existiera.
- **Entrada:**
  - Solo se aceptan DTOs de request con los campos permitidos. Nunca se enlaza una entidad del dominio desde el body.
  - Todo request se valida con FluentValidation.
- **Paginación:** `page` ≥ 1 (por defecto 1) y `pageSize` entre 1 y 100 (por defecto 20). Si está fuera de rango, responde 400.
- **Errores 500:** devuelven solo `"Ocurrió un error inesperado."` y el `traceId`. Nunca el `exception.Message` ni el stack trace.
- **Logging:**
  - Se usa `ILogger` con mensajes estructurados.
  - Prohibido registrar contraseñas, tokens, connection strings o el body de los requests.
- **Transporte:**
  - Fuera de `Development`: `UseHsts()` y `UseHttpsRedirection()`.
  - OpenAPI y Scalar solo se exponen en `Development`.
- **Contenedor:** el Dockerfile es multi-stage y la imagen final corre como usuario no root (`USER $APP_UID` de la imagen `mcr.microsoft.com/dotnet/aspnet:10.0`).
- **Concurrencia:** los agregados que mueven estado crítico tienen token de concurrencia (ver sección 9). `DbUpdateConcurrencyException` responde **409**.

## 5. Errores y respuestas HTTP

Un único `GlobalExceptionHandler : IExceptionHandler`, más `AddProblemDetails()`. Todas las respuestas de error son `application/problem+json` e incluyen `traceId` y `code`.

| Excepción | HTTP | `code` | `detail` |
|---|---|---|---|
| `FluentValidation.ValidationException` | 400 | `Validation.Failed` | `"Uno o más campos no son válidos."` + `errors: { campo: [mensajes] }` en camelCase |
| `BadHttpRequestException` (JSON mal formado) | 400 | `Request.Malformed` | `"La solicitud no tiene un formato válido."` |
| `NotFoundException` | 404 | el de la excepción | mensaje de la excepción |
| `ConflictException` | 409 | el de la excepción | mensaje de la excepción |
| `DbUpdateConcurrencyException` | 409 | `Concurrency.Conflict` | `"El recurso fue modificado por otra operación. Intenta de nuevo."` |
| `DbUpdateException` por índice único (SQL Server: números de error 2601 o 2627; PostgreSQL: `SqlState` 23505) | 409 | `Conflict.Duplicate` | `"Ya existe un registro con esos datos."` (cubre carreras entre dos solicitudes simultáneas que pasaron el chequeo previo de unicidad) |
| `DomainException` | 422 | el de la excepción | mensaje de la excepción |
| `UnauthorizedException` | 401 | el de la excepción | mensaje de la excepción |
| `ForbiddenException` | 403 | el de la excepción | mensaje de la excepción |
| Cualquier otra | 500 | `Server.Unexpected` | `"Ocurrió un error inesperado."` (se registra con `LogError`) |

Las respuestas 401 y 403 automáticas del middleware de autenticación también salen como ProblemDetails, configurándolo en `AddProblemDetails` y con `UseStatusCodePages()`.

Ejemplo de respuesta de error:
```json
{ "type": "https://httpstatuses.io/422", "title": "Regla de negocio incumplida", "status": 422,
  "detail": "La mesa 3 no puede pasar de InService a InService.", "code": "Table.InvalidTransition",
  "traceId": "00-..." }
```
Títulos por código de estado:
- 400: `"Solicitud inválida"`
- 401: `"No autenticado"`
- 403: `"Acceso denegado"`
- 404: `"Recurso no encontrado"`
- 409: `"Conflicto"`
- 422: `"Regla de negocio incumplida"`
- 429: `"Demasiadas solicitudes"`
- 500: `"Error interno"`

## 6. Estrategia de pruebas

- **Frameworks:** NUnit, Shouldly (aserciones) y Moq (mocks).
- **Nombres de test:** `Metodo_Escenario_ResultadoEsperado`, por ejemplo `StartService_TableInService_ThrowsInvalidTransition`.
- **Estructura:** cada test lleva los comentarios `// Arrange`, `// Act`, `// Assert`.
- **Proyectos de test:** `Restaurant.Domain.Tests` (dominio, sin dependencias), `Restaurant.Application.Tests` (handlers con Moq) y `Restaurant.Api.IntegrationTests` (endpoints contra PostgreSQL real con Testcontainers).
- **Qué es obligatorio probar:**
  - **Dominio:** cada invariante y cada transición de estado tiene al menos un test del caso válido y uno del inválido, verificando el `code` del error.
  - **Handlers** (si el proyecto tiene tests de Application): el camino feliz y cada excepción que el handler lanza por su cuenta (not found, conflict).
  - **Endpoints** (integración): el camino feliz con el código y el body esperados, 401 sin token (salvo endpoints anónimos), 403 con un rol no permitido (si el endpoint restringe roles), y el error principal del PBI (400, 404, 409 o 422).
- **Infraestructura de integración** (Ola 0):
  - `ApiFactory : WebApplicationFactory<Program>`, con un contenedor de Testcontainers compartido por todo el ensamblado de tests (`[SetUpFixture]`).
  - Crea el esquema con `Database.EnsureCreatedAsync()`. Los PBIs no dependen de migraciones.
  - La clase `TestAuth` (PLT-05) tiene los helpers que crean un usuario único, con email aleatorio, y devuelven un `HttpClient` con su token, además de un cliente anónimo. Sus nombres exactos están en el PBI PLT-05.
- **Tests con conteos o listados exactos:** usan una base propia (`new ApiFactory(databaseName: "<prefijo>_" + Guid.NewGuid().ToString("N"))`) o filtran el resultado por los IDs que el mismo test creó.
- **Aislamiento de datos:**
  - Cada test crea sus propios datos, con nombres o números únicos aleatorios. No se limpia la base entre tests y ningún test depende de otro.
  - Los datos de prueba de un PBI se insertan **directamente** con el `DbContext` (`factory.ExecuteDbContextAsync(...)`), no llamando a endpoints de otros PBIs.
- **Prohibido:** `[Ignore]`, tests vacíos y `Thread.Sleep`.


## 7. Solución y arquitectura

### 7.1 Estructura exacta
```
RestaurantApi/
├─ RestaurantApi.slnx
├─ global.json · Directory.Build.props · Directory.Packages.props · .editorconfig · .gitignore
├─ docker-compose.yml · .env.example · Dockerfile · README.md
├─ docs/  progress.md · decisions.md · architecture.md
├─ .github/  workflows/ci.yml · ISSUE_TEMPLATE/{feature.yml,pbi.yml,bug.yml} · pull_request_template.md
├─ src/
│  ├─ Restaurant.Domain/
│  │  ├─ Common/          AggregateRoot.cs · Entity.cs · DomainException.cs · NotFoundException.cs · ConflictException.cs · Money.cs
│  │  ├─ Ingredients/     Ingredient.cs · IngredientName.cs · IngredientErrors.cs · IIngredientRepository.cs
│  │  ├─ Dishes/          Dish.cs · DishName.cs · DishCategory.cs · DishIngredient.cs · DishErrors.cs · IDishRepository.cs
│  │  ├─ Tables/          Table.cs · TableStatus.cs · TableErrors.cs · ITableRepository.cs
│  │  └─ Orders/          Order.cs · OrderItem.cs · OrderLine.cs · OrderStatus.cs · OrderErrors.cs · IOrderRepository.cs
│  ├─ Restaurant.Application/
│  │  ├─ Abstractions/    ICommandHandler.cs · IQueryHandler.cs · Unit.cs · ICurrentUser.cs · IUnitOfWork.cs
│  │  │                   ForbiddenException.cs · UnauthorizedException.cs
│  │  │                   PagedResponse.cs · Roles.cs · Policies.cs · ValidationDecorator.cs
│  │  ├─ Identity/        ILoginService.cs · LoginResponse.cs · UserResponse.cs · IUserManagementService.cs · IUserActivationService.cs (+ use cases)
│  │  ├─ Ingredients/     IngredientResponse.cs · <UseCase>/{<UseCase>Command.cs, ...Handler.cs, ...Validator.cs}
│  │  ├─ Dishes/ · Tables/ · Orders/   (mismo patrón)
│  │  └─ DependencyInjection.cs
│  ├─ Restaurant.Infrastructure/
│  │  ├─ Persistence/     RestaurantDbContext.cs · DesignTimeDbContextFactory.cs · Configurations/ · Repositories/ · Migrations/
│  │  ├─ Identity/        ApplicationUser.cs · LoginService.cs · UserManagementService.cs · UserActivationService.cs · JwtOptions.cs · JwtTokenGenerator.cs · IdentitySeeder.cs
│  │  └─ DependencyInjection.cs
│  └─ Restaurant.Api/
│     ├─ Common/          IEndpoint.cs · EndpointExtensions.cs · GlobalExceptionHandler.cs · CurrentUser.cs · OpenApiExtensions.cs
│     ├─ Endpoints/       Auth/ · Users/ · Ingredients/ · Dishes/ · Tables/ · Orders/   (un archivo por endpoint)
│     ├─ Program.cs · appsettings.json · appsettings.Development.json · Restaurant.Api.http
└─ tests/
   ├─ Restaurant.Domain.Tests/        <Agregado>/<Agregado>Tests.cs
   ├─ Restaurant.Application.Tests/   <Feature>/<UseCase>HandlerTests.cs
   └─ Restaurant.Api.IntegrationTests/ Common/{ApiFactory.cs, PostgresFixture.cs, TestAuth.cs, MigrationsTests.cs} · <Feature>/<UseCase>Tests.cs
```

**Referencias entre proyectos (y ninguna otra):**
- Application → Domain.
- Infrastructure → Application, Domain.
- Api → Application, Infrastructure.
- Domain.Tests → Domain.
- Application.Tests → Application.
- IntegrationTests → Api.

### 7.2 Patrón de casos de uso (sin MediatR)
```csharp
// Application/Abstractions
public interface ICommandHandler<in TCommand, TResult> { Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken); }
public interface IQueryHandler<in TQuery, TResult>     { Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken); }
public readonly record struct Unit { public static readonly Unit Value = new(); }
```
- **`DependencyInjection.AddApplication()`** escanea el ensamblado de Application. Por cada clase concreta `H` que implementa `ICommandHandler<TC,TR>` o `IQueryHandler<TQ,TR>`, hace esto:
  - Registra `H` como scoped.
  - Registra la interfaz cerrada como scoped, con una factory que devuelve `new ValidationDecorator<T,TR>(sp.GetRequiredService<H>(), sp.GetServices<IValidator<T>>())`.
  - Además llama a `AddValidatorsFromAssembly(..., ServiceLifetime.Scoped)`.
- **`ValidationDecorator<T,TR>`** implementa las dos interfaces. Ejecuta todos los validators y, si hay fallos, lanza `FluentValidation.ValidationException` con la lista de fallos. Si no, delega en el handler.
- **Flujo de una solicitud:**
```
HTTP ─► Endpoint (Api) ─► ICommandHandler<T,R> = ValidationDecorator ─► Handler (Application)
                                                    │ valida (400)          ├─► Repositorio (Infrastructure)
                                                    │                       ├─► Agregado.Método()  ← reglas (422)
                                                    │                       └─► IUnitOfWork.SaveChangesAsync()
◄── TypedResults ◄──────────────────────────────────┴── Response DTO ◄──────┘
Excepciones ─► GlobalExceptionHandler ─► ProblemDetails (sección 5)
```

### 7.3 Ejemplo de referencia (todos los casos de uso siguen esta forma)
```csharp
// Application/Ingredients/CreateIngredient/CreateIngredientCommand.cs
public sealed record CreateIngredientCommand(string Name);

// Application/Ingredients/CreateIngredient/CreateIngredientValidator.cs
public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
                            .MaximumLength(60).WithMessage("El nombre no puede exceder 60 caracteres.");
}

// Application/Ingredients/CreateIngredient/CreateIngredientHandler.cs
public sealed class CreateIngredientHandler(IIngredientRepository ingredients, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateIngredientCommand, IngredientResponse>
{
    public async Task<IngredientResponse> HandleAsync(CreateIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = Ingredient.Create(command.Name);
        if (await ingredients.ExistsByNormalizedNameAsync(ingredient.NormalizedName, null, cancellationToken))
            throw IngredientErrors.DuplicateName(ingredient.Name.Value);
        ingredients.Add(ingredient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return IngredientResponse.From(ingredient);
    }
}

// Api/Endpoints/Ingredients/CreateIngredientEndpoint.cs
public sealed class CreateIngredientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/ingredients", async (CreateIngredientCommand command,
                ICommandHandler<CreateIngredientCommand, IngredientResponse> handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/ingredients/{result.Id}", result);
            })
           .RequireAuthorization(Policies.Admin)
           .WithName("CreateIngredient").WithTags("Ingredients").WithSummary("Crea un ingrediente")
           .Produces<IngredientResponse>(StatusCodes.Status201Created)
           .ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
}
```
**Endpoints con id en la ruta y body** (por ejemplo, PUT): el body se recibe en un record `<UseCase>Request` definido **en el mismo archivo del endpoint**, y el endpoint construye el command como `new UpdateXCommand(id, request.Campo...)`.

### 7.4 Roles y políticas

| Constante | Valor |
|---|---|
| `Roles.Admin` | `"Admin"` |
| `Roles.Waiter` | `"Waiter"` |
| `Policies.Admin` | requiere el rol Admin |
| `Policies.Waiter` | requiere el rol Waiter |
| `Policies.Staff` | requiere el rol Admin **o** Waiter |

## 8. Modelo de dominio

### 8.1 Common (Ola 0)
- **`AggregateRoot`:** abstracta, con `Guid Id { get; protected set; }`. Este proyecto no usa eventos de dominio.
- **`Entity`:** abstracta, con `Guid Id { get; protected set; }`. Se usa para las entidades hijas.
- **`DomainException(string code, string message) : Exception`**, con la propiedad `Code`.
- **`NotFoundException(string code, string message)`** y **`ConflictException(string code, string message)`**: heredan de `Exception` (no de `DomainException`) y tienen la propiedad `Code`. Viven en el dominio para que las clases `<Agregado>Errors` puedan crearlas.
- **`Money`:** `sealed record Money` con `decimal Amount { get; }` y constructor privado (convención 10).
  - `Create(decimal amount)`: lanza `Money.Negative` ("El monto no puede ser negativo.") si `amount < 0`, y `Money.TooManyDecimals` ("El monto no puede tener más de 2 decimales.") si `decimal.Round(amount, 2) != amount`.
  - Además tiene `Zero`, `operator +` y `Multiply(int quantity)`.

### 8.2 Ingredient (PBI RST-ING-01)

| Miembro | Detalle |
|---|---|
| `Guid Id` | v7 |
| `IngredientName Name` | VO: string sin espacios al inicio ni al final, de 2 a 60 caracteres |
| `string NormalizedName` | `Name.Value.ToUpperInvariant()`; se actualiza en `Create` y `Rename` |
| `static Create(string name)` | crea con un nuevo Id |
| `Rename(string name)` | cambia el nombre |

**Repositorio `IIngredientRepository`:**
- `GetByIdAsync(Guid id, ct)` → `Ingredient?`
- `GetByIdsAsync(IReadOnlyCollection<Guid> ids, ct)` → `IReadOnlyList<Ingredient>`
- `ListAsync(ct)`, ordenado por `Name`
- `ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId, ct)`
- `Add(Ingredient)`
- `Remove(Ingredient)`

**`IngredientResponse(Guid Id, string Name)`.**

| Código | HTTP | Mensaje |
|---|---|---|
| `Ingredient.InvalidName` | 422 | `El nombre del ingrediente debe tener entre 2 y 60 caracteres.` |
| `Ingredient.DuplicateName` | 409 | `Ya existe un ingrediente con el nombre '{name}'.` |
| `Ingredient.NotFound` | 404 | `No existe el ingrediente '{id}'.` |
| `Ingredient.InUse` | 409 | `El ingrediente está siendo usado por uno o más platos.` |

### 8.3 Dish (PBI RST-DSH-01)

| Miembro | Detalle |
|---|---|
| `DishName Name` | VO de 2 a 80 caracteres, sin espacios al inicio ni al final |
| `string NormalizedName` | en mayúsculas, único |
| `Money Price` | mayor que 0 y como máximo 999 999.99 |
| `int Servings` | de 1 a 20 |
| `DishCategory Category` | enum `Appetizer`, `MainCourse`, `Dessert`, `Beverage` |
| `bool IsActive` | `true` al crearse |
| `IReadOnlyCollection<DishIngredient> Ingredients` | entidad propia con `Guid IngredientId`; entre 1 y 30 ingredientes, duplicados eliminados con `Distinct` |
| `static Create(name, Money price, int servings, DishCategory category, IEnumerable<Guid> ingredientIds)` | valida todas las reglas |
| `Update(name, price, servings, category, ingredientIds)` | exige `IsActive`; reemplaza la colección de ingredientes |
| `Deactivate()` / `Activate()` | idempotentes |
| `OrderLine ToOrderLine(int quantity)` | exige `IsActive`; devuelve `new OrderLine(Id, Name.Value, Price, quantity)` |

**Repositorio `IDishRepository`:**
- `GetByIdAsync` (incluye los ingredientes)
- `GetByIdsAsync(ids)`
- `ListAsync(DishCategory? category, bool? isActive, ct)`, ordenado por `Name`
- `ExistsByNormalizedNameAsync(normalizedName, excludeId)`
- `AnyUsingIngredientAsync(Guid ingredientId)`
- `Add`

**Respuestas:**
- `DishResponse(Guid Id, string Name, decimal Price, int Servings, string Category, bool IsActive, IReadOnlyList<IngredientResponse> Ingredients)`.
- `DishResponse.From(Dish dish, IReadOnlyList<Ingredient> ingredients)` ordena los ingredientes por nombre.

| Código | HTTP | Mensaje |
|---|---|---|
| `Dish.InvalidName` | 422 | `El nombre del plato debe tener entre 2 y 80 caracteres.` |
| `Dish.InvalidPrice` | 422 | `El precio debe ser mayor que 0 y no exceder 999,999.99.` |
| `Dish.InvalidServings` | 422 | `Las porciones deben estar entre 1 y 20.` |
| `Dish.IngredientsRequired` | 422 | `El plato debe tener al menos un ingrediente.` |
| `Dish.TooManyIngredients` | 422 | `El plato no puede tener más de 30 ingredientes.` |
| `Dish.Inactive` | 422 | `El plato '{name}' no está disponible.` |
| `Dish.DuplicateName` | 409 | `Ya existe un plato con el nombre '{name}'.` |
| `Dish.NotFound` | 404 | `No existe el plato '{id}'.` |

### 8.4 Table (PBI RST-TBL-01)

| Miembro | Detalle |
|---|---|
| `int Number` | de 1 a 999, único, no editable |
| `int Capacity` | de 1 a 20 |
| `string? Description` | máximo 100 caracteres; se guarda `null` si viene vacía |
| `TableStatus Status` | `Available`, `InService`, `Served`; `Available` al crearse |
| `static Create(int number, int capacity, string? description)` | crea la mesa |
| `UpdateDetails(int capacity, string? description)` | solo si está `Available` |
| `StartService()` | `Available` → `InService` |
| `MarkServed()` | `InService` → `Served` |
| `Release()` | `Served` → `Available` |

**Repositorio `ITableRepository`:**
- `GetByIdAsync`
- `GetByIdsAsync(ids)`
- `ListAsync(TableStatus? status)`, ordenado por `Number`
- `ExistsByNumberAsync(int number)`
- `Add`

**`TableResponse(Guid Id, int Number, int Capacity, string? Description, string Status)`.**

| Código | HTTP | Mensaje |
|---|---|---|
| `Table.InvalidNumber` | 422 | `El número de mesa debe estar entre 1 y 999.` |
| `Table.InvalidCapacity` | 422 | `La capacidad debe estar entre 1 y 20.` |
| `Table.InvalidDescription` | 422 | `La descripción no puede exceder 100 caracteres.` |
| `Table.NotAvailable` | 422 | `Solo se puede modificar una mesa disponible.` |
| `Table.InvalidTransition` | 422 | `La mesa {number} no puede pasar de {from} a {to}.` |
| `Table.DuplicateNumber` | 409 | `Ya existe la mesa número {number}.` |
| `Table.NotFound` | 404 | `No existe la mesa '{id}'.` |

### 8.5 Order (PBI RST-ORD-01)

**`OrderLine`:** `sealed record OrderLine(Guid DishId, string DishName, Money UnitPrice, int Quantity)`. Es un record de datos interno, no un value object validado, así que se permite la forma posicional; lo crea `Dish.ToOrderLine`.

**`OrderItem : Entity`:** `DishId`, `DishName` (máximo 80), `Money UnitPrice` (precio congelado al momento de ordenar), `int Quantity` (de 1 a 50) y `Money LineTotal => UnitPrice.Multiply(Quantity)` (no se persiste).

| Miembro de `Order` | Detalle |
|---|---|
| `Guid TableId`, `Guid WaiterId` | |
| `OrderStatus Status` | `InProgress`, `Completed` |
| `IReadOnlyCollection<OrderItem> Items` | |
| `Money Subtotal` | se persiste y se recalcula en cada cambio de items (suma de `LineTotal`) |
| `DateTimeOffset CreatedAt`, `DateTimeOffset? CompletedAt` | |
| `static Open(Guid tableId, Guid waiterId, IReadOnlyList<OrderLine> lines, DateTimeOffset now)` | exige al menos 1 línea; las líneas con el mismo `DishId` se combinan sumando cantidades |
| `AddItem(OrderLine line)` | exige `InProgress`; si ya existe un item con el mismo `DishId` **y** el mismo `UnitPrice`, suma la cantidad; si el precio es distinto (el plato cambió de precio), crea un item nuevo con el precio actual |
| `RemoveItem(Guid orderItemId)` | exige `InProgress`; el item debe existir; no se puede quitar el último |
| `Complete(DateTimeOffset now)` | exige `InProgress`; asigna `CompletedAt` |

La regla de cantidad (de 1 a 50) se aplica a la cantidad final de cada item, después de combinar.

**Repositorio `IOrderRepository`:**
- `GetByIdAsync` (incluye los items)
- `ListAsync(Guid? tableId, OrderStatus? status, int page, int pageSize, ct)` → `(IReadOnlyList<Order> Items, int TotalCount)`, ordenado por `CreatedAt` descendente
- `Add`

**Respuestas:**
- `OrderItemResponse(Guid Id, Guid DishId, string DishName, decimal UnitPrice, int Quantity, decimal LineTotal)`
- `OrderResponse(Guid Id, Guid TableId, int TableNumber, Guid WaiterId, string Status, decimal Subtotal, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, IReadOnlyList<OrderItemResponse> Items)`
- `OrderSummaryResponse(Guid Id, Guid TableId, int TableNumber, string Status, decimal Subtotal, int ItemCount, DateTimeOffset CreatedAt)`

| Código | HTTP | Mensaje |
|---|---|---|
| `Order.ItemsRequired` | 422 | `La orden debe tener al menos un plato.` |
| `Order.InvalidQuantity` | 422 | `La cantidad de cada plato debe estar entre 1 y 50.` |
| `Order.NotInProgress` | 422 | `La orden ya fue completada y no se puede modificar.` |
| `Order.ItemNotFound` | 422 | `La orden no contiene el item '{id}'.` |
| `Order.CannotRemoveLastItem` | 422 | `No se puede eliminar el único plato de la orden.` |
| `Order.NotFound` | 404 | `No existe la orden '{id}'.` |

**Modelo de trabajo en sala (decisión de negocio):** cualquier usuario `Waiter` puede agregar, quitar y completar cualquier orden `InProgress`, porque el servicio es de equipo. `WaiterId` solo registra quién la abrió. No es un IDOR: es la regla del negocio, y queda escrita en `docs/decisions.md` (PLT-01).

### 8.6 Usuarios (Identity; no son agregados de dominio)
- **`ApplicationUser : IdentityUser<Guid>`:** agrega `FirstName` (2–50), `LastName` (2–50), `IsActive` (por defecto `true`) y `CreatedAt`.
- **Servicios de identidad:** las interfaces están en `Application/Identity` y las implementaciones en `Infrastructure/Identity`. Hay **una interfaz por PBI**, cada una en su propio archivo, para que no haya conflictos entre PBIs paralelos:
  - `ILoginService`, con `LoginAsync(string email, string password, CancellationToken ct)` → `LoginResponse` y `GetCurrentAsync(Guid userId, ct)` → `CurrentUserResponse` (PLT-05; implementación `LoginService`).
  - `IUserManagementService`, con `CreateAsync(CreateUserCommand, ct)` → `UserResponse`, `ListAsync(string? role, bool? isActive, ct)` y `GetAsync(Guid id, ct)` (USR-01; implementación `UserManagementService`).
  - `IUserActivationService.SetActiveAsync(Guid userId, bool isActive, ct)` (USR-02; implementación `UserActivationService`).
  - Toda clase de Infrastructure cuyo nombre termina en `Service` y que implementa una interfaz de Application se registra como scoped por escaneo (Ola 0).
- **Respuestas:**
  - `LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email, IReadOnlyList<string> Roles)`
  - `UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role, bool IsActive)`

| Código | HTTP | Mensaje |
|---|---|---|
| `Auth.InvalidCredentials` | 401 | `Credenciales inválidas.` |
| `User.DuplicateEmail` | 409 | `Ya existe un usuario con el correo '{email}'.` |
| `User.NotFound` | 404 | `No existe el usuario '{id}'.` |
| `User.CannotDeactivateSelf` | 422 | `No puedes desactivar tu propio usuario.` |

`Auth.InvalidCredentials` se lanza como `UnauthorizedException(code, message)`, definida en `Application/Abstractions` en la Ola 0. El `GlobalExceptionHandler` la mapea a 401.

## 9. Persistencia

- **`RestaurantDbContext`** hereda de `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` e implementa `IUnitOfWork`.
  - Las tablas de Identity van en el esquema `identity` y las de negocio en `restaurant`.
  - Nombres en `snake_case` con `UseSnakeCaseNamingConvention()`.
  - `ApplyConfigurationsFromAssembly`.
  - **No se declaran `DbSet` de negocio.** Los repositorios usan `Set<T>()` y los agregados entran al modelo solo por su `IEntityTypeConfiguration`. Así, ningún PBI modifica el DbContext, que está congelado después de la Ola 0.
- **Concurrencia:** `Table` y `Order` tienen la propiedad sombra `builder.Property<uint>("RowVersion").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();`. Es la columna de sistema `xmin` de PostgreSQL, no una columna nueva; el nombre explícito evita que la convención snake_case la renombre.
- **Repositorios:**
  - Toda clase `*Repository` de `Infrastructure/Persistence/Repositories` se registra como scoped contra su interfaz, por escaneo (Ola 0).
  - Las consultas de solo lectura usan `AsNoTracking()`.

| Tabla | Columnas | Restricciones |
|---|---|---|
| `restaurant.ingredients` | `id uuid PK`, `name varchar(60)`, `normalized_name varchar(60)` | único(`normalized_name`) |
| `restaurant.dishes` | `id`, `name varchar(80)`, `normalized_name varchar(80)`, `price numeric(18,2)`, `servings int`, `category varchar(30)`, `is_active bool` | único(`normalized_name`) |
| `restaurant.dish_ingredients` | `dish_id uuid`, `ingredient_id uuid` | PK(`dish_id`,`ingredient_id`); FK dish → CASCADE; FK ingredient → RESTRICT |
| `restaurant.tables` | `id`, `number int`, `capacity int`, `description varchar(100) null`, `status varchar(30)`, `xmin` | único(`number`) |
| `restaurant.orders` | `id`, `table_id uuid`, `waiter_id uuid`, `status varchar(30)`, `subtotal numeric(18,2)`, `created_at timestamptz`, `completed_at timestamptz null`, `xmin` | FK table → RESTRICT; FK waiter → `identity.users(id)` RESTRICT; índice(`table_id`,`status`); **índice único parcial** (`table_id`) `WHERE status = 'InProgress'` |
| `restaurant.order_items` | `id`, `order_id uuid`, `dish_id uuid`, `dish_name varchar(80)`, `unit_price numeric(18,2)`, `quantity int` | FK order → CASCADE; FK dish → RESTRICT |

- **Seed** (`IdentitySeeder`, `IHostedService`, idempotente, se ejecuta al arrancar):
  - Crea los roles `Admin` y `Waiter` si no existen.
  - Crea el usuario Admin con `Seed:AdminEmail` y `Seed:AdminPassword` si no existe.
  - No se siembran datos de negocio.
- **`DesignTimeDbContextFactory`:** usa la connection string de la variable de entorno `RESTAURANT_DESIGN_CONNECTION`. Si no existe, usa `Host=localhost;Database=restaurant_design`, que no lleva contraseña y solo sirve para generar migraciones.

## 10. API (todas bajo `/api/v1`, salvo `/health`)

| Método y ruta | Política | Request | Respuesta | Códigos | PBI |
|---|---|---|---|---|---|
| `GET /health` | anónimo | — | texto `Healthy` | 200 | PLT-04 |
| `POST /auth/login` | anónimo + rate `auth` | `{ email, password }` | `LoginResponse` | 200, 400, 401, 429 | PLT-05 |
| `GET /auth/me` | autenticado | — | `CurrentUserResponse` | 200, 401 | PLT-05 |
| `POST /users` | Admin | `{ email, firstName, lastName, password, role }` | `UserResponse` | 201, 400, 401, 403, 409 | USR-01 |
| `GET /users` | Admin | `?role=&isActive=` | `UserResponse[]` ordenado por email | 200, 400, 401, 403 | USR-01 |
| `GET /users/{id}` | Admin | — | `UserResponse` | 200, 401, 403, 404 | USR-01 |
| `PATCH /users/{id}/deactivate` | Admin | — | — | 204, 401, 403, 404, 422 | USR-02 |
| `PATCH /users/{id}/activate` | Admin | — | — | 204, 401, 403, 404 | USR-02 |
| `POST /ingredients` | Admin | `{ name }` | `IngredientResponse` | 201, 400, 401, 403, 409, 422 | ING-01 |
| `GET /ingredients` | Staff | — | `IngredientResponse[]` | 200, 401 | ING-02 |
| `GET /ingredients/{id}` | Staff | — | `IngredientResponse` | 200, 401, 404 | ING-02 |
| `PUT /ingredients/{id}` | Admin | `{ name }` | `IngredientResponse` | 200, 400, 401, 403, 404, 409, 422 | ING-03 |
| `DELETE /ingredients/{id}` | Admin | — | — | 204, 401, 403, 404, 409 | ING-04 |
| `POST /dishes` | Admin | `{ name, price, servings, category, ingredientIds[] }` | `DishResponse` | 201, 400, 401, 403, 404, 409, 422 | DSH-01 |
| `GET /dishes` | Staff | `?category=&isActive=` | `DishResponse[]` | 200, 400, 401 | DSH-02 |
| `GET /dishes/{id}` | Staff | — | `DishResponse` | 200, 401, 404 | DSH-02 |
| `PUT /dishes/{id}` | Admin | igual que POST | `DishResponse` | 200, 400, 401, 403, 404, 409, 422 | DSH-03 |
| `PATCH /dishes/{id}/deactivate` | Admin | — | — | 204, 401, 403, 404 | DSH-04 |
| `PATCH /dishes/{id}/activate` | Admin | — | — | 204, 401, 403, 404 | DSH-04 |
| `POST /tables` | Admin | `{ number, capacity, description }` | `TableResponse` | 201, 400, 401, 403, 409, 422 | TBL-01 |
| `GET /tables` | Staff | `?status=` | `TableResponse[]` | 200, 400, 401 | TBL-02 |
| `GET /tables/{id}` | Staff | — | `TableResponse` | 200, 401, 404 | TBL-02 |
| `PUT /tables/{id}` | Admin | `{ capacity, description }` | `TableResponse` | 200, 400, 401, 403, 404, 422 | TBL-03 |
| `PATCH /tables/{id}/release` | Staff | — | `TableResponse` | 200, 401, 404, 409, 422 | TBL-04 |
| `POST /orders` | Waiter | `{ tableId, items: [{ dishId, quantity }] }` | `OrderResponse` | 201, 400, 401, 403, 404, 409, 422 | ORD-01 |
| `POST /orders/{id}/items` | Waiter | `{ dishId, quantity }` | `OrderResponse` | 200, 400, 401, 403, 404, 409, 422 | ORD-02 |
| `DELETE /orders/{id}/items/{itemId}` | Waiter | — | `OrderResponse` | 200, 401, 403, 404, 409, 422 | ORD-02 |
| `PATCH /orders/{id}/complete` | Waiter | — | `OrderResponse` | 200, 401, 403, 404, 409, 422 | ORD-03 |
| `GET /orders` | Staff | `?tableId=&status=&page=&pageSize=` | `PagedResponse<OrderSummaryResponse>` | 200, 400, 401 | ORD-04 |
| `GET /orders/{id}` | Staff | — | `OrderResponse` | 200, 401, 404 | ORD-04 |

- **Los 409 de endpoints que mutan mesas u órdenes** salen de `Concurrency.Conflict` (xmin) o de `Conflict.Duplicate` (índice único parcial de una orden activa por mesa) cuando dos solicitudes compiten.
- **`PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages)`.**
- **Validaciones de request (400):** `email` con formato válido; `password` con la política de 4.2 (FluentValidation, mensajes en español); `role` ∈ {`Admin`, `Waiter`}; `price` > 0; `quantity` entre 1 y 50; `items` no vacío y con máximo 30 entradas; `ingredientIds` no vacío; ids distintos de `Guid.Empty`; enums en query string válidos.


## 11. Trabajo en paralelo: worktrees, olas y Supervisor

### 11.1 Roles

| Rol | Responsabilidad |
|---|---|
| **Supervisor** (1 agente) | Es el único que escribe en `main`. Ejecuta la Ola 0 y los PBIs `RST-PLT-*`. Asigna PBIs y revisa cada PR con el checklist de la sección 11.7. Hace los merges en el orden definido. Es el único que genera migraciones. Resuelve conflictos en archivos compartidos y mantiene `docs/progress.md` y `docs/decisions.md`. |
| **Implementador** (1 a 4 agentes) | Toma **un PBI a la vez**, en su propio worktree y su propia rama. Solo crea o modifica los archivos que su PBI declara. Abre un PR cuando cumple la DoD. |

### 11.2 Olas
- Los PBIs están agrupados en **olas** (sección 12). Una ola empieza cuando la anterior está **completamente mergeada en `main`** y el Supervisor ya agregó su migración.
- Dentro de una ola, todos los PBIs pueden **empezar** en paralelo. Si hay menos agentes que PBIs, cada agente toma el siguiente PBI pendiente de la ola, en el orden de la tabla.
- **Excepción:** si la sección 12 o el PBI indican un orden de merge dentro de la ola ("X se mergea primero"), el PBI dependiente programa contra las firmas fijas de este plan. Antes de abrir su PR espera a que X esté en `main` y hace `git rebase origin/main`. El Supervisor respeta ese orden en la cola de merge.
- La Ola 0 es siempre secuencial y la ejecuta el Supervisor.

### 11.3 Reglas anti-conflicto (obligatorias)
1. **Archivos congelados después de la Ola 0.** Solo el Supervisor los toca, dentro de un PBI `RST-PLT-*`:
   - `Program.cs`
   - Los `DependencyInjection.cs` de cada capa
   - El `DbContext`
   - `Directory.Build.props` y `Directory.Packages.props`
   - El `.slnx` y los `.csproj`
   - `.editorconfig`, `global.json` y `.github/`
   - Las carpetas `Common/` y `Abstractions/` de cada capa
   - La infraestructura de tests compartida (`ApiFactory`, `TestAuth`, fixtures). Los helpers de datos (`TestData`) siguen las reglas propias de cada PBI que los menciona.
2. **Descubrimiento por reflexión.** La Ola 0 deja todo listo para que un PBI **solo agregue archivos nuevos**:
   - Endpoints: toda clase que implementa `IEndpoint` se mapea automáticamente bajo `/api/v1`.
   - Handlers y validators: se registran por escaneo de ensamblado.
   - Configuraciones de EF Core: `ApplyConfigurationsFromAssembly`.
   - Repositorios y servicios: toda clase de Infrastructure cuyo nombre termina en `Repository` o `Service` se registra como scoped contra **todas** las interfaces que implementa y que están definidas en Domain o Application (por ejemplo, `CurrentCustomerService` contra `ICurrentCustomerProvider`).
3. **Propiedad de archivos.** Cada PBI lista los archivos que crea. El PBI que crea un agregado (el de "modelo") crea también su clase `Errors`, su configuración de EF, sus DTOs de respuesta compartidos y su repositorio (interfaz e implementación), con **todos** los métodos que la feature va a necesitar según este plan. Los PBIs posteriores de esa feature no modifican esos archivos.
4. **Sin migraciones en ramas de PBI.** Los tests usan `EnsureCreated`. Las migraciones las genera solo el Supervisor, en `main`, al cerrar cada ola (sección 11.6).
5. **Sin paquetes nuevos.** Todos se declaran en la Ola 0.
6. **Bugs en código ajeno:** si un implementador encuentra un bug en código ya mergeado de otro PBI, no lo corrige en su rama. Lo reporta al Supervisor, que crea un PBI `RST-FIX-NN`.
7. **Conflictos al hacer rebase:** el implementador solo resuelve conflictos en archivos de su PBI. Si el conflicto está en un archivo congelado o de otro PBI, se detiene y escala al Supervisor.

### 11.4 Layout y comandos de worktrees
```
~/dev/RestaurantApi/                      ← clon principal en main (solo el Supervisor trabaja aquí)
~/dev/RestaurantApi.worktrees/
    RST-XXX-01/                  ← un worktree por PBI, cada uno en su rama
    RST-YYY-01/
```
**Rama:** `feature/<ID-PBI>-<slug-en-ingles>`, por ejemplo `feature/RST-ING-01-create-ingredient`. Para correcciones: `fix/<ID>-<slug>`.

**Implementador, al empezar un PBI:**
```bash
cd ~/dev/RestaurantApi
git fetch origin
git worktree add ../RestaurantApi.worktrees/<ID> -b feature/<ID>-<slug> origin/main
cd ../RestaurantApi.worktrees/<ID>
dotnet restore && dotnet build && dotnet test
```
**Implementador, por cada Task del PBI** (1 Task = 1 commit):
```bash
dotnet build && dotnet test
git add <solo archivos del PBI>
git commit -m "<tipo>(<scope>): <descripción en imperativo> [<ID-PBI>]"
```
**Implementador, al terminar el PBI:**
```bash
git fetch origin
git rebase origin/main
dotnet build -warnaserror && dotnet format --verify-no-changes && dotnet test
git push -u origin feature/<ID>-<slug>
gh pr create --base main --title "<tipo>(<scope>): <título del PBI> [<ID>]" \
  --body-file .github/pull_request_template.md   # completar: "Closes #<issue>" y el checklist
```
**Supervisor, al hacer merge** (en el orden de la tabla de la ola):
```bash
gh pr merge <n> --merge --delete-branch      # merge commit, sin squash, para conservar cada Task
git -C ~/dev/RestaurantApi pull
dotnet build -warnaserror && dotnet test     # en main; si falla: git revert -m 1 <merge-sha> y se devuelve el PBI
git worktree remove ../RestaurantApi.worktrees/<ID>
```
**Sin remoto en GitHub** (todo local): la rama sale de `main` (`git worktree add ... -b feature/... main`). En lugar del PR, el Supervisor revisa con `git diff main...feature/<ID>` y hace `git merge --no-ff feature/<ID>` sobre `main`, con los mismos pasos de verificación.

**Notas sobre los worktrees:**
- Cada worktree tiene sus propios `bin/` y `obj/`, así que no hay choques al compilar.
- Testcontainers asigna puertos aleatorios, así que los tests de varios worktrees pueden correr al mismo tiempo.
- `user-secrets` se comparte entre todos los worktrees, porque depende del `UserSecretsId`.
- Para ejecutar la API a mano hay un solo contenedor de base de datos (`docker compose up -d db`), compartido por todos.

### 11.5 Commits y trazabilidad
- **Formato:** Conventional Commits en inglés y en imperativo, terminando con el ID del PBI entre corchetes.
  - Si existe Issue de GitHub, se agrega su número: `feat(orders): add Order aggregate with item rules [RST-ORD-01] (#23)`.
  - Tipos permitidos: `feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `ci`. El `scope` es el nombre de la feature en minúsculas.
- **Una Task del plan es un commit.** Si una Task no compila sola, se combina con la siguiente en un solo commit que menciona las dos.
- **PR:**
  - Título: `<tipo>(<scope>): <título del PBI> [<ID>]`.
  - El cuerpo incluye `Closes #<issue>` (si hay Issue) y el checklist de la DoD marcado.
- **Issues de GitHub** (los crea el Supervisor en la Ola 0, si hay `gh` autenticado):
  - Se crea un Issue por Feature (label `feature`) y uno por PBI (label `pbi`, sub-issue de su Feature).
  - Las Tasks van como checklist en el cuerpo del PBI.
  - En `docs/progress.md` se registra la tabla `ID plan | # issue | estado | rama | PR`.
- **Board:** GitHub Projects con las columnas Backlog → Ready → In Progress → In Review → Done. Cada ola es un Milestone (`Ola N`).
- **Releases:** al cerrar cada ola, el Supervisor crea el tag `v0.<N>.0` con `gh release create v0.<N>.0 --generate-notes`. Al terminar todo el plan, el tag es `v1.0.0`.

### 11.6 Cierre de ola (Supervisor)
1. Confirmar que todos los PRs de la ola están mergeados y que `main` compila con tests verdes.
2. Generar la migración de la ola:
   ```bash
   dotnet ef migrations add Wave<N>_<DescripcionCorta> \
     --project src/Restaurant.Infrastructure --startup-project src/Restaurant.Api --output-dir Persistence/Migrations
   ```
   - Revisar el SQL con `dotnet ef migrations script`: nombres de tablas y columnas según la sección 9, índices únicos y FKs con el `OnDelete` indicado.
   - Si la migración sale vacía, no se crea.
3. Ejecutar el test `MigrationsTests.AllMigrations_ApplyOnEmptyDatabase` (Ola 0), que aplica `MigrateAsync()` sobre un contenedor vacío.
4. Commit: `chore(db): add Wave<N> migration [RST-PLT-MIG]`.
5. Actualizar `docs/progress.md`, cerrar el Milestone y crear el tag `v0.<N>.0`.
6. Anunciar el inicio de la siguiente ola.

### 11.7 Checklist de revisión del Supervisor (para cada PR)
- [ ] Solo modifica o crea los archivos que declara el PBI.
- [ ] Ningún archivo congelado fue tocado.
- [ ] Las reglas de negocio están en el dominio y cada una tiene su test de dominio.
- [ ] Los endpoints cumplen la convención 8: sin lógica, `TypedResults`, metadatos y autorización correcta.
- [ ] Rutas, verbos, DTOs, códigos de estado, `code` de error y mensajes **idénticos** al plan.
- [ ] `CancellationToken` propagado. Sin `.Result` ni `.Wait()`.
- [ ] Sin secretos, sin `TODO` y sin código comentado.
- [ ] Commits con el formato de la sección 11.5. Una Task, un commit.
- [ ] El build con `-warnaserror`, `dotnet format --verify-no-changes` y todos los tests pasan después del rebase sobre `main`.
- [ ] El PR enlaza su Issue y tiene la DoD marcada.

Si una regla no se cumple, el Supervisor pide cambios en el PR y no hace merge.

### 11.8 Ejecución secuencial (un solo agente)
Si solo hay un agente, ese agente hace de Supervisor e Implementador a la vez. El orden y las reglas son los mismos:
1. Ejecuta la Ola 0 completa en `main`, PBI por PBI, cada uno en su rama con `merge --no-ff`.
2. Para cada ola siguiente, en el orden de la tabla de la sección 12, toma un PBI y hace esto:
   - `git switch -c feature/<ID>-<slug> main` (o un worktree; el resultado es el mismo).
   - Implementa las Tasks, una por commit.
   - Verifica la DoD.
   - Hace merge a `main` con `git merge --no-ff`.
   - Borra la rama.
3. Al terminar cada ola, ejecuta el cierre de ola (sección 11.6).

El historial queda igual que en el modo paralelo, así que se puede pasar de un modo al otro en cualquier momento entre olas.

### 11.9 Ambigüedades durante la implementación
Si algo no está especificado en este plan:
1. Aplica la opción más simple que respete las convenciones de las secciones 3 a 6.
2. Escríbela en la sección "Decisiones" del cuerpo del PR, con el formato `<fecha> · <ID-PBI> · <decisión> · <motivo>`. **Los implementadores no editan `docs/decisions.md`**, para evitar conflictos. Al hacer merge, el Supervisor copia las decisiones aprobadas a ese archivo, en un commit `docs(platform): record decisions from <ID> [<PFX>-PLT-DOC]`.

El Supervisor la aprueba o la rechaza en la revisión. Nunca se inventan endpoints, campos ni reglas que no estén en el plan.


## 12. Olas de ejecución

| Ola | Modo | PBIs (en orden de merge) | Al cerrar la ola |
|---|---|---|---|
| 0 | Secuencial (Supervisor) | RST-PLT-01 → RST-PLT-02 → RST-PLT-03 → RST-PLT-04 → RST-PLT-05 → RST-PLT-06 | Migración `Wave0_Identity`, tag `v0.0.0` |
| 1 | Paralelo (4) | RST-USR-01, RST-USR-02, RST-ING-01, RST-TBL-01 | `Wave1_IngredientsTables`, `v0.1.0` |
| 2 | Paralelo (6) | RST-ING-02, RST-ING-03, RST-TBL-02, RST-TBL-03, RST-TBL-04, RST-DSH-01 | `Wave2_Dishes`, `v0.2.0` |
| 3 | Paralelo (5) | RST-DSH-02, RST-DSH-03, RST-DSH-04, RST-ING-04, RST-ORD-01 | `Wave3_Orders`, `v0.3.0` |
| 4 | Paralelo (3) | RST-ORD-02, RST-ORD-03, RST-ORD-04 | sin cambios de modelo (no se crea migración), `v0.4.0` |
| 5 | Secuencial (Supervisor) | RST-PLT-07 → RST-PLT-99 | `v1.0.0` |

**Grafo de dependencias** (una flecha significa "necesita lo mergeado de"):
```
Ola0 ──► USR-01, USR-02, ING-01, TBL-01
ING-01 ──► ING-02, ING-03, DSH-01          TBL-01 ──► TBL-02, TBL-03, TBL-04
DSH-01 ──► DSH-02, DSH-03, DSH-04, ING-04 (usa IDishRepository.AnyUsingIngredientAsync)
DSH-01 + TBL-01 ──► ORD-01 ──► ORD-02, ORD-03, ORD-04
```

## 13. Backlog: Features → PBIs → Tasks

Formato de cada PBI: ola, rama, historia, criterios en Gherkin, archivos exclusivos y Tasks (una Task es un commit). Las rutas de archivo son relativas a `src/` o `tests/`, según corresponda.

---

### Feature F0 · Plataforma (`scope: platform`)

#### RST-PLT-01 · Scaffolding de la solución
- **Ola** 0 · **Rama** `feature/RST-PLT-01-solution-scaffolding`
- **Historia:** Como equipo de desarrollo, queremos una solución base con convenciones compartidas para que todos los PBIs compilen igual.
- **Criterios:**
```gherkin
Escenario: La solución compila vacía
  Dado un clon limpio del repositorio
  Cuando ejecuto "dotnet build -warnaserror" y "dotnet test"
  Entonces ambos terminan con código 0
```
- **Tasks:**
  1. `chore(platform): add global.json, gitignore, editorconfig and build props [RST-PLT-01]`.
     - `global.json` según la sección 2.
     - `.gitignore` con `dotnet new gitignore`, más las entradas de la sección 4.1.
     - `.editorconfig` con `dotnet new editorconfig`, más estas reglas: `csharp_style_namespace_declarations = file_scoped:warning`, `csharp_style_prefer_primary_constructors = true:suggestion`, `dotnet_sort_system_directives_first = true`, `insert_final_newline = true` e indentación de 4 espacios.
     - `Directory.Build.props` según la convención 2.
  2. `chore(platform): add central package management [RST-PLT-01]`. `Directory.Packages.props` con `ManagePackageVersionsCentrally=true` y todos los paquetes de la sección 2, en versiones exactas.
  3. `chore(platform): create solution and projects [RST-PLT-01]`.
     - `dotnet new sln -n RestaurantApi`.
     - Domain, Application e Infrastructure con `dotnet new classlib`.
     - Api con `dotnet new web`.
     - Los 3 proyectos de test con `dotnet new nunit`.
     - Borrar `Class1.cs` y `UnitTest1.cs`.
     - Agregar las referencias de la sección 7.1 y los `PackageReference` sin versión.
  4. `docs(platform): add README skeleton and docs folder [RST-PLT-01]`. `docs/decisions.md` empieza con la decisión del modelo de trabajo en sala (sección 8.5).
     - `README.md` con los títulos de la sección 14.2, vacíos.
     - `docs/progress.md` con una tabla de todos los PBIs de este plan en estado `Pendiente`.
     - `docs/decisions.md` con su encabezado.
     - `docs/architecture.md` con sus títulos.

#### RST-PLT-02 · Building blocks de dominio y aplicación
- **Ola** 0 · **Rama** `feature/RST-PLT-02-building-blocks`
- **Historia:** Como desarrollador, quiero las abstracciones base para escribir agregados y casos de uso uniformes.
- **Criterios:**
```gherkin
Escenario: Money rechaza montos inválidos
  Cuando creo Money con -1 o con 10.555
  Entonces se lanza DomainException con code "Money.Negative" o "Money.TooManyDecimals"
Escenario: El decorador de validación bloquea comandos inválidos
  Dado un comando con un validator que falla
  Cuando se ejecuta el handler registrado
  Entonces se lanza ValidationException y el handler interno no se invoca
```
- **Archivos:**
  - `Restaurant.Domain/Common/*` (sección 8.1).
  - `Restaurant.Application/Abstractions/*` (sección 7.1, excepto `Roles.cs` y `Policies.cs`, que se crean aquí también).
  - `Restaurant.Application/DependencyInjection.cs`.
  - `Restaurant.Domain.Tests/Common/MoneyTests.cs`.
  - `Restaurant.Application.Tests/Abstractions/ValidationDecoratorTests.cs`.
- **Tasks:**
  1. `feat(platform): add domain building blocks [RST-PLT-02]`: AggregateRoot, Entity, DomainException, NotFoundException, ConflictException y Money.
  2. `test(platform): cover Money rules [RST-PLT-02]`.
  3. `feat(platform): add application abstractions [RST-PLT-02]`:
     - Interfaces de handlers, `Unit`, `ICurrentUser` (`Guid UserId`, `bool IsAuthenticated`) e `IUnitOfWork` (`Task<int> SaveChangesAsync(CancellationToken)`).
     - `ForbiddenException` y `UnauthorizedException`.
     - `PagedResponse<T>`, con `static Create(items, page, pageSize, totalCount)`, que calcula `TotalPages = ceil(total / pageSize)`.
     - `PagingRules`: método de extensión de FluentValidation `ApplyPaging()` con los mensajes `"page debe ser mayor o igual a 1."` y `"pageSize debe estar entre 1 y 100."`.
     - `Roles` y `Policies` (sección 7.4).
  4. `feat(platform): add validation decorator and handler scanning [RST-PLT-02]` (sección 7.2).
  5. `test(platform): cover validation decorator [RST-PLT-02]`.

#### RST-PLT-03 · Persistencia base, Docker y secretos
- **Ola** 0 · **Rama** `feature/RST-PLT-03-persistence-base`
- **Historia:** Como desarrollador, quiero el DbContext, la conexión configurada por secretos y la base en Docker, para persistir sin exponer credenciales.
- **Criterios:**
```gherkin
Escenario: Falta la connection string
  Dado que no existe "ConnectionStrings:Default"
  Cuando arranca la API
  Entonces falla con InvalidOperationException y el mensaje de la sección 4.1
Escenario: Docker levanta la base
  Dado un .env creado desde .env.example
  Cuando ejecuto "docker compose up -d db"
  Entonces el servicio db queda "healthy"
```
- **Archivos:**
  - `Restaurant.Infrastructure/Persistence/RestaurantDbContext.cs` y `DesignTimeDbContextFactory.cs`.
  - `Restaurant.Infrastructure/Identity/ApplicationUser.cs`.
  - `Restaurant.Infrastructure/DependencyInjection.cs`.
  - `docker-compose.yml`, `.env.example`, `Dockerfile`.
  - `Restaurant.Api/Restaurant.Api.csproj` (solo `UserSecretsId`).
- **Tasks:**
  1. `feat(platform): add RestaurantDbContext with identity schema [RST-PLT-03]`.
     - Sección 9.
     - Las tablas de Identity se renombran a `identity.users`, `identity.roles`, `identity.user_roles`, `identity.user_claims`, `identity.user_logins`, `identity.user_tokens` e `identity.role_claims`.
     - `ApplicationUser` según la sección 8.6.
  2. `feat(platform): register infrastructure services [RST-PLT-03]`. `AddInfrastructure(IConfiguration)` hace esto:
     - Lee `ConnectionStrings:Default` y falla rápido si no existe.
     - `UseNpgsql(cs, o => o.MigrationsAssembly(...))`, más `UseSnakeCaseNamingConvention()`.
     - Registra `IUnitOfWork` → `RestaurantDbContext`.
     - Escanea `*Repository` y `*Service` (sección 9).
     - Agrega `AddHealthChecks().AddDbContextCheck<RestaurantDbContext>()`.
  3. `feat(platform): add design-time DbContext factory [RST-PLT-03]` (sección 9).
  4. `chore(platform): add docker-compose, env example and Dockerfile [RST-PLT-03]`.
     - Servicio `db`: `postgres:17`, puerto `5432:5432`, volumen `restaurant-db-data`, `POSTGRES_DB=restaurant`, `POSTGRES_USER=restaurant_app`, `POSTGRES_PASSWORD=${POSTGRES_PASSWORD}`, healthcheck `pg_isready -U restaurant_app -d restaurant`.
     - Servicio `api`: `build: .`, puerto `8080:8080`, `depends_on: db (service_healthy)`, `ASPNETCORE_ENVIRONMENT=Development`.
       - `ConnectionStrings__Default=Host=db;Port=5432;Database=restaurant;Username=restaurant_app;Password=${POSTGRES_PASSWORD}`.
       - `Jwt__Key=${JWT_KEY}`, `Seed__AdminEmail=${SEED_ADMIN_EMAIL}`, `Seed__AdminPassword=${SEED_ADMIN_PASSWORD}`.
     - `.env.example` con esas 4 variables con valor `cambiar-esto`.
     - `Dockerfile` multi-stage: `sdk:10.0` para compilar y `aspnet:10.0` para correr, con `USER $APP_UID` y `EXPOSE 8080`.
  5. `chore(platform): set fixed UserSecretsId [RST-PLT-03]`. `<UserSecretsId>`, con el valor de la sección 4.1, en el csproj de Api.

#### RST-PLT-04 · API base e infraestructura de pruebas de integración
- **Ola** 0 · **Rama** `feature/RST-PLT-04-api-base`
- **Historia:** Como consumidor de la API, quiero errores uniformes, documentación y health check. Como desarrollador, quiero una factory de tests con PostgreSQL real.
- **Criterios:**
```gherkin
Escenario: Health check
  Cuando hago GET /health
  Entonces recibo 200 con el texto "Healthy"
Escenario: Mapeo de excepciones
  Dada cada excepción de la tabla de la sección 5
  Cuando la procesa GlobalExceptionHandler
  Entonces la respuesta tiene el status, code, title y detail de la tabla y un traceId
```
- **Archivos:**
  - `Restaurant.Api/Common/*` (sección 7.1).
  - `Restaurant.Api/Program.cs`, `appsettings.json`, `appsettings.Development.json`.
  - `Restaurant.Api.IntegrationTests/Common/{PostgresFixture.cs, ApiFactory.cs}`.
  - `Restaurant.Api.IntegrationTests/Platform/{HealthTests.cs, GlobalExceptionHandlerTests.cs}`.
- **Tasks:**
  1. `feat(platform): add endpoint discovery [RST-PLT-04]`.
     - `IEndpoint { void MapEndpoint(IEndpointRouteBuilder app); }`.
     - `AddEndpoints(Assembly)` registra cada implementación como `IEndpoint` transient.
     - `MapEndpoints(RouteGroupBuilder)` resuelve todas y llama a `MapEndpoint`.
  2. `feat(platform): add global exception handler and problem details [RST-PLT-04]`.
     - Sección 5. `UnauthorizedException` se mapea a 401 con su code y mensaje.
     - `AddProblemDetails(o => o.CustomizeProblemDetails = ctx => agrega traceId)`.
  3. `feat(platform): add OpenAPI, Scalar, JSON options, current user and time provider [RST-PLT-04]`.
     - `AddOpenApi`, con un document transformer que agrega el esquema `Bearer` (http, bearer, JWT) como requisito global.
     - `JsonStringEnumConverter`.
     - `CurrentUser` lee `sub` de `HttpContext.User`.
     - `TimeProvider.System` como singleton.
  4. `feat(platform): compose Program pipeline [RST-PLT-04]`. Orden del pipeline:
     - `UseExceptionHandler` → `UseStatusCodePages` → (si no es Development) `UseHsts` → `UseHttpsRedirection` → `UseRateLimiter` → `UseAuthentication` → `UseAuthorization`.
     - En Development: `MapOpenApi().AllowAnonymous()` y `MapScalarApiReference().AllowAnonymous()`. Sin esto, la `FallbackPolicy` exigiría un token para abrir la documentación.
     - `MapHealthChecks("/health").AllowAnonymous()`.
     - `MapGroup("/api/v1")` + `MapEndpoints`.
     - Al final del archivo: `public partial class Program;`.
     - Se registran `AddAuthentication()`, `AddAuthorization()` y `AddRateLimiter(o => o.RejectionStatusCode = 429)`, todavía sin esquemas ni políticas, para que `UseAuthentication`, `UseAuthorization` y `UseRateLimiter` no fallen. PLT-05 los completa.
  5. `test(platform): add Postgres fixture and ApiFactory [RST-PLT-04]`.
     - `PostgresFixture` (`[SetUpFixture]`) arranca un `PostgreSqlBuilder().WithImage("postgres:17")` una sola vez.
     - `ApiFactory(string? databaseName = null)`: si se pasa `databaseName`, crea esa base en el contenedor y la usa; si no, usa la base compartida `restaurant_tests`.
     - `ApiFactory` usa el entorno `Testing` e inyecta por `ConfigureAppConfiguration` en memoria: la connection string del contenedor, `Jwt:Key` aleatoria de 64 bytes, `Seed:AdminEmail` y `Seed:AdminPassword` aleatorios, `RateLimiting:AuthPermitLimit=1000` y `Database:ApplyMigrationsOnStartup=false`.
     - Quita el `IdentitySeeder` de los hosted services. Al crearse, ejecuta `EnsureCreatedAsync()` y luego `IdentitySeeder.SeedAsync()` manualmente (el seeder llega en PLT-05; mientras tanto, solo `EnsureCreatedAsync`).
     - Expone `ExecuteDbContextAsync(Func<RestaurantDbContext, Task>)`.
     - `TestNumbers.NextTableNumber()`: contador estático con `Interlocked.Increment`, que empieza en 1. Da números de mesa únicos en toda la corrida de tests (máximo 999) sobre la base compartida.
  6. `test(platform): cover health check and exception mapping [RST-PLT-04]`.

#### RST-PLT-05 · Autenticación JWT, roles, seed y login
- **Ola** 0 · **Rama** `feature/RST-PLT-05-authentication`
- **Historia:** Como usuario del restaurante, quiero iniciar sesión y recibir un token con mi rol, para usar solo lo que me corresponde.
- **Criterios:**
```gherkin
Escenario: Login correcto
  Dado el Admin sembrado
  Cuando hago POST /api/v1/auth/login con sus credenciales
  Entonces recibo 200 con accessToken, expiresAtUtc dentro de 60 minutos y roles ["Admin"]
Escenario: Credenciales inválidas (contraseña incorrecta, email inexistente o usuario inactivo)
  Cuando hago login
  Entonces recibo 401 con code "Auth.InvalidCredentials" y detail "Credenciales inválidas."
Escenario: Bloqueo
  Dado 5 intentos con contraseña incorrecta
  Cuando intento con la contraseña correcta
  Entonces recibo 401
Escenario: Rate limit
  Dado AuthPermitLimit = 5
  Cuando hago 6 POST /auth/login en menos de 1 minuto
  Entonces la sexta responde 429
Escenario: Token de usuario desactivado
  Dado un token válido de un usuario que luego se marca IsActive = false
  Cuando hago GET /api/v1/auth/me
  Entonces recibo 401
```
- **Archivos:**
  - `Restaurant.Infrastructure/Identity/{JwtOptions, JwtTokenGenerator, LoginService, IdentitySeeder, DatabaseOptions}.cs`.
  - `Restaurant.Infrastructure/DependencyInjection.cs`.
  - `Restaurant.Application/Identity/{ILoginService, LoginResponse, CurrentUserResponse}.cs` y `Restaurant.Application/Identity/Login/{LoginCommand, LoginValidator, LoginHandler}.cs`.
  - `Restaurant.Api/Endpoints/Auth/LoginEndpoint.cs`, `Program.cs`, `appsettings*.json`.
  - `Restaurant.Api.IntegrationTests/Common/TestAuth.cs` y `Restaurant.Api.IntegrationTests/Auth/LoginTests.cs`.
  - `Restaurant.Api.IntegrationTests/Common/ApiFactory.cs`: agrega quitar `IdentitySeeder` de los hosted services y llamar a `SeedAsync()`.
  - `Restaurant.Application/Identity/GetCurrentUser/{GetCurrentUserQuery, GetCurrentUserHandler}.cs`, que usa `ILoginService.GetCurrentAsync`.
  - `Restaurant.Api/Endpoints/Auth/MeEndpoint.cs` (`GET /api/v1/auth/me`).
- **Tasks:**
  1. `feat(auth): configure identity with password and lockout policies [RST-PLT-05]` (sección 4.2).
  2. `feat(auth): add JWT options, token generator and bearer validation [RST-PLT-05]`.
     - `JwtOptions { [Required] Issuer, [Required] Audience, [Required, MinLength(64)] Key, [Range(5,1440)] ExpiresMinutes = 60 }`.
     - En `appsettings.json`: `Jwt:Issuer = "RestaurantApi"`, `Jwt:Audience = "RestaurantApi.Clients"`, `Jwt:ExpiresMinutes = 60`.
     - Validación y `OnTokenValidated` según la sección 4.2.
     - Políticas `Admin`, `Waiter` y `Staff`, más la `FallbackPolicy`.
  3. `feat(auth): add idempotent identity seeder [RST-PLT-05]`.
     - `DatabaseOptions.ApplyMigrationsOnStartup`: `true` en `appsettings.Development.json` y `false` en `appsettings.json`.
     - `SeedAsync()`: si la opción está activa, ejecuta `MigrateAsync()`. Luego crea los roles y el Admin (con `FirstName = "Admin"` y `LastName = "Sistema"`) si no existen.
  4. `feat(auth): add login use case and endpoint with rate limiting [RST-PLT-05]`.
     - Política `auth` con `PermitLimit` tomado de `RateLimiting:AuthPermitLimit` (5 en `appsettings.json`), ventana de 1 minuto y partición por `RemoteIpAddress` (o `"unknown"`). Rechazo con 429 y ProblemDetails.
     - El validator exige `email` con formato válido y `password` no vacío.
  5. `test(auth): add TestAuth helper [RST-PLT-05]`.
     - `CreateClientAsync(ApiFactory, string role)` crea un usuario con email `"{guid}@test.local"`, la contraseña `"Test-Passw0rd!"` y el rol indicado.
     - Obtiene el token llamando a `/auth/login` y devuelve el `HttpClient` con el header `Authorization: Bearer`.
     - `CreateAnonymousClient(ApiFactory)`.
  6. `feat(auth): add current user endpoint [RST-PLT-05]`.
     - `GET /api/v1/auth/me` requiere estar autenticado (cualquier rol) y devuelve `CurrentUserResponse(Guid UserId, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles)`.
     - `ILoginService.GetCurrentAsync(Guid userId, ct)` va en la misma interfaz `ILoginService`.
  7. `test(auth): cover login and me scenarios [RST-PLT-05]`.
     - Todos los escenarios de arriba. El de rate limit usa `factory.WithWebHostBuilder` con `AuthPermitLimit=5`. El de usuario desactivado usa `GET /auth/me`.
     - Además: `GET /auth/me` sin token responde 401 y con token responde 200 con los datos del usuario.

#### RST-PLT-06 · CI, plantillas de GitHub y test de migraciones
- **Ola** 0 · **Rama** `feature/RST-PLT-06-ci-github`
- **Criterios:**
```gherkin
Escenario: CI en PR
  Cuando se abre un PR contra main
  Entonces GitHub Actions ejecuta format, build -warnaserror y test, y el check queda verde
```
- **Archivos:**
  - `.github/workflows/ci.yml`.
  - `.github/ISSUE_TEMPLATE/{feature.yml, pbi.yml, bug.yml}` y `.github/pull_request_template.md`.
  - `Restaurant.Api.IntegrationTests/Common/MigrationsTests.cs`.
  - `docs/progress.md`.
- **Tasks:**
  1. `ci(platform): add build and test workflow [RST-PLT-06]`.
     - Se dispara con `push` a `main` y con `pull_request`. Corre en `ubuntu-latest`.
     - Pasos: `actions/checkout@v4` → `actions/setup-dotnet@v4` (`global-json-file: global.json`) → `dotnet restore` → `dotnet format --verify-no-changes --no-restore` → `dotnet build -c Release --no-restore -warnaserror` → `dotnet test -c Release --no-build`.
  2. `chore(platform): add issue and pull request templates [RST-PLT-06]`.
     - `pbi.yml`: campos ID, historia, criterios Gherkin, tasks y dependencias.
     - El template de PR lleva `Closes #`, el ID del plan, el checklist de la sección 14.1 y una sección de decisiones.
  3. `test(platform): add migrations smoke test [RST-PLT-06]`. Crea una base nueva en el contenedor (`CREATE DATABASE`), aplica `MigrateAsync()` y verifica que `GetPendingMigrationsAsync()` quede vacío.
  4. `docs(platform): register GitHub issues mapping [RST-PLT-06]`.
     - Solo si `gh` está autenticado: crea los labels `feature`, `pbi` y `bug`, los Milestones `Ola 0` a `Ola 5`, un Issue por Feature y por PBI, y el Project.
     - Completa `docs/progress.md`. Si no hay `gh`, deja la columna de issue en `—`.

#### RST-PLT-07 · Cierre: README, `.http` y diagramas
- **Ola** 5 · **Rama** `feature/RST-PLT-07-docs-closure`
- **Archivos:** `README.md`, `docs/architecture.md`, `src/Restaurant.Api/Restaurant.Api.http`, `docs/progress.md`.
- **Tasks:**
  1. `docs(platform): add end-to-end http requests [RST-PLT-07]`.
     - Variables `@baseUrl = http://localhost:8080/api/v1`, `@adminToken` y `@waiterToken`.
     - Flujo: login de Admin → crear Waiter → crear 2 ingredientes → crear plato → crear mesa → login de Waiter → abrir orden → agregar item → quitar item → completar → liberar mesa → listar órdenes.
     - Incluye también un ejemplo de cada error 409 y 422.
  2. `docs(platform): add architecture diagrams [RST-PLT-07]`. Diagramas Mermaid: dependencias entre capas, `stateDiagram` de Table y de Order, `erDiagram` de las tablas de la sección 9 y `sequenceDiagram` de "abrir orden".
  3. `docs(platform): complete README [RST-PLT-07]`. Todos los títulos de la sección 14.2, con badge del CI y tabla de endpoints (copiada de la sección 10).

#### RST-PLT-99 · Verificación final
- **Ola** 5, después de PLT-07. El Supervisor ejecuta la sección 14.2 completa. Si algo falla, crea un PBI `RST-FIX-NN`.

---

### Feature F1 · Usuarios (`scope: users`)

#### RST-USR-01 · Crear, listar y consultar usuarios
- **Ola** 1 · **Rama** `feature/RST-USR-01-create-and-query-users`
- **Historia:** Como **Admin**, quiero crear usuarios con rol Admin o Waiter y consultarlos, para controlar quién usa el sistema.
- **Criterios:**
```gherkin
Escenario: Crear mesero
  Dado un Admin autenticado
  Cuando hago POST /api/v1/users con un email nuevo, nombre, apellido, contraseña válida y role "Waiter"
  Entonces recibo 201 con UserResponse, role "Waiter", isActive true y header Location /api/v1/users/{id}
Escenario: Email duplicado
  Cuando creo un usuario con un email existente
  Entonces recibo 409 con code "User.DuplicateEmail"
Escenario: Datos inválidos
  Cuando role es "Chef" o la contraseña es "123"
  Entonces recibo 400 con errores en "role" o "password"
Escenario: Un mesero no puede crear usuarios
  Dado un Waiter autenticado
  Cuando hago POST /api/v1/users
  Entonces recibo 403
Escenario: Listar con filtros
  Cuando hago GET /api/v1/users?role=Waiter&isActive=true
  Entonces solo recibo meseros activos, ordenados por email
Escenario: Usuario inexistente
  Cuando hago GET /api/v1/users/{id-aleatorio}
  Entonces recibo 404 con code "User.NotFound"
```
- **Archivos:**
  - `Application/Identity/{IUserManagementService, UserResponse}.cs`.
  - `Application/Identity/CreateUser/{CreateUserCommand, CreateUserValidator, CreateUserHandler}.cs`.
  - `Application/Identity/ListUsers/{ListUsersQuery, ListUsersValidator, ListUsersHandler}.cs`.
  - `Application/Identity/GetUser/{GetUserQuery, GetUserHandler}.cs`.
  - `Infrastructure/Identity/UserManagementService.cs`.
  - `Api/Endpoints/Users/{CreateUserEndpoint, ListUsersEndpoint, GetUserEndpoint}.cs`.
  - `Application.Tests/Identity/CreateUserHandlerTests.cs` e `IntegrationTests/Users/{CreateUserTests, ListUsersTests, GetUserTests}.cs`.
- **Tasks:**
  1. `feat(users): add user management contract and response [RST-USR-01]`.
  2. `feat(users): implement user management service [RST-USR-01]`.
     - `CreateAsync`:
       - Si el email ya existe, lanza `ConflictException("User.DuplicateEmail", ...)`.
       - El servicio inyecta `RestaurantDbContext` y abre `dbContext.Database.BeginTransactionAsync(ct)`. Dentro de esa transacción, hace `CreateAsync(user, password)`. Si falla, lanza `ValidationException` con los errores de Identity en el campo `password`.
       - Luego `AddToRoleAsync` y commit.
     - `ListAsync` filtra por rol y estado. `GetAsync` lanza `NotFoundException("User.NotFound", ...)`.
  3. `feat(users): add create user use case [RST-USR-01]`.
     - Validator: `email` con formato válido y máximo 256; `firstName` y `lastName` de 2 a 50; `password` con la política de la sección 4.2 y mensaje `"La contraseña debe tener al menos 10 caracteres e incluir mayúscula, minúscula, número y símbolo."`; `role` ∈ Roles (`"El rol debe ser Admin o Waiter."`).
     - Test unitario del handler.
  4. `feat(users): add list and get user queries [RST-USR-01]`. El query de lista valida que `role`, si viene, sea un rol válido.
  5. `feat(users): expose user endpoints [RST-USR-01]`. Política `Admin`.
  6. `test(users): cover user endpoints [RST-USR-01]`.

#### RST-USR-02 · Activar y desactivar usuarios
- **Ola** 1 · **Rama** `feature/RST-USR-02-user-activation`
- **Historia:** Como **Admin**, quiero desactivar y reactivar usuarios, para quitar el acceso inmediatamente a quien ya no trabaja aquí.
- **Criterios:**
```gherkin
Escenario: Desactivar mesero
  Dado un Waiter con token vigente
  Cuando el Admin hace PATCH /api/v1/users/{id}/deactivate
  Entonces recibo 204
  Y el token del Waiter recibe 401 en su siguiente solicitud
  Y su login devuelve 401 "Auth.InvalidCredentials"
Escenario: No puede desactivarse a sí mismo
  Cuando el Admin desactiva su propio id
  Entonces recibo 422 con code "User.CannotDeactivateSelf"
Escenario: Reactivar
  Cuando el Admin hace PATCH /api/v1/users/{id}/activate sobre un usuario inactivo
  Entonces recibo 204 y el usuario puede iniciar sesión de nuevo
Escenario: Usuario inexistente
  Entonces recibo 404 "User.NotFound"
```
- **Archivos:**
  - `Application/Identity/IUserActivationService.cs`.
  - `Application/Identity/SetUserActive/{SetUserActiveCommand, SetUserActiveValidator, SetUserActiveHandler}.cs`.
  - `Infrastructure/Identity/UserActivationService.cs`.
  - `Api/Endpoints/Users/{DeactivateUserEndpoint, ActivateUserEndpoint}.cs`.
  - `Application.Tests/Identity/SetUserActiveHandlerTests.cs` e `IntegrationTests/Users/UserActivationTests.cs`.
- **Tasks:**
  1. `feat(users): add user activation contract and service [RST-USR-02]`. Si el usuario no existe, `NotFoundException("User.NotFound", ...)`. Si existe, asigna `IsActive` y llama a `UpdateAsync`.
  2. `feat(users): add set user active use case [RST-USR-02]`. Si `!command.IsActive && command.UserId == currentUser.UserId`, lanza `DomainException("User.CannotDeactivateSelf", ...)`. Test unitario.
  3. `feat(users): expose activate and deactivate endpoints [RST-USR-02]`. Responden 204 (`TypedResults.NoContent()`).
  4. `test(users): cover user activation [RST-USR-02]`.

---

### Feature F2 · Ingredientes (`scope: ingredients`)

#### RST-ING-01 · Modelo Ingredient y alta
- **Ola** 1 · **Rama** `feature/RST-ING-01-create-ingredient`
- **Historia:** Como **Admin**, quiero registrar ingredientes para asignarlos a los platos.
- **Criterios:**
```gherkin
Escenario: Crear ingrediente
  Cuando el Admin hace POST /api/v1/ingredients con name "  Tomate  "
  Entonces recibo 201 con name "Tomate"
Escenario: Nombre duplicado sin importar mayúsculas
  Dado el ingrediente "Tomate"
  Cuando creo "TOMATE"
  Entonces recibo 409 con code "Ingredient.DuplicateName"
Escenario: Nombre inválido en dominio
  Cuando creo "A"
  Entonces recibo 422 con code "Ingredient.InvalidName"
Escenario: Sin rol Admin
  Dado un Waiter
  Entonces recibo 403
```
- **Archivos:**
  - `Domain/Ingredients/{Ingredient, IngredientName, IngredientErrors, IIngredientRepository}.cs` (sección 8.2 completa).
  - `Application/Ingredients/IngredientResponse.cs` y `Application/Ingredients/CreateIngredient/*`.
  - `Infrastructure/Persistence/Configurations/IngredientConfiguration.cs` e `Infrastructure/Persistence/Repositories/IngredientRepository.cs` (todos los métodos de 8.2).
  - `Api/Endpoints/Ingredients/CreateIngredientEndpoint.cs`.
  - `Domain.Tests/Ingredients/IngredientTests.cs`, `Application.Tests/Ingredients/CreateIngredientHandlerTests.cs` e `IntegrationTests/Ingredients/CreateIngredientTests.cs`.
- **Tasks:**
  1. `feat(ingredients): add Ingredient aggregate and value object [RST-ING-01]`.
  2. `test(ingredients): cover Ingredient rules [RST-ING-01]`.
  3. `feat(ingredients): add repository and EF configuration [RST-ING-01]`.
  4. `feat(ingredients): add create ingredient use case [RST-ING-01]`. Es el ejemplo de la sección 7.3, con su test unitario.
  5. `feat(ingredients): expose create ingredient endpoint [RST-ING-01]`.
  6. `test(ingredients): cover create ingredient endpoint [RST-ING-01]`.

#### RST-ING-02 · Listar y consultar ingredientes
- **Ola** 2 · **Rama** `feature/RST-ING-02-query-ingredients` · **Depende de** ING-01
- **Criterios:**
```gherkin
Escenario: Listar
  Dado los ingredientes "Queso" y "Ajo"
  Cuando un usuario Staff hace GET /api/v1/ingredients
  Entonces recibo 200 con la lista ordenada por nombre ("Ajo" antes que "Queso")
Escenario: Consultar inexistente
  Cuando hago GET /api/v1/ingredients/{id-aleatorio}
  Entonces recibo 404 con code "Ingredient.NotFound"
```
- **Archivos:** `Application/Ingredients/{ListIngredients, GetIngredient}/*`, `Api/Endpoints/Ingredients/{ListIngredientsEndpoint, GetIngredientEndpoint}.cs`, `IntegrationTests/Ingredients/QueryIngredientsTests.cs`.
- **Tasks:**
  1. `feat(ingredients): add list and get queries [RST-ING-02]`.
  2. `feat(ingredients): expose query endpoints [RST-ING-02]`. Política `Staff`.
  3. `test(ingredients): cover query endpoints [RST-ING-02]`.

#### RST-ING-03 · Renombrar ingrediente
- **Ola** 2 · **Rama** `feature/RST-ING-03-rename-ingredient` · **Depende de** ING-01
- **Criterios:**
```gherkin
Escenario: Renombrar
  Cuando el Admin hace PUT /api/v1/ingredients/{id} con name "Tomate cherry"
  Entonces recibo 200 con el nuevo nombre
Escenario: Renombrar al mismo nombre propio no es duplicado
  Cuando envío el mismo nombre en distinta capitalización
  Entonces recibo 200
Escenario: Duplicado de otro ingrediente
  Entonces recibo 409 "Ingredient.DuplicateName"
Escenario: Inexistente
  Entonces recibo 404 "Ingredient.NotFound"
```
- **Archivos:** `Application/Ingredients/RenameIngredient/*`, `Api/Endpoints/Ingredients/RenameIngredientEndpoint.cs` (con `RenameIngredientRequest(string Name)` en el mismo archivo), `Application.Tests/Ingredients/RenameIngredientHandlerTests.cs`, `IntegrationTests/Ingredients/RenameIngredientTests.cs`.
- **Tasks:**
  1. `feat(ingredients): add rename ingredient use case [RST-ING-03]`. Usa `ExistsByNormalizedNameAsync(normalized, excludeId: id)`. Con test unitario.
  2. `feat(ingredients): expose rename endpoint [RST-ING-03]`.
  3. `test(ingredients): cover rename endpoint [RST-ING-03]`.

#### RST-ING-04 · Eliminar ingrediente no usado
- **Ola** 3 · **Rama** `feature/RST-ING-04-delete-ingredient` · **Depende de** DSH-01
- **Criterios:**
```gherkin
Escenario: Eliminar ingrediente libre
  Cuando el Admin hace DELETE /api/v1/ingredients/{id} de un ingrediente sin platos
  Entonces recibo 204 y GET /api/v1/ingredients/{id} devuelve 404
Escenario: Ingrediente en uso
  Dado un plato que usa el ingrediente
  Cuando lo elimino
  Entonces recibo 409 con code "Ingredient.InUse"
```
- **Archivos:** `Application/Ingredients/DeleteIngredient/*`, `Api/Endpoints/Ingredients/DeleteIngredientEndpoint.cs`, `Application.Tests/Ingredients/DeleteIngredientHandlerTests.cs`, `IntegrationTests/Ingredients/DeleteIngredientTests.cs`.
- **Tasks:**
  1. `feat(ingredients): add delete ingredient use case [RST-ING-04]`. Si no existe, 404. Si `IDishRepository.AnyUsingIngredientAsync` es verdadero, `IngredientErrors.InUse()`. Si no, `Remove` y guardar. Con test unitario.
  2. `feat(ingredients): expose delete endpoint [RST-ING-04]`.
  3. `test(ingredients): cover delete endpoint [RST-ING-04]`. El plato del escenario se inserta directo con el `DbContext`.

---

### Feature F3 · Mesas (`scope: tables`)

#### RST-TBL-01 · Modelo Table y alta
- **Ola** 1 · **Rama** `feature/RST-TBL-01-create-table`
- **Historia:** Como **Admin**, quiero registrar las mesas del local con su número y capacidad.
- **Criterios:**
```gherkin
Escenario: Crear mesa
  Cuando el Admin hace POST /api/v1/tables con number 5, capacity 4, description "Terraza"
  Entonces recibo 201 con status "Available"
Escenario: Número duplicado
  Entonces recibo 409 "Table.DuplicateNumber"
Escenario: Capacidad inválida
  Cuando capacity es 0 o 21
  Entonces recibo 400 (validator) con error en "capacity"
Escenario de dominio: transiciones
  Dado una mesa Available
  Cuando llamo MarkServed()
  Entonces se lanza DomainException "Table.InvalidTransition"
```
- **Archivos:**
  - `Domain/Tables/*` (sección 8.4 completa, incluidos `StartService`, `MarkServed`, `Release` y `UpdateDetails`).
  - `Application/Tables/TableResponse.cs` y `Application/Tables/CreateTable/*`.
  - `Infrastructure/Persistence/Configurations/TableConfiguration.cs` (con `RowVersion`) e `Infrastructure/Persistence/Repositories/TableRepository.cs`.
  - `Api/Endpoints/Tables/CreateTableEndpoint.cs`.
  - `Domain.Tests/Tables/TableTests.cs` (todas las transiciones válidas e inválidas), `Application.Tests/Tables/CreateTableHandlerTests.cs` e `IntegrationTests/Tables/CreateTableTests.cs`.
- **Tasks:**
  1. `feat(tables): add Table aggregate with status transitions [RST-TBL-01]`.
  2. `test(tables): cover Table rules and transitions [RST-TBL-01]`.
  3. `feat(tables): add repository and EF configuration [RST-TBL-01]`.
  4. `feat(tables): add create table use case [RST-TBL-01]`. Validator: `number` de 1 a 999, `capacity` de 1 a 20 y `description` de máximo 100. Con test unitario.
  5. `feat(tables): expose create table endpoint [RST-TBL-01]`.
  6. `test(tables): cover create table endpoint [RST-TBL-01]`.

#### RST-TBL-02 · Listar y consultar mesas
- **Ola** 2 · **Rama** `feature/RST-TBL-02-query-tables` · **Depende de** TBL-01
- **Criterios:**
```gherkin
Escenario: Filtrar por estado
  Cuando hago GET /api/v1/tables?status=Available
  Entonces solo recibo mesas Available, ordenadas por número
Escenario: Estado inválido en query
  Cuando status es "Ocupada"
  Entonces recibo 400
Escenario: Inexistente
  Entonces GET /api/v1/tables/{id} devuelve 404 "Table.NotFound"
```
- **Archivos:** `Application/Tables/{ListTables, GetTable}/*`, `Api/Endpoints/Tables/{ListTablesEndpoint, GetTableEndpoint}.cs`, `IntegrationTests/Tables/QueryTablesTests.cs`.
- **Tasks:**
  1. `feat(tables): add list and get queries [RST-TBL-02]`.
  2. `feat(tables): expose query endpoints [RST-TBL-02]`.
  3. `test(tables): cover query endpoints [RST-TBL-02]`.

#### RST-TBL-03 · Actualizar detalles de mesa
- **Ola** 2 · **Rama** `feature/RST-TBL-03-update-table` · **Depende de** TBL-01
- **Criterios:**
```gherkin
Escenario: Actualizar mesa disponible
  Cuando el Admin hace PUT /api/v1/tables/{id} con capacity 6 y description ""
  Entonces recibo 200 con capacity 6 y description null
Escenario: Mesa en servicio
  Dado una mesa InService
  Cuando la actualizo
  Entonces recibo 422 "Table.NotAvailable"
```
- **Archivos:** `Application/Tables/UpdateTable/*`, `Api/Endpoints/Tables/UpdateTableEndpoint.cs`, `Application.Tests/Tables/UpdateTableHandlerTests.cs`, `IntegrationTests/Tables/UpdateTableTests.cs`.
- **Tasks:**
  1. `feat(tables): add update table use case [RST-TBL-03]`.
  2. `feat(tables): expose update endpoint [RST-TBL-03]`.
  3. `test(tables): cover update endpoint [RST-TBL-03]`.

#### RST-TBL-04 · Liberar mesa atendida
- **Ola** 2 · **Rama** `feature/RST-TBL-04-release-table` · **Depende de** TBL-01
- **Historia:** Como **Waiter**, quiero liberar una mesa ya atendida para que pueda recibir clientes nuevos.
- **Criterios:**
```gherkin
Escenario: Liberar mesa Served
  Dado una mesa Served
  Cuando un usuario Staff hace PATCH /api/v1/tables/{id}/release
  Entonces recibo 200 con status "Available"
Escenario: Mesa no atendida
  Dado una mesa InService
  Entonces recibo 422 "Table.InvalidTransition"
```
- **Archivos:** `Application/Tables/ReleaseTable/*`, `Api/Endpoints/Tables/ReleaseTableEndpoint.cs`, `Application.Tests/Tables/ReleaseTableHandlerTests.cs`, `IntegrationTests/Tables/ReleaseTableTests.cs`. Los datos de prueba se insertan con el `DbContext`, llamando a `StartService()` y `MarkServed()` antes de guardar.
- **Tasks:**
  1. `feat(tables): add release table use case [RST-TBL-04]`.
  2. `feat(tables): expose release endpoint [RST-TBL-04]`. Política `Staff`.
  3. `test(tables): cover release endpoint [RST-TBL-04]`.

---

### Feature F4 · Platos (`scope: dishes`)

#### RST-DSH-01 · Modelo Dish y alta
- **Ola** 2 · **Rama** `feature/RST-DSH-01-create-dish` · **Depende de** ING-01
- **Historia:** Como **Admin**, quiero crear platos con precio, porciones, categoría e ingredientes.
- **Criterios:**
```gherkin
Escenario: Crear plato
  Dado los ingredientes "Arroz" y "Pollo"
  Cuando el Admin hace POST /api/v1/dishes con name "Locrio", price 450.00, servings 1, category "MainCourse" e ingredientIds de ambos
  Entonces recibo 201 con isActive true y los 2 ingredientes ordenados por nombre
Escenario: Ingrediente inexistente
  Cuando un ingredientId no existe
  Entonces recibo 404 "Ingredient.NotFound" con ese id en el mensaje
Escenario: Nombre duplicado
  Entonces recibo 409 "Dish.DuplicateName"
Escenario: Precio con 3 decimales
  Entonces recibo 422 "Money.TooManyDecimals"
Escenarios de dominio: IngredientsRequired, TooManyIngredients, InvalidServings, Update sobre un plato inactivo (Dish.Inactive), ToOrderLine sobre un plato inactivo (Dish.Inactive)
```
- **Archivos:**
  - `Domain/Dishes/*` (sección 8.3 completa, incluidos `Update`, `Activate`, `Deactivate` y `ToOrderLine`).
  - `Domain/Orders/OrderLine.cs`. **Excepción:** lo crea este PBI porque `ToOrderLine` lo necesita; ORD-01 no lo modifica.
  - `Application/Dishes/DishResponse.cs` y `Application/Dishes/CreateDish/*`.
  - `Infrastructure/Persistence/Configurations/DishConfiguration.cs`: `OwnsMany(Ingredients)` en la tabla `dish_ingredients`, con clave (`DishId`, `IngredientId`), `HasOne<Ingredient>().WithMany().HasForeignKey(IngredientId).OnDelete(Restrict)` y acceso por campo.
  - `Infrastructure/Persistence/Repositories/DishRepository.cs` (todos los métodos de 8.3).
  - `Api/Endpoints/Dishes/CreateDishEndpoint.cs`.
  - `Domain.Tests/Dishes/DishTests.cs`, `Application.Tests/Dishes/CreateDishHandlerTests.cs` e `IntegrationTests/Dishes/CreateDishTests.cs`.
- **Tasks:**
  1. `feat(dishes): add Dish aggregate and OrderLine value object [RST-DSH-01]`.
  2. `test(dishes): cover Dish rules [RST-DSH-01]`.
  3. `feat(dishes): add repository and EF configuration [RST-DSH-01]`.
  4. `feat(dishes): add create dish use case [RST-DSH-01]`. Orden del handler: cargar los ingredientes con `GetByIdsAsync`; si falta alguno, `IngredientErrors.NotFound(primerIdFaltante)`; luego `Dish.Create`; luego el chequeo de duplicado; luego `Add` y guardar. Validator: `name` no vacío y de máximo 80; `price` > 0; `servings` de 1 a 20; `category` es un enum válido; `ingredientIds` no vacío y sin `Guid.Empty`.
  5. `feat(dishes): expose create dish endpoint [RST-DSH-01]`.
  6. `test(dishes): cover create dish endpoint [RST-DSH-01]`.

#### RST-DSH-02 · Listar y consultar platos
- **Ola** 3 · **Rama** `feature/RST-DSH-02-query-dishes` · **Depende de** DSH-01
- **Criterios:**
```gherkin
Escenario: Filtrar
  Cuando hago GET /api/v1/dishes?category=Dessert&isActive=true
  Entonces solo recibo postres activos, cada uno con sus ingredientes (id y name)
Escenario: Inexistente
  Entonces GET /api/v1/dishes/{id} devuelve 404 "Dish.NotFound"
```
- **Archivos:** `Application/Dishes/{ListDishes, GetDish}/*`, `Api/Endpoints/Dishes/{ListDishesEndpoint, GetDishEndpoint}.cs`, `IntegrationTests/Dishes/QueryDishesTests.cs`.
- **Tasks:**
  1. `feat(dishes): add list and get queries [RST-DSH-02]`. Los nombres de ingredientes se resuelven con una sola llamada a `GetByIdsAsync` con todos los ids.
  2. `feat(dishes): expose query endpoints [RST-DSH-02]`.
  3. `test(dishes): cover query endpoints [RST-DSH-02]`.

#### RST-DSH-03 · Actualizar plato
- **Ola** 3 · **Rama** `feature/RST-DSH-03-update-dish` · **Depende de** DSH-01
- **Criterios:**
```gherkin
Escenario: Actualizar
  Cuando el Admin hace PUT /api/v1/dishes/{id} con nuevo precio e ingredientes
  Entonces recibo 200 con los datos nuevos
Escenario: Plato inactivo
  Entonces recibo 422 "Dish.Inactive"
Escenario: Nombre de otro plato
  Entonces recibo 409 "Dish.DuplicateName"
```
- **Archivos:** `Application/Dishes/UpdateDish/*`, `Api/Endpoints/Dishes/UpdateDishEndpoint.cs` (con `UpdateDishRequest` en el mismo archivo), `Application.Tests/Dishes/UpdateDishHandlerTests.cs`, `IntegrationTests/Dishes/UpdateDishTests.cs`.
- **Tasks:**
  1. `feat(dishes): add update dish use case [RST-DSH-03]`.
  2. `feat(dishes): expose update endpoint [RST-DSH-03]`.
  3. `test(dishes): cover update endpoint [RST-DSH-03]`.

#### RST-DSH-04 · Activar y desactivar plato
- **Ola** 3 · **Rama** `feature/RST-DSH-04-dish-availability` · **Depende de** DSH-01
- **Criterios:**
```gherkin
Escenario: Desactivar
  Cuando el Admin hace PATCH /api/v1/dishes/{id}/deactivate
  Entonces recibo 204 y GET devuelve isActive false
Escenario: Idempotencia
  Cuando desactivo dos veces
  Entonces ambas responden 204
```
- **Archivos:** `Application/Dishes/SetDishAvailability/{SetDishAvailabilityCommand, SetDishAvailabilityHandler}.cs`, `Api/Endpoints/Dishes/{DeactivateDishEndpoint, ActivateDishEndpoint}.cs`, `IntegrationTests/Dishes/DishAvailabilityTests.cs`.
- **Tasks:**
  1. `feat(dishes): add set availability use case [RST-DSH-04]`.
  2. `feat(dishes): expose activate and deactivate endpoints [RST-DSH-04]`.
  3. `test(dishes): cover availability endpoints [RST-DSH-04]`.

---

### Feature F5 · Órdenes (`scope: orders`)

#### RST-ORD-01 · Modelo Order y abrir orden
- **Ola** 3 · **Rama** `feature/RST-ORD-01-open-order` · **Depende de** DSH-01 y TBL-01
- **Historia:** Como **Waiter**, quiero abrir una orden en una mesa disponible con los platos pedidos, y que el sistema calcule el subtotal.
- **Criterios:**
```gherkin
Escenario: Abrir orden
  Dado la mesa 3 Available y el plato "Locrio" (450.00) activo
  Cuando el Waiter hace POST /api/v1/orders con tableId de la mesa 3 e items [{ dishId: Locrio, quantity: 2 }]
  Entonces recibo 201 con status "InProgress", subtotal 900.00, tableNumber 3 y waiterId igual al id del token
  Y la mesa 3 queda "InService"
Escenario: Mesa no disponible
  Dado la mesa 3 InService
  Entonces recibo 422 "Table.InvalidTransition"
Escenario: Plato inactivo
  Entonces recibo 422 "Dish.Inactive"
Escenario: Plato o mesa inexistente
  Entonces recibo 404 "Dish.NotFound" o "Table.NotFound"
Escenario: Platos repetidos se combinan
  Cuando envío el mismo dishId con quantity 1 y quantity 2
  Entonces la orden tiene 1 item con quantity 3
Escenario: Admin no abre órdenes
  Dado un Admin
  Entonces recibo 403
Escenarios de dominio: ItemsRequired, InvalidQuantity (51), NotInProgress en AddItem/RemoveItem/Complete, CannotRemoveLastItem, ItemNotFound, recálculo del subtotal en cada operación
```
- **Archivos:**
  - `Domain/Orders/{Order, OrderItem, OrderStatus, OrderErrors, IOrderRepository}.cs` (sección 8.5 completa, incluidos `AddItem`, `RemoveItem` y `Complete`).
  - `Application/Orders/{OrderResponse, OrderItemResponse, OrderSummaryResponse}.cs` y `Application/Orders/OpenOrder/*`.
  - `Infrastructure/Persistence/Configurations/OrderConfiguration.cs`: `OwnsMany(Items)` en la tabla `order_items` con clave `Id`; FK a dish (Restrict); FK a table y a waiter (Restrict); índice único parcial `HasFilter("status = 'InProgress'")`; `RowVersion`.
  - `Infrastructure/Persistence/Repositories/OrderRepository.cs`.
  - `Api/Endpoints/Orders/OpenOrderEndpoint.cs`.
  - `Domain.Tests/Orders/OrderTests.cs`, `Application.Tests/Orders/OpenOrderHandlerTests.cs` e `IntegrationTests/Orders/OpenOrderTests.cs`.
- **Tasks:**
  1. `feat(orders): add Order aggregate with item rules [RST-ORD-01]`.
  2. `test(orders): cover Order rules [RST-ORD-01]`.
  3. `feat(orders): add repository and EF configuration [RST-ORD-01]`.
  4. `feat(orders): add open order use case [RST-ORD-01]`. Orden del handler:
     - (1) Cargar la mesa; si no existe, 404.
     - (2) Cargar los platos con `GetByIdsAsync`; si falta alguno, `DishErrors.NotFound(id)`.
     - (3) `lines = items.Select(i => dish.ToOrderLine(i.Quantity))`.
     - (4) `table.StartService()`.
     - (5) `Order.Open(table.Id, currentUser.UserId, lines, timeProvider.GetUtcNow())`.
     - (6) `Add` y un solo `SaveChangesAsync`.
     - (7) `OrderResponse.From(order, table.Number)`.
     - Con test unitario que verifique el orden de las validaciones.
  5. `feat(orders): expose open order endpoint [RST-ORD-01]`. Política `Waiter`.
  6. `test(orders): cover open order endpoint [RST-ORD-01]`.

#### RST-ORD-02 · Agregar y quitar platos de una orden
- **Ola** 4 · **Rama** `feature/RST-ORD-02-order-items` · **Depende de** ORD-01
- **Criterios:**
```gherkin
Escenario: Agregar plato existente suma la cantidad
  Dado una orden InProgress con Locrio x2
  Cuando hago POST /api/v1/orders/{id}/items con Locrio x1
  Entonces recibo 200 con Locrio x3 y el subtotal recalculado
Escenario: El plato cambió de precio
  Dado una orden con Locrio x2 a 450.00 y el precio de Locrio cambiado a 500.00
  Cuando agrego Locrio x1
  Entonces la orden tiene 2 items de Locrio (x2 a 450.00 y x1 a 500.00) y subtotal 1400.00
Escenario: Quitar item
  Dado una orden con 2 items
  Cuando hago DELETE /api/v1/orders/{id}/items/{itemId}
  Entonces recibo 200 con 1 item
Escenario: Quitar el único item
  Entonces recibo 422 "Order.CannotRemoveLastItem"
Escenario: Orden completada
  Entonces recibo 422 "Order.NotInProgress"
```
- **Archivos:** `Application/Orders/{AddOrderItem, RemoveOrderItem}/*`, `Api/Endpoints/Orders/{AddOrderItemEndpoint, RemoveOrderItemEndpoint}.cs`, `Application.Tests/Orders/{AddOrderItemHandlerTests, RemoveOrderItemHandlerTests}.cs`, `IntegrationTests/Orders/OrderItemsTests.cs`.
- **Tasks:**
  1. `feat(orders): add add-item and remove-item use cases [RST-ORD-02]`. Cada handler carga la orden (404 si no existe) y su mesa (para `tableNumber`). Para agregar, carga el plato (404) y usa `dish.ToOrderLine(qty)`.
  2. `feat(orders): expose order item endpoints [RST-ORD-02]`.
  3. `test(orders): cover order item endpoints [RST-ORD-02]`.

#### RST-ORD-03 · Completar orden
- **Ola** 4 · **Rama** `feature/RST-ORD-03-complete-order` · **Depende de** ORD-01
- **Criterios:**
```gherkin
Escenario: Completar
  Dado una orden InProgress en la mesa 3 (InService)
  Cuando hago PATCH /api/v1/orders/{id}/complete
  Entonces recibo 200 con status "Completed" y completedAt asignado
  Y la mesa 3 queda "Served"
Escenario: Ya completada
  Entonces recibo 422 "Order.NotInProgress"
```
- **Archivos:** `Application/Orders/CompleteOrder/*`, `Api/Endpoints/Orders/CompleteOrderEndpoint.cs`, `Application.Tests/Orders/CompleteOrderHandlerTests.cs`, `IntegrationTests/Orders/CompleteOrderTests.cs`.
- **Tasks:**
  1. `feat(orders): add complete order use case [RST-ORD-03]`. `order.Complete(now)` y `table.MarkServed()`, con un solo `SaveChangesAsync`.
  2. `feat(orders): expose complete endpoint [RST-ORD-03]`.
  3. `test(orders): cover complete endpoint [RST-ORD-03]`.

#### RST-ORD-04 · Listar y consultar órdenes
- **Ola** 4 · **Rama** `feature/RST-ORD-04-query-orders` · **Depende de** ORD-01
- **Criterios:**
```gherkin
Escenario: Listar paginado y filtrado
  Dado 25 órdenes de la mesa 3, insertadas con el DbContext en estado Completed (el índice parcial permite una sola InProgress por mesa)
  Cuando hago GET /api/v1/orders?tableId={mesa3}&page=2&pageSize=10
  Entonces recibo 10 items, totalCount 25 y totalPages 3, ordenados por createdAt descendente
Escenario: pageSize fuera de rango
  Cuando pageSize es 101
  Entonces recibo 400
Escenario: Consultar
  Entonces GET /api/v1/orders/{id} devuelve OrderResponse con sus items; si no existe, 404 "Order.NotFound"
```
- **Archivos:** `Application/Orders/{ListOrders, GetOrder}/*`, `Api/Endpoints/Orders/{ListOrdersEndpoint, GetOrderEndpoint}.cs`, `IntegrationTests/Orders/QueryOrdersTests.cs`.
- **Tasks:**
  1. `feat(orders): add list and get queries [RST-ORD-04]`. Los números de mesa se resuelven con `ITableRepository.GetByIdsAsync`. El validator usa `ApplyPaging()`.
  2. `feat(orders): expose query endpoints [RST-ORD-04]`.
  3. `test(orders): cover query endpoints [RST-ORD-04]`.


## 14. Definition of Done y verificación final

### 14.1 DoD de cada PBI
- [ ] Todos los criterios de aceptación (Gherkin) del PBI están cubiertos por tests automatizados.
- [ ] Hay tests de dominio por cada regla nueva y tests de integración según la sección 6.
- [ ] `dotnet build -warnaserror` pasa sin errores ni warnings.
- [ ] `dotnet format --verify-no-changes` pasa sin cambios.
- [ ] `dotnet test` pasa completo: el suite entero, no solo los tests del PBI.
- [ ] Solo se tocaron los archivos del PBI. Cero archivos congelados.
- [ ] Commits con el formato de la sección 11.5, una Task por commit.
- [ ] El PR tiene título, `Closes #`, checklist y la sección "Decisiones" completa (o "Ninguna").

### 14.2 Verificación final del proyecto (Supervisor, PBI `RST-PLT-99`)
En una carpeta nueva, partiendo de un `git clone` limpio:
```bash
cp .env.example .env                       # y poner valores locales en .env
docker compose up -d db
# configurar los user-secrets con los comandos de la sección 4.1
dotnet ef database update --project src/Restaurant.Infrastructure --startup-project src/Restaurant.Api
dotnet run --project src/Restaurant.Api
```
- [ ] `GET /health` responde 200.
- [ ] `/scalar/v1` carga en Development y lista todos los endpoints de la sección 10 con su documentación.
- [ ] El archivo `src/Restaurant.Api/Restaurant.Api.http` ejecuta de punta a punta el flujo principal (sección 13, PBI de cierre) sin errores.
- [ ] `dotnet test` está verde y el CI de GitHub Actions está verde en `main`.
- [ ] `docker compose up --build` levanta la base y la API, y la API responde en `http://localhost:8080/health`.
- [ ] `git grep -nE "(Password|Pwd|Key)=[^$<{]" -- ':!*.md' ':!.env.example'` no devuelve nada. Las referencias del tipo `${VAR}` en `docker-compose.yml` no cuentan como secretos.
- [ ] El README tiene: descripción y problema de negocio; stack; arquitectura con diagrama Mermaid; estructura de carpetas; requisitos; pasos para correrlo (los de arriba); configuración de secretos; tabla de endpoints; decisiones técnicas (enlace a `docs/decisions.md`); limitaciones conocidas; y badge del CI.
- [ ] Tag `v1.0.0` publicado con release notes.
