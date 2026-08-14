# CafePOS

> Proyecto nuevo e independiente. Todo el código, documentación, pruebas y archivos de distribución de CafePOS viven dentro de esta carpeta; el proyecto Android Comaleña permanece separado.

POS local de escritorio para cafetería, construido con .NET 10, C#, Avalonia, MVVM,
EF Core y SQLite. El análisis verificable del proyecto Android de referencia y el
mapeo de reglas están en [`docs/COMALENA_ANALYSIS.md`](docs/COMALENA_ANALYSIS.md).
El estado de cumplimiento, brechas y riesgos encontrados en la revisión están en
[`docs/MIGRATION_COMPLIANCE.md`](docs/MIGRATION_COMPLIANCE.md).

## Desarrollo local

Para desarrollar y ejecutar CafePOS se necesita el **SDK de .NET 10** (no solamente
el runtime). No hacen falta Android Studio, Java, un emulador, Docker ni un servidor
de base de datos: Avalonia crea la aplicación de escritorio y SQLite se ejecuta de
forma local.

La guía paso a paso incluye instalación y verificación para ambas plataformas,
comandos de PowerShell y Terminal, ubicación de datos, solución de problemas y
publicación local:

**[Configurar el entorno local en macOS y Windows](docs/LOCAL_DEVELOPMENT.md)**

Una vez instalado el SDK, el recorrido corto desde la raíz del repositorio es:

```bash
dotnet --version
dotnet restore CafePOS.slnx
dotnet test CafePOS.slnx
dotnet run --project src/CafePOS.Desktop/CafePOS.Desktop.csproj
```

Los datos se guardan en `~/Library/Application Support/CafePOS`; en Windows se usa
`C:\ProgramData\CafePOS`. La base, `backups`, `logs`, tickets y configuración nunca
forman parte del paquete de aplicación. Al iniciar se crea un backup si hay migrations
pendientes y después se llama `MigrateAsync`; no se usa `EnsureCreated`.

El flujo operativo del MVP requiere abrir **Caja** antes de cobrar efectivo. Agregue
productos, opcionalmente escriba un importe parcial y cobre con efectivo o
transferencia. El historial muestra los últimos 30 días. Ajustes permite crear un
backup manual; se conservan los 30 más recientes.

## Publicación Windows

```bash
dotnet test CafePOS.slnx
dotnet publish src/CafePOS.Desktop/CafePOS.Desktop.csproj \
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El workflow incluido en `.github/workflows/release.yml` compila y adjunta un ZIP self-contained. Un tag `v1.0.1` crea el GitHub
Release. Configure `GitHubRepository` (por ejemplo `owner/repo`) en `settings.json`
para consultar releases. La descarga queda en el directorio persistente `updates`;
la fase siguiente incorporará un updater externo firmado que cierre la app, valide el
paquete, reemplace únicamente binarios y reinicie. Esto evita un reemplazo inseguro
desde el propio proceso y, por diseño, jamás apunta al directorio de datos.

## Alcance pendiente

Editor completo de catálogo, detalle/cancelación desde historial, variantes y extras,
impresora térmica real, updater externo e interfaz de ajustes completa. La abstracción
y el esquema permiten agregarlos sin migrar datos ya guardados.
