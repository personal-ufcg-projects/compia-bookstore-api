# COMPIA Backend — Guia de Setup

## Pré-requisitos
- .NET 8 SDK instalado
- Docker Desktop instalado e rodando

---

## 1. Suba o PostgreSQL com Docker

```bash
docker-compose up -d
```

Isso sobe:
- **PostgreSQL** na porta 5432
- **pgAdmin** (interface visual) em http://localhost:5050
  - login: admin@compia.com / admin123
  - servidor: host=postgres, user=compia, senha=compia123

---

## 2. Instale os pacotes NuGet

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

---

## 3. Crie a Migration e atualize o banco

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> Se o comando `dotnet ef` não for encontrado:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

---

## 4. Rode o servidor

```bash
dotnet run
```

Acesse o Swagger em: **http://localhost:5000/swagger**

---

## Rotas disponíveis

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | /auth/register | ❌ | Cadastra usuário |
| POST | /auth/login | ❌ | Login → retorna JWT |
| POST | /api/shipping/quote | ❌ | Opções de frete |
| POST | /api/orders | ✅ | Cria pedido |
| GET | /api/orders/me | ✅ | Meus pedidos |
| GET | /api/orders | ✅ Admin | Todos os pedidos |
| PATCH | /api/orders/{id}/status | ✅ Admin | Atualiza status |

---

## Conectar o Frontend

No arquivo `src/services/api/client.ts`, troque os métodos mock por:

```typescript
const API_BASE = "http://localhost:5000";

async getShippingQuote(req) {
  const res = await fetch(`${API_BASE}/api/shipping/quote`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  });
  return res.json();
},

async createOrder(req) {
  const token = localStorage.getItem("token");
  const res = await fetch(`${API_BASE}/api/orders`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`,
    },
    body: JSON.stringify(req),
  });
  return res.json();
},
```

E no `src/hooks/useAuth.tsx`, substitua as chamadas do Supabase por:

```typescript
// Login
const res = await fetch(`${API_BASE}/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});
const data = await res.json();
localStorage.setItem("token", data.token);
```