# Mock API — artefacto de contenerización (AI-200)

**Tipo:** Aplicación .NET real (repositorio independiente, no forma parte del monorepo de estudio [`Azure-AI-200`](https://github.com/AdrianPolanco/Azure-AI-200)).
**Propósito:** dar a los labs de contenerización del programa de estudio AI-200 (ACR, ACR Tasks, App Service, Container Apps, AKS) una imagen real y no trivial para build/push/deploy, en vez de un `Dockerfile` + `hello.txt` dummy.

No es contenido de estudio en sí — es infraestructura de prueba que esos labs referencian y despliegan.

## Qué es

Una Web API mínima en ASP.NET Core (.NET 10) con CRUD de `Product` contra una base de datos en memoria (EF Core `InMemory`). Sin dependencias externas (sin Docker, sin Azure) para poder ejecutarla y testearla localmente antes de contenerizarla.

## Estructura

```
mock-api-acr/
├── MockApi.slnx
├── src/MockApi.Api/
│   ├── Program.cs              — bootstrap, DI, endpoints (Minimal API)
│   ├── Entities/Product.cs     — entidad EF Core
│   ├── Data/MockApiDbContext.cs
│   ├── Contracts/ProductDto.cs — ProductDto, CreateProductRequest, UpdateProductRequest
│   ├── Contracts/ReviewContracts.cs — ReviewDto, ProductReviewsDto, ProductWithReviewsDto
│   ├── Services/                — IProductsService / ProductsService (lógica de negocio)
│   ├── Clients/                  — IReviewsClient / ReviewsClient (typed HttpClient hacia mock-reviews-api)
│   ├── Options/ReviewsApiOptions.cs — BaseUrl del servicio interno, vía IOptionsMonitor<T>
│   ├── Common/                  — Result<T>, rutas y mensajes como constantes
│   └── Dockerfile               — build multi-stage
└── tests/MockApi.Tests/
    ├── ProductsServiceTests.cs — xUnit + FluentAssertions, EF Core InMemory
    └── ReviewsClientTests.cs   — xUnit + FluentAssertions, HttpMessageHandler falso
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Liveness check (útil como healthProbe en Container Apps/AKS) |
| GET | `/api/products` | Lista todos los productos |
| GET | `/api/products/{id}` | Obtiene un producto por id |
| POST | `/api/products` | Crea un producto |
| PUT | `/api/products/{id}` | Actualiza un producto |
| DELETE | `/api/products/{id}` | Elimina un producto |
| GET | `/api/products/{id}/reviews` | Producto + reseñas, llamando internamente a `mock-reviews-api` (404 si el producto no existe, 503 si el servicio interno no responde) |
| GET | `/openapi/v1.json` (solo Development) | Documento OpenAPI |

La base de datos en memoria se siembra con 2 productos al arrancar, así el `GET /api/products` devuelve algo útil sin pasos manuales — conveniente para verificar un despliegue en un lab.

## Comandos

```bash
# Build
dotnet build

# Tests
dotnet test

# Ejecutar localmente
dotnet run --project src/MockApi.Api
```

## Docker

```bash
# Desde la raíz del repo (el contexto de build es la raíz, no src/MockApi.Api/)
docker build -f src/MockApi.Api/Dockerfile -t mock-api-acr:local .
docker run --rm -p 8080:8080 mock-api-acr:local
curl http://localhost:8080/health
```

## Notas de diseño

- **In-memory, no persistente**: intencional — es un artefacto de práctica de contenerización/despliegue, no una app productiva. Cada reinicio del proceso pierde los datos sembrados.
- Convenciones: primary constructors, namespaces file-scoped, records para DTOs, `Result<T>` en vez de excepciones para flujo de control, `CancellationToken` propagado en toda la cadena, constantes en vez de strings mágicos (`Common/ApiRoutes`, `Common/ErrorMessages`).
- `Program.cs` expone `public partial class Program` al final para permitir tests de integración con `WebApplicationFactory<Program>` más adelante si se necesitan (hoy los tests son unitarios contra `ProductsService`, no de integración).

## Integración con mock-reviews-api

`mock-api-acr` llama a [`mock-reviews-api`](https://github.com/AdrianPolanco/mock-reviews-api) — un segundo servicio dummy pensado para desplegarse en el **mismo environment de Azure Container Apps** — para practicar comunicación service-to-service dentro del environment:

- **`mock-api-acr`** se despliega con `--ingress external` (alcanzable desde internet).
- **`mock-reviews-api`** se despliega con `--ingress internal` (solo alcanzable desde otras Container Apps del mismo environment).

El endpoint `GET /api/products/{id}/reviews` obtiene el producto localmente y llama a `mock-reviews-api` vía un `HttpClient` tipado (`Clients/ReviewsClient.cs`) con Polly v8 (`AddStandardResilienceHandler`). La URL base se configura con `IOptionsMonitor<ReviewsApiOptions>` (sección `ReviewsApi:BaseUrl` en `appsettings.json`, o la variable de entorno `ReviewsApi__BaseUrl` en Container Apps).

```bash
# Local: levantar mock-reviews-api en otra terminal (puerto 5200) y luego
dotnet run --project src/MockApi.Api
curl http://localhost:5154/api/products/1/reviews
```

```bash
# Azure Container Apps: apuntar mock-api-acr al FQDN interno de mock-reviews-api
az containerapp update -n mock-api-acr -g <rg> \
  --set-env-vars ReviewsApi__BaseUrl=https://<mock-reviews-api-fqdn-interno>
```

Ver el README de `mock-reviews-api` para los comandos completos de despliegue con ingress `internal`.

## Relación con el repo de estudio AI-200

Este repo se referencia desde los labs de `01-containers/` en [`Azure-AI-200`](https://github.com/AdrianPolanco/Azure-AI-200) como la imagen a construir/publicar/desplegar (ACR, ACR Tasks, App Service, Container Apps, AKS), en lugar de un artefacto dummy.
