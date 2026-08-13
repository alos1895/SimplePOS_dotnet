# Auditoría de cumplimiento de la migración

Fecha de revisión: 2026-08-13. Esta auditoría compara el repositorio actual con el
alcance solicitado; no afirma que CafePOS sea todavía una versión lista para operar
sin una prueba de aceptación en el hardware de la cafetería.

## Resultado ejecutivo

El proyecto **cumple parcialmente**. La base técnica y el recorrido principal del MVP
están implementados: solución por capas, Avalonia/MVVM, EF Core/SQLite, migración
inicial, rutas persistentes, catálogo semilla, órdenes, pagos normalizados, caja,
historial, ticket de texto, backups, logs y publicación Windows. La revisión corrigió
dos bloqueadores: la migración inicial ahora está asociada explícitamente al contexto
EF y la UI continúa una misma orden al registrar pagos parciales en vez de crear otra.

No debe declararse completo el alcance total: faltan administración de productos,
cancelación y detalle de historial, filtros/búsqueda configurables en la pantalla,
usuarios reales, actualización instalada por un proceso externo, impresión térmica y
pruebas de integración de persistencia/transacciones.

## Matriz de cumplimiento

| Área solicitada | Estado | Evidencia / observación |
|---|---|---|
| .NET, C#, Avalonia, MVVM, DI | Cumple | Cuatro proyectos productivos, Toolkit MVVM y composición en `Program`/`DependencyInjection`. |
| Dominio/Application/Infrastructure/Desktop | Cumple | Separación pragmática sin CQRS ni repositorio genérico. |
| EF Core + SQLite + migrations | Cumple base | `MigrateAsync`, migración inicial versionada y sin `EnsureCreated`; falta una prueba de upgrade real entre dos versiones. |
| Ruta persistente por SO | Cumple | ProgramData en Windows, Application Support en macOS y LocalApplicationData en Linux. |
| Backup premigración/manual/rotación | Cumple base | Copia consistente mediante API de backup SQLite y retención de 30; backup diario aún es futuro. |
| Producto solicitado | Cumple modelo | Campos requeridos e imagen opcional; no existe todavía CRUD de catálogo ni variantes/extras. |
| Crear/modificar orden | Cumple MVP | Carrito con cantidad, borrado al llegar a cero, comentarios y snapshots de precio/nombre. No hay descuento en UI. |
| Efectivo/transferencia y pagos parciales | Cumple MVP | Pagos como filas auditables; la UI conserva el identificador tras el primer pago y bloquea modificar el carrito ya persistido. |
| Estado Open/Paid/Cancelled | Parcial | Reglas Open/Paid implementadas; modelo de cancelación existe, pero no su caso de uso/UI. |
| Caja y corte | Cumple MVP | Apertura, ingreso, retiro, venta en efectivo, saldo esperado, conteo y cierre. |
| Historial | Parcial | Persiste y lista 30 días con artículos/pagos; faltan controles de fecha, búsqueda, métodos y vista de detalle. |
| Transacciones e idempotencia | Parcial | Guardado de orden/pago/movimiento de venta usa transacción y movimiento único por orden; falta protección integral contra dos cobros concurrentes. |
| Impresión | Cumple abstracción | `IReceiptPrinter` y ticket de texto; hardware ESC/POS/Windows pendiente. |
| Configuración y logs persistentes | Cumple base | JSON atómico y Serilog diario fuera del paquete; falta editor completo en UI. |
| Consulta de GitHub Releases | Parcial | Consulta y descarga a datos persistentes; no instala, valida firma/hash ni reinicia. |
| CI, win-x64 y Release por tag | Cumple definición | Workflow restaura, prueba, publica self-contained, comprime y crea release para tags `v*.*.*`. Debe validarse en GitHub. |
| Pruebas críticas | Parcial | Totales, división de pagos y balance tienen tests unitarios; faltan tests EF SQLite, backup, migración e idempotencia. |

## Reglas de Comaleña conservadas

Se conservaron pagos divididos, snapshot de artículos, folio diario, comentarios,
historial por rango, separación de movimientos manuales y ventas, y abstracción de
ticket. Se mejoró el origen mediante importes `decimal` almacenados como centavos,
pagos relacionales y sesión durable de caja. El análisis detallado y el mapeo de
nombres reales permanece en `COMALENA_ANALYSIS.md`.

No se copiaron Compose, `AndroidViewModel`, Room/DAO, singleton de base, Bluetooth
Android, JSON como fuente de verdad, dinero en `Double`, migraciones destructivas ni
inventario especializado de pizzas. Delivery, clientes, métricas y variantes son
extensiones posteriores y no requisitos cerrados del MVP de cafetería.

## Riesgos y siguiente etapa recomendada

1. Agregar pruebas de integración con SQLite real para creación, pago dividido,
   rollback, reapertura de proceso y migración desde una base de la versión anterior.
2. Implementar detalle/cancelación auditable e historial con rango y búsqueda.
3. Añadir CRUD de productos y ajustes, con validaciones y desactivación en lugar de
   borrado destructivo.
4. Implementar un updater externo firmado con checksum; la descarga actual no debe
   confundirse con autoactualización completa.
5. Ejecutar una prueba de aceptación en Windows con impresora, permisos de ProgramData,
   corte real, desconexión abrupta y restauración de backup antes del primer uso.
