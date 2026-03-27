# EIA Nuclear Outages — Data Pipeline & Dashboard

Solución integral End-to-End que extrae, transforma, almacena y visualiza datos de la API de la *Energy Information Administration (EIA)* sobre cortes de energía nuclear.

El sistema consta de un pipeline de datos automatizado, una API RESTful y un dashboard interactivo en React.

---

## Arquitectura y Tecnologías

| Capa | Tecnología |
|---|---|
| Data Connector | C# (.NET 9) con resiliencia usando *Polly* |
| Almacenamiento Intermedio | Parquet (*Parquet.Net*) |
| Base de Datos | SQLite + Entity Framework Core |
| Backend API | ASP.NET Core Minimal APIs + JWT |
| Frontend | React, Vite, Bootstrap, Axios |
| Despliegue | Docker + Docker Compose (multi-stage builds) |

---

## Configuración de la API Key

Para ejecutar este proyecto es necesario contar con una API Key proporcionada por el gobierno de EE. UU.

1. Visita el portal oficial de datos abiertos: <https://www.eia.gov/opendata/>
2. Regístrate con tu correo electrónico.
3. Recibirás tu clave de acceso en tu bandeja de entrada.

---

## Requisitos Previos

### Docker

- Docker Desktop o Docker Engine
- Docker Compose

### Local

- .NET 9 SDK
- Node.js v20+

---

## Opción 1: Instalación con Docker (Quick Start)

Forma recomendada para levantar el proyecto completo en producción.

**1. Clonar el repositorio**
```bash
git clone <repo_url>
cd <repo>
```

**2. Crear archivo de variables de entorno**

Crea un archivo `.env` en la raíz del proyecto:
```env
EIA_API_KEY=tu_api_key_aqui
```

**3. Levantar los contenedores**
```bash
docker-compose up --build -d
```

**Accesos**

| Servicio | URL |
|---|---|
| Frontend | <http://localhost:3000> |
| Backend (Swagger) | <http://localhost:5090/swagger> |

---

## Opción 2: Instalación Local (Desarrollo)

**1. Configurar Backend y API Key**
```bash
cd Eia.Api
dotnet user-secrets init
dotnet user-secrets set "EIA_API_KEY" "tu_api_key_aqui"
dotnet run
```

API disponible en: <http://localhost:5090>

**2. Configurar Frontend**

En una terminal nueva:
```bash
cd Eia.Frontend
npm install
npm run dev
```

Disponible en: <http://localhost:5173>

---

## Comandos Útiles para el Desarrollo

### Generar archivo Parquet manualmente

Para probar la extracción y serialización de datos sin levantar la API:
```bash
cd Eia.Connector
dotnet run
```

### Manejo de Base de Datos (Migraciones)

Si modificas el modelo de datos en `Eia.Data`, genera y aplica las migraciones desde el proyecto de la API:
```bash
cd Eia.Api
dotnet ef migrations add InitialCreate --project ../Eia.Data --startup-project .
dotnet ef database update
```

---

## Supuestos del Proyecto

**Autenticación preconfigurada:** El sistema es de uso interno corporativo. El pipeline siembra automáticamente un usuario administrador por defecto durante la inicialización.

**Estructura de datos de la EIA:** Se asume que el contrato del endpoint `/v2/nuclear-outages/us-nuclear-outages/data` mantiene su estructura actual (`period`, `capacity`, `outage`). El formato intermedio Parquet se genera en base a esta estructura exacta.

### Credenciales por defecto

| Campo | Valor |
|---|---|
| Email | `admin@eia.local` |
| Contraseña | `Admin1234!` |

---

## Flujo ETL

1. Inicia sesión en el dashboard.
2. Presiona **"Extraer Datos Nuevos"**.
3. La API ejecuta el conector como subproceso.
4. El conector extrae datos con paginación, valida esquemas y guarda la información en `nuclear_outages_raw.parquet`.
5. Se actualiza `checkpoint.json` para garantizar cargas incrementales futuras.
6. La API lee el archivo Parquet y realiza una operación de Upsert en SQLite.
7. El frontend se actualiza automáticamente mostrando la información.

---

## Pruebas y Resultados

El proyecto cuenta con una suite de pruebas unitarias y de integración desarrolladas con **xUnit**, cubriendo:

- Validación de datos
- Control de duplicados en repositorios
- Seguridad en los endpoints de la API

Para ejecutar todas las pruebas desde la raíz del proyecto:
```bash
dotnet test --verbosity normal
```