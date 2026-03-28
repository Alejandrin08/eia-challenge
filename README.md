# EIA Nuclear Outages 

Proyecto que extrae, almacena y visualiza datos de la API de la *Energy Information Administration (EIA)* sobre cortes de energía nuclear.

---

## Despliegue

El proyecto está desplegado y disponible en:

| Servicio | URL |
|---|---|
| Frontend | <https://eia-challenge.vercel.app/login> |
| Backend (Swagger) | <https://eia-challenge.onrender.com/swagger/index.html> |

> **Nota:** El backend está alojado en Render con plan gratuito, por lo que puede tardar ~30 segundos en responder si estuvo inactivo.

---

## Arquitectura y Tecnologías

| Capa | Tecnología |
|---|---|
| Data Connector | C# (.NET 9) |
| Almacenamiento Intermedio | Parquet (*Parquet.Net*) |
| Base de Datos | SQLite + Entity Framework Core |
| Backend API | ASP.NET Core Minimal APIs + JWT |
| Frontend | React, Vite, Bootstrap, Axios |
| Despliegue | Docker + Docker Compose |

---

## Configuración de la API Key

Para ejecutar este proyecto es necesario contar con una API Key.

1. Obtener API Key en: <https://www.eia.gov/opendata/>
2. Regístrar con correo electrónico.
3. Recibirás tu clave de acceso en tu correo.

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

**1. Clonar el repositorio**
```bash
git clone https://github.com/Alejandrin08/eia-challenge.git
cd eia-challenge
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

**2. Configurar Frontend**

En una terminal nueva:
```bash
cd Eia.Frontend
npm install
npm run dev
```

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

**Autenticación preconfigurada:** El sistema coloca automáticamente un usuario administrador por defecto.

**Estructura de datos de la EIA:** Se asume que el endpoint `/v2/nuclear-outages/us-nuclear-outages/data` mantiene su estructura actual (`period`, `capacity`, `outage`). El formato intermedio Parquet se genera en base a esta estructura exacta.

### Credenciales por defecto

| Campo | Valor |
|---|---|
| Email | `admin@eia.local` |
| Contraseña | `Admin1234!` |

---

## Pruebas y Resultados

El proyecto cuenta con una conjunto de pruebas unitarias y de integración desarrolladas con **MSTest**, cubriendo:

- Validación de datos
- Control de duplicados en repositorios
- Seguridad en los endpoints de la API

Para ejecutar todas las pruebas desde la raíz del proyecto:
```bash
dotnet test --verbosity normal
```
