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
│   ├── Clients/                  — IReviewsClient / ReviewsClient (Dapr service invocation hacia mock-reviews-api)
│   ├── Options/ReviewsApiOptions.cs — AppId de Dapr del servicio interno, vía IOptionsMonitor<T>
│   ├── Common/                  — Result<T>, rutas y mensajes como constantes
│   └── Dockerfile               — build multi-stage
└── tests/MockApi.Tests/
    ├── ProductsServiceTests.cs — xUnit + FluentAssertions, EF Core InMemory
    └── ReviewsClientTests.cs   — xUnit + FluentAssertions, cubre ReviewsClient.MapResponseAsync (ver nota abajo)
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
- `ReviewsClient` habla con el sidecar de Dapr por gRPC, no por el pipeline de `HttpMessageHandler` de .NET, así que no es fakeable con un handler en memoria. `ReviewsClientTests.cs` prueba directamente `ReviewsClient.MapResponseAsync` (la lógica de mapear la respuesta a `Result<T>`), dejando el transporte del sidecar sin cubrir por test unitario — para probarlo end-to-end hace falta un sidecar real (Dapr CLI local o desplegado en ACA).

## Integración con mock-reviews-api (Dapr service invocation)

`mock-api-acr` llama a [`mock-reviews-api`](https://github.com/AdrianPolanco/mock-reviews-api) — un segundo servicio dummy pensado para desplegarse en el **mismo environment de Azure Container Apps** — usando el **service invocation building block de Dapr** (ACA trae Dapr integrado, no hay que instalar nada aparte):

- **`mock-api-acr`** se despliega con `--ingress external` (alcanzable desde internet) y `--enable-dapr --dapr-app-id mock-api-acr --dapr-app-port 8080`.
- **`mock-reviews-api`** se despliega con `--ingress internal` (solo alcanzable desde otras Container Apps del mismo environment) y `--enable-dapr --dapr-app-id mock-reviews-api --dapr-app-port 8080`.

El endpoint `GET /api/products/{id}/reviews` obtiene el producto localmente y llama a `mock-reviews-api` a través de `Clients/ReviewsClient.cs`, que usa `DaprClient.CreateInvokableHttpClient(appId)` (SDK `Dapr.Client`/`Dapr.AspNetCore`). La petición **no** va a una URL/DNS del otro servicio — va al sidecar de Dapr de `mock-api-acr` (`localhost:{DAPR_HTTP_PORT}`), que la reenvía al sidecar de `mock-reviews-api` por su `app-id`. El `app-id` de destino se configura con `IOptionsMonitor<ReviewsApiOptions>` (sección `ReviewsApi:AppId` en `appsettings.json`, o la variable de entorno `ReviewsApi__AppId`).

Errores de red/sidecar (`HttpRequestException`) y respuestas no exitosas del servicio de reseñas se traducen a `Result.Failure` → el endpoint responde **503**; producto inexistente → **404**.

```bash
# Local: requiere el Dapr CLI (`dapr init`). En dos terminales:
dapr run --app-id mock-reviews-api --app-port 8080 --dapr-http-port 3501 -- dotnet run --project ../mock-reviews-api/src/MockReviewsApi.Api
dapr run --app-id mock-api-acr --app-port 8080 --dapr-http-port 3500 -- dotnet run --project src/MockApi.Api

curl http://localhost:5154/api/products/1/reviews
```

```bash
# Azure Container Apps: habilitar Dapr en ambas Container Apps al crearlas
az containerapp create -n mock-reviews-api -g <rg> --environment <env> \
  --image <acr>.azurecr.io/mock-reviews-api:v1 --target-port 8080 --ingress internal \
  --enable-dapr --dapr-app-id mock-reviews-api --dapr-app-port 8080

az containerapp create -n mock-api-acr -g <rg> --environment <env> \
  --image <acr>.azurecr.io/mock-api-acr:v1 --target-port 8080 --ingress external \
  --enable-dapr --dapr-app-id mock-api-acr --dapr-app-port 8080 \
  --set-env-vars ReviewsApi__AppId=mock-reviews-api
```

No hace falta apuntar `mock-api-acr` a ningún FQDN de `mock-reviews-api` — Dapr resuelve por `app-id` dentro del environment. `--ingress internal` en `mock-reviews-api` sigue siendo útil para poder probarlo de forma aislada (sin pasar por Dapr) si hace falta depurar.

Ver el README de `mock-reviews-api` para más detalle.

## Relación con el repo de estudio AI-200

Este repo se referencia desde los labs de `01-containers/` en [`Azure-AI-200`](https://github.com/AdrianPolanco/Azure-AI-200) como la imagen a construir/publicar/desplegar (ACR, ACR Tasks, App Service, Container Apps, AKS), en lugar de un artefacto dummy.
