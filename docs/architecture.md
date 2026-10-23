# Arquitectura

## Dependencias entre capas

Las referencias entre proyectos son solo estas: Application → Domain, Infrastructure → Application y Domain, Api → Application e Infrastructure.

```mermaid
flowchart LR
    Api[Restaurant.Api] --> Application[Restaurant.Application]
    Api --> Infrastructure[Restaurant.Infrastructure]
    Infrastructure --> Application
    Infrastructure --> Domain[Restaurant.Domain]
    Application --> Domain
```

## Diagrama de estados de mesa

```mermaid
stateDiagram-v2
    [*] --> Available
    Available --> InService: StartService (al abrir una orden)
    InService --> Served: MarkServed (al completar la orden)
    Served --> Available: Release (PATCH /tables/{id}/release)
```

## Diagrama de estados de orden

```mermaid
stateDiagram-v2
    [*] --> InProgress: Open
    InProgress --> InProgress: AddItem / RemoveItem
    InProgress --> Completed: Complete
    Completed --> [*]
```

## Modelo de datos

```mermaid
erDiagram
    USERS ||--o{ ORDERS : "abre (waiter_id)"
    TABLES ||--o{ ORDERS : "recibe (table_id)"
    ORDERS ||--|{ ORDER_ITEMS : contiene
    DISHES ||--o{ ORDER_ITEMS : "se pide (dish_id)"
    DISHES ||--|{ DISH_INGREDIENTS : "usa"
    INGREDIENTS ||--o{ DISH_INGREDIENTS : "está en (RESTRICT)"

    USERS {
        uuid id PK
        string email
        bool is_active
    }
    INGREDIENTS {
        uuid id PK
        string name
        string normalized_name UK
    }
    DISHES {
        uuid id PK
        string name
        string normalized_name UK
        numeric price
        int servings
        string category
        bool is_active
    }
    DISH_INGREDIENTS {
        uuid dish_id PK
        uuid ingredient_id PK
    }
    TABLES {
        uuid id PK
        int number UK
        int capacity
        string description
        string status
        xid xmin
    }
    ORDERS {
        uuid id PK
        uuid table_id FK
        uuid waiter_id FK
        string status
        numeric subtotal
        timestamptz created_at
        timestamptz completed_at
        xid xmin
    }
    ORDER_ITEMS {
        uuid id PK
        uuid order_id FK
        uuid dish_id FK
        string dish_name
        numeric unit_price
        int quantity
    }
```

Índices que no aparecen en el diagrama: `orders(table_id, status)` y el índice único parcial `orders(table_id) WHERE status = 'InProgress'`, que garantiza una sola orden activa por mesa.

## Secuencia abrir orden

```mermaid
sequenceDiagram
    actor W as Waiter
    participant API as Endpoint POST /orders
    participant H as OpenOrderHandler
    participant T as ITableRepository
    participant D as IDishRepository
    participant O as Order (dominio)
    participant DB as PostgreSQL

    W->>API: POST /orders { tableId, items }
    API->>H: HandleAsync(command)
    H->>T: GetByIdAsync(tableId)
    H->>D: GetByIdsAsync(dishIds)
    H->>O: dish.ToOrderLine(qty) (422 si el plato está inactivo)
    H->>T: table.StartService() (422 si la mesa no está Available)
    H->>O: Order.Open(tableId, waiterId, lines, now)
    H->>DB: SaveChangesAsync() (una sola transacción)
    DB-->>H: OK (409 si otra orden activa ganó la carrera)
    H-->>API: OrderResponse
    API-->>W: 201 Created
```
