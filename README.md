# compia-bookstore-api

API REST para a COMPIA Bookstore. Node.js + Fastify + Prisma + PostgreSQL.

## Stack

| Tecnologia | Versão  | Função             |
|------------|---------|--------------------|
| Node.js    | 20      | Runtime            |
| Fastify    | 4       | HTTP framework     |
| Prisma     | 5       | ORM + migrations   |
| PostgreSQL | 15      | Banco de dados     |
| TypeScript | 5       | Tipagem            |
| Zod        | 3       | Validação          |

---

## Rodar localmente (sem Docker)

```bash
cp .env.example .env        # edite se necessário
npm install
npx prisma migrate dev --name init
npx prisma db seed
npm run dev                 # http://localhost:3333
```

## Rodar com Docker

```bash
# Desenvolvimento (hot reload)
docker compose -f docker-compose.dev.yml up

# Produção
docker compose up --build
```

Na primeira inicialização o seed popula automaticamente os produtos e pedidos de exemplo.

---

## Endpoints

### Products

| Método | Rota                  | Descrição                          |
|--------|-----------------------|------------------------------------|
| GET    | /api/products         | Lista produtos                     |
| GET    | /api/products/:id     | Produto por ID                     |
| POST   | /api/products         | Criar produto                      |
| PUT    | /api/products/:id     | Atualizar produto                  |
| DELETE | /api/products/:id     | Deletar produto                    |

**Query params em GET /api/products:**

| Param    | Valores possíveis                                                              |
|----------|--------------------------------------------------------------------------------|
| category | Inteligencia_Artificial, Blockchain, Ciberseguranca, Machine_Learning, Data_Science |
| format   | Fisico, Ebook, Kit                                                             |
| search   | string livre (busca em título, autor e descrição)                              |
| inStock  | true / false                                                                   |

### Orders

| Método | Rota                     | Descrição                    |
|--------|--------------------------|------------------------------|
| GET    | /api/orders              | Lista pedidos                |
| GET    | /api/orders/:id          | Pedido por ID                |
| POST   | /api/orders              | Criar pedido (checkout)      |
| PATCH  | /api/orders/:id/status   | Atualizar status             |

**Query params em GET /api/orders:** `status`, `page`, `limit`

**Status válidos:** `Processando`, `Em_transito`, `Entregue`, `Cancelado`

### Stats

| Método | Rota        | Descrição                                  |
|--------|-------------|--------------------------------------------|
| GET    | /api/stats  | Métricas do dashboard (produtos, pedidos, receita, crescimento, logs) |

### Health

```
GET /health → { status: "ok", timestamp: "..." }
```

---

## Variáveis de ambiente

| Variável       | Padrão                                          | Descrição                         |
|----------------|-------------------------------------------------|-----------------------------------|
| DATABASE_URL   | postgresql://compia:compia@localhost:5432/compia_db | String de conexão Prisma      |
| PORT           | 3333                                            | Porta do servidor                 |
| HOST           | 0.0.0.0                                         | Interface de escuta               |
| NODE_ENV       | development                                     | Ambiente                          |
| CORS_ORIGIN    | http://localhost:5173                           | URL do frontend autorizada        |

---

## Scripts

```bash
npm run dev          # sobe com hot reload (tsx watch)
npm run build        # compila TypeScript
npm run start        # roda o build compilado
npm run db:migrate   # aplica migrations pendentes
npm run db:seed      # popula dados iniciais
npm run db:studio    # abre Prisma Studio (UI do banco)
npm run db:reset     # reseta banco e re-aplica tudo (⚠️ apaga dados)
```
