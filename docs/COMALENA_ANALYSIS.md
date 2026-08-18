# Análisis funcional de Comaleña y decisiones para CafePOS

## Arquitectura encontrada

Comaleña es una aplicación Android monolítica por capas informales: Compose en `ui`,
`AndroidViewModel` con `StateFlow`, repositorios que construyen directamente la base
Room, entidades/DAO en `db`, y modelos de carrito en memoria. No existe una capa de
casos de uso ni inyección de dependencias. `AppDatabase` es un singleton global.

La venta nace en `CartViewModel`: reserva bases por tamaño, agrega pizzas simples o
porciones combinadas, postres/bebidas/extras, envío y comentarios; calcula el total;
serializa carrito y cliente a JSON; y `OrderRepository.addOrder` asigna un consecutivo
diario e inserta la orden y sus renglones normalizados dentro de una transacción.

## Reglas y capacidades observadas

* **Catálogo:** ingredientes, pizzas, precios por tamaño y extras.
  Una pizza combinada cuesta el precio más alto de sus sabores. Se impide agregarla
  sin una base disponible y al vender se consume la base más antigua.
* **Órdenes:** consecutivo diario, cliente (nombre/teléfono), comentarios, dirección,
  productos y total. El historial filtra por día y permite editar, reimprimir y hacer
  borrado lógico. Al cancelar se intenta devolver la base consumida.
* **Pagos:** `PaymentPart` admite `EFECTIVO` y `TRANSFERENCIA`, referencia opcional y
  varios componentes. Pagado significa que la suma alcanza el total (en un lugar se
  tolera un epsilon). Puede limpiarse o sustituirse el importe de un método. Los pagos
  viven como JSON en la orden, no como registros auditables independientes.
* **Entrega:** `PASAN`, `CAMINANDO`, `DOMICILIO` y `TOTODO`; agrega cargo/zona/dirección.
  TOTODO aplica 10 % de descuento y redondea a entero, además de no consumir bases.
* **Caja:** `TransactionEntity` registra `INGRESO` o `GASTO`, separado de órdenes. La
  vista calcula ventas por método, ingresos/gastos manuales, categorías, delivery y
  exporta CSV. No hay una entidad durable de sesión de caja/apertura/corte.
* **Impresión:** existe en Android mediante Bluetooth y tickets separados, pero queda excluida de la recreación de escritorio.
* **Métricas:** rangos de fecha, KPI, tendencias y rankings. Los usuarios
  son clientes en memoria; no existe autenticación ni empleados persistentes.
* **Riesgos:** dinero en `Double`; JSON duplicado junto a `order_items`; actualización
  de orden no resincroniza renglones; límites diarios suman 86 400 000 ms (DST);
  `fallbackToDestructiveMigration` puede destruir datos y no hay idempotencia de cobros.

## Mapeo conceptual

| Comaleña real | CafePOS |
|---|---|
| `OrderEntity` | `Order` |
| `OrderItemEntity` | `OrderItem` (snapshot de nombre/precio) |
| `PaymentPart` JSON | `Payment` normalizado y auditable |
| `TransactionEntity` / `TransactionType` | `CashMovement` / `CashMovementType` |
| Sin entidad de corte | `CashSession` con apertura, cierre y balance esperado |
| `PizzaEntity`, `PizzaSizeEntity`, `ExtraEntity` | `Product`; variantes/modificadores quedan como extensión futura |
| `CartItem`, `CartItemPostre` | borrador de `Order` + `OrderItem` |
| `User` (cliente) | `Employee` para vendedor; cliente futuro separado |
| Room + DAO | EF Core + SQLite + servicios de aplicación |
| repositorios creados en ViewModels | DI y `IDbContextFactory` |
| Compose / `AndroidViewModel` | Avalonia / MVVM Toolkit |
| JSON de pagos | relación `Order` → `Payment` |
| borrado lógico `isDeleted` | estado `Cancelled`, conservando auditoría |

## Qué conservar y qué no

Se conservan el pago dividido, snapshots de producto, consecutivo diario, comentarios,
historial por rango, movimientos separados de ventas y cancelación auditable. También
se deja el modelo preparado para variantes/modificadores, sin implementar complejidad
de pizza que una cafetería aún no necesita.

No se copian JSON como fuente de verdad, `Double`, singletons Android, repositorios
acoplados a `Context`, migración destructiva, lógica de calendario por milisegundos,
ni impresión ni flujo Bluetooth. Delivery, clientes y métricas avanzadas
quedan explícitamente fuera del MVP, no bloqueados por el diseño.

## Arquitectura propuesta y etapas

La solución usa cuatro proyectos productivos y uno de pruebas. Domain contiene reglas
sin infraestructura; Application contiene contratos y servicios transaccionales;
Infrastructure contiene EF, migraciones, backups, archivos y releases;
Desktop contiene únicamente composición DI, MVVM y Avalonia. Es suficiente separación
para testear integridad sin introducir CQRS, bus de mensajes o repositorio genérico.

1. **Fundación (incluida):** dominio, EF/SQLite, migración destructiva a cafetería,
   rutas persistentes, backup/rotación, configuración, logs, DI y datos iniciales.
2. **MVP (incluido):** catálogo genérico, carrito, pagos
   totales (efectivo/tarjeta), entregas, historial y caja.
3. **Distribución (incluida como base):** consulta GitHub Releases, workflow win-x64
   self-contained y release por tag. La instalación desatendida queda deliberadamente
   en un proceso externo futuro: reemplazar un ejecutable en uso desde sí mismo no es
   confiable; el MVP descarga el paquete fuera del directorio de datos.
4. **Posterior:** updater firmado, backup diario programado, roles y variantes/modificadores.
