# Auditoría de cumplimiento de la migración

Fecha de revisión: 2026-08-13. Esta auditoría compara el repositorio actual con el
alcance solicitado; no afirma que CafePOS sea todavía una versión lista para operar
sin una prueba de aceptación en el hardware de la cafetería.

## Resultado ejecutivo

La migración a cafetería está implementada: solución por capas, Avalonia/MVVM, EF
Core/SQLite, catálogo genérico, órdenes con snapshots, pagos
divididos, clientes, entregas, caja, movimientos manuales, respaldos y métricas.
La migración `RebuildCafeSchema` es deliberadamente destructiva: respalda antes de
aplicarse y elimina el esquema anterior para no conservar conceptos especializados.

No debe declararse completo el alcance total: faltan usuarios reales y un updater
instalado por un proceso externo.

## Matriz de cumplimiento

| Área solicitada | Estado | Evidencia / observación |
|---|---|---|
| .NET, C#, Avalonia, MVVM, DI | Cumple | Cuatro proyectos productivos, Toolkit MVVM y composición en `Program`/`DependencyInjection`. |
| Dominio/Application/Infrastructure/Desktop | Cumple | Separación pragmática sin CQRS ni repositorio genérico. |
| EF Core + SQLite + migrations | Cumple | `MigrateAsync`, migración destructiva versionada y sin `EnsureCreated`; hay prueba de creación de esquema. |
| Ruta persistente por SO | Cumple | ProgramData en Windows, Application Support en macOS y LocalApplicationData en Linux. |
| Backup premigración/manual/rotación | Cumple base | Copia consistente mediante API de backup SQLite y retención de 30; backup diario aún es futuro. |
| Productos | Cumple | CRUD genérico por categoría, precio y estado activo; esta primera versión no limita cantidades. |
| Crear/modificar orden | Cumple | Carrito con cantidad, comentarios, nombre/teléfono y snapshots de nombre, categoría y precio. |
| Pago de órdenes | Cumple | Liquidación total con un solo cobro en efectivo o tarjeta; no se admiten pagos parciales ni divididos. |
| Entregas | Cumple | CRUD de opción, tipo Pickup/Walking/Delivery, tarifa, estado activo y snapshot en orden. |
| Caja, historial y movimientos | Cumple | Reporte por método/categoría, búsqueda por rango, cancelación con reembolso y movimientos manuales. |
| Métricas | Cumple | Comparación contra periodo anterior, rankings de productos y categorías. |
| Transacciones | Cumple base | Guardado de orden, consumo/devolución y reemplazo de pagos se realiza en transacciones SQLite. |
| Configuración y logs persistentes | Cumple base | JSON atómico y Serilog diario fuera del paquete; falta editor completo en UI. |
| Consulta de GitHub Releases | Parcial | Consulta y descarga a datos persistentes; no instala, valida firma/hash ni reinicia. |
| CI, win-x64 y Release por tag | Cumple definición | Workflow restaura, prueba, publica self-contained, comprime y crea release para tags `v*.*.*`. Debe validarse en GitHub. |
| Pruebas críticas | Parcial | Totales, división de pagos y balance tienen tests unitarios; faltan tests EF SQLite, backup, migración e idempotencia. |

## Reglas de Comaleña conservadas

Se simplificaron los cobros a pago total en efectivo o tarjeta y se conservaron snapshot de artículos, folio diario, comentarios,
historial por rango, separación de movimientos manuales y ventas, y cancelación auditable. Se mejoró el origen mediante importes `decimal` almacenados como centavos,
pagos relacionales y sesión durable de caja. El análisis detallado y el mapeo de
nombres reales permanece en `COMALENA_ANALYSIS.md`.

No se copiaron Compose, AndroidViewModel, Room/DAO, singleton de base, Bluetooth
Android, impresión, JSON como fuente de verdad ni dinero en Double. La migración
actual sí es destructiva por decisión explícita para sustituir el esquema anterior
por productos de cafetería.

## Riesgos y siguiente etapa recomendada

1. Agregar pruebas de integración para concurrencia de pagos, rollback y reapertura de proceso.
2. Implementar un updater externo firmado con checksum; la descarga actual no debe confundirse con autoactualización completa.
3. Ejecutar una prueba de aceptación en Windows con permisos de ProgramData, desconexión abrupta y restauración de backup antes del primer uso.
