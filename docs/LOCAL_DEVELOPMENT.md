# Desarrollo local en macOS y Windows

Esta guía prepara una computadora para compilar, probar y ejecutar CafePOS desde el
código fuente. Aunque el producto original era Android, este repositorio es una
aplicación de escritorio independiente: no requiere Android Studio, JDK, Gradle,
emuladores, Docker ni un servidor externo de base de datos.

## 1. Requisitos

| Requisito | macOS | Windows |
| --- | --- | --- |
| Sistema | Una versión de macOS compatible con .NET 10 | Windows 10/11 de 64 bits |
| Herramienta obligatoria | SDK de .NET 10 | SDK de .NET 10 |
| Arquitectura | Apple Silicon (`arm64`) o Intel (`x64`) | `x64` |
| Base de datos | SQLite, incluida mediante NuGet | SQLite, incluida mediante NuGet |
| Editor opcional | VS Code, Rider u otro editor C# | Visual Studio, VS Code, Rider u otro editor C# |

> Se necesita el **SDK**, no solamente “.NET Runtime” o “Desktop Runtime”. El
> framework de todos los proyectos se define como `net10.0` en
> `Directory.Build.props`.

También se necesita Git para clonar el repositorio. Si ya se recibió una copia del
código, Git no es indispensable para ejecutarla.

## 2. Instalar y comprobar .NET

Descargue el SDK de .NET 10 desde la [página oficial de
.NET](https://dotnet.microsoft.com/download/dotnet/10.0). En macOS seleccione el
instalador correspondiente a Apple Silicon (`Arm64`) o Intel (`x64`). En Windows use
el instalador `x64`.

Cierre y vuelva a abrir Terminal o PowerShell después de instalar. Compruebe el
entorno:

```text
dotnet --version
dotnet --list-sdks
```

El primer comando debe mostrar una versión `10.0.x`, y la lista debe contener al
menos un SDK `10.0.x`. Si aparece que `dotnet` no existe, reinicie la terminal; si el
problema continúa, reinstale el SDK y verifique que su directorio esté en `PATH`.

## 3. Obtener el código

En Terminal (macOS) o PowerShell (Windows):

```text
git clone <URL-DEL-REPOSITORIO>
cd SimplePOS_dotnet
```

Si el repositorio ya está descargado, solamente abra una terminal en la carpeta que
contiene `CafePOS.slnx`.

## 4. Restaurar, compilar y probar

Los siguientes comandos son iguales en macOS y PowerShell:

```text
dotnet restore CafePOS.slnx
dotnet build CafePOS.slnx --configuration Debug --no-restore
dotnet test CafePOS.slnx --configuration Debug --no-build
```

La primera restauración descarga los paquetes NuGet, por lo que necesita conexión a
Internet. Las ejecuciones siguientes aprovechan la caché local.

## 5. Ejecutar la aplicación

Desde la raíz del repositorio:

```text
dotnet run --project src/CafePOS.Desktop/CafePOS.Desktop.csproj
```

La primera ejecución:

1. crea el directorio de datos y el archivo SQLite `cafe.db`;
2. aplica las migraciones pendientes;
3. carga un empleado y un catálogo inicial si la base está vacía; y
4. abre la ventana de CafePOS.

Para detener la aplicación desde la terminal use `Ctrl+C` o cierre su ventana. Para
el flujo funcional básico, agregue productos y guarde la orden. Después, abra
**Historial** para liquidar cada orden con pago total en efectivo o tarjeta;
no se permiten pagos parciales ni pagos divididos.

## 6. Archivos locales y permisos

Los binarios compilados quedan bajo `src/CafePOS.Desktop/bin/` y los datos de negocio
se guardan fuera del repositorio:

| Plataforma | Directorio de datos |
| --- | --- |
| macOS | `~/Library/Application Support/CafePOS` |
| Windows | `C:\ProgramData\CafePOS` |

Dentro de ese directorio se encuentran:

- `cafe.db`: base de datos SQLite;
- `backups/`: respaldos automáticos y manuales;
- `logs/`: registros de diagnóstico diarios;
- `settings.json`: configuración, una vez guardada; y
- `updates/`: paquetes de actualización descargados, cuando se habilite esa función.

En Windows, `ProgramData` puede exigir permisos adicionales según las políticas del
equipo. Si el inicio falla con `UnauthorizedAccessException`, abra PowerShell **como
administrador** para la primera ejecución o conceda al usuario de desarrollo permiso
de modificación sobre `C:\ProgramData\CafePOS`. No cambie el código para guardar la
base dentro de `bin/`, porque una compilación o actualización podría eliminarla.

## 7. Reiniciar solamente los datos de desarrollo

> **Advertencia:** estos comandos eliminan ventas, caja, configuración y
> respaldos locales. No los use sobre una instalación con datos reales.

macOS:

```bash
rm -rf "$HOME/Library/Application Support/CafePOS"
```

Windows PowerShell (puede requerir una consola como administrador):

```powershell
Remove-Item -Recurse -Force "$env:ProgramData\CafePOS"
```

Al volver a ejecutar el proyecto se creará una base limpia con los datos iniciales.

Para limpiar solamente artefactos de compilación, sin borrar datos:

```text
dotnet clean CafePOS.slnx
```

## 8. Publicar un ejecutable local

Ejecutable de Windows `x64` autocontenido (no requiere instalar .NET en el equipo de
destino):

```text
dotnet publish src/CafePOS.Desktop/CafePOS.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output artifacts/CafePOS-win-x64
```

Desde macOS también se puede generar ese paquete de Windows; `dotnet publish` no lo
ejecutará ni lo probará en macOS. Para ejecutar desde el código no hace falta publicar.

## 9. Problemas frecuentes

### `The current .NET SDK does not support targeting .NET 10.0`

Hay un SDK anterior seleccionado. Confirme `dotnet --version`, instale el SDK 10 y
vuelva a abrir la terminal.

### Falla `dotnet restore`

Compruebe la conexión a Internet y el acceso a `https://api.nuget.org`. En una red
corporativa puede ser necesario configurar el proxy o la fuente NuGet aprobada por
la organización. No continúe con `--no-restore` hasta que la restauración termine.

### La aplicación no abre o se cierra al iniciar

Revise el archivo más reciente en `logs/` dentro del directorio de datos indicado
arriba. En Windows compruebe primero los permisos de `C:\ProgramData\CafePOS`.

### Quiero conservar datos antes de probar cambios

Cierre CafePOS y copie el directorio de datos completo a otro lugar. También se
puede crear un respaldo desde **Ajustes**, pero copiar el directorio cerrado conserva
la base, configuración y logs juntos.
