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
dotnet build CafePOS.slnx --configuration Debug --no-restore
dotnet run --project src/CafePOS.Desktop/CafePOS.Desktop.csproj
```

Para ejecutar las pruebas:

```bash
dotnet test CafePOS.slnx --configuration Debug
```

En Windows, si el proyecto compila pero no aparece la ventana, revise el log más
reciente en `C:\ProgramData\CafePOS\logs`. Si aparece un error de SQLite como
`table Employees already exists`, la base local de desarrollo quedó incompatible con
las migraciones actuales. Si no hay datos reales que conservar, puede reiniciarla y
volver a ejecutar la app:

```powershell
Remove-Item -Recurse -Force "$env:ProgramData\CafePOS"
dotnet run --project src/CafePOS.Desktop/CafePOS.Desktop.csproj
```

Si quiere guardar una copia antes de borrar la base local:

```powershell
Copy-Item -Recurse "$env:ProgramData\CafePOS" "$env:USERPROFILE\Desktop\CafePOS-backup"
Remove-Item -Recurse -Force "$env:ProgramData\CafePOS"
dotnet run --project src/CafePOS.Desktop/CafePOS.Desktop.csproj
```

Si `Remove-Item` falla por permisos, abra PowerShell como administrador y repita el
comando.

Los datos se guardan en `~/Library/Application Support/CafePOS`; en Windows se usa
`C:\ProgramData\CafePOS`. La base, `backups`, `logs` y configuración nunca
forman parte del paquete de aplicación. Al iniciar se crea un backup si hay migrations
pendientes y después se llama `MigrateAsync`; no se usa `EnsureCreated`.
La migración de reconstrucción nunca se aplica automáticamente sobre una base con
datos: el inicio falla con una instrucción accionable. Solo para reinicios de
desarrollo explícitos, después de verificar el respaldo, puede usarse
`CAFEPOS_ALLOW_DESTRUCTIVE_RESET=true`.

El flujo operativo permite crear una orden con productos de café, bebidas, comida,
postres o extras; la existencia se descuenta de forma transaccional. Capture nombre
y teléfono del cliente, seleccione la entrega y guarde la orden. Las entregas exigen
teléfono normalizado y dirección, admiten repartidor, compromiso, estados y efectivo
contra entrega. Los cobros, ajustes de cobro, reembolsos de cancelación y reversas
manuales son registros auditables: no se eliminan. **Stock** registra entradas,
conteos, merma y correcciones con motivo, fecha, actor y proveedor/referencia.
**Indicadores** separa facturado, cobrado y pendiente. Las funciones administrativas
se muestran solo al rol Administrador del alcance local actual.

## Publicación Windows

```bash
dotnet test CafePOS.slnx
dotnet publish src/CafePOS.Desktop/CafePOS.Desktop.csproj \
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El workflow incluido en `.github/workflows/release.yml` compila y adjunta
`CafePOS-win-x64.zip`, autocontenido y listo para Windows de 64 bits. Cada cambio
integrado en la rama `main` incrementa automáticamente el último número de versión,
crea el tag y publica el Release. Por ejemplo, después de `v1.0.1`, el siguiente
cambio integrado publicará `v1.0.2`; no hace falta crear esos tags manualmente.

Para iniciar deliberadamente una versión mayor o menor (por ejemplo `v1.1.0` o
`v2.0.0`), todavía puede crear ese tag manualmente:

```bash
git tag v1.1.0
git push origin v1.1.0
```

GitHub también creará ese Release automáticamente. En la primera instalación, el cliente
descarga el ZIP desde la sección **Releases**, lo descomprime en una carpeta con
permisos de escritura (por ejemplo `%LOCALAPPDATA%\CafePOS`) y ejecuta
`CafePOS.Desktop.exe`; no necesita instalar .NET.

La aplicación consulta el último Release del repositorio configurado en
`GitHubRepository` (por defecto `alos1895/SimplePOS_dotnet`). Cuando hay una versión
superior muestra **Actualizar ahora**: descarga el ZIP, cierra CafePOS, reemplaza sus
binarios mediante un proceso externo y vuelve a abrirlo. La base de datos y los
respaldos están fuera de la carpeta de instalación, por lo que no se reemplazan.

## Alcance pendiente

Firma de código del ejecutable y del instalador, e interfaz de ajustes completa.
Impresión, impresoras y conexiones de impresora quedan fuera del alcance por decisión del proyecto.
