import Fastify from "fastify";
import cors from "@fastify/cors";
import { productsRoutes } from "./routes/products";
import { ordersRoutes } from "./routes/orders";
import { statsRoutes } from "./routes/stats";
import { registerErrorHandler } from "./middleware/errorHandler";

const app = Fastify({
  logger: {
    transport:
      process.env.NODE_ENV === "development"
        ? { target: "pino-pretty", options: { colorize: true } }
        : undefined,
  },
});

async function bootstrap() {
  // CORS — permite o frontend (Vite dev server)
  await app.register(cors, {
    origin: process.env.CORS_ORIGIN ?? "http://localhost:5173",
    methods: ["GET", "POST", "PUT", "PATCH", "DELETE"],
  });

  // Error handler global
  registerErrorHandler(app);

  // Rotas
  await app.register(productsRoutes, { prefix: "/api/products" });
  await app.register(ordersRoutes, { prefix: "/api/orders" });
  await app.register(statsRoutes, { prefix: "/api/stats" });

  // Health check
  app.get("/health", async () => ({ status: "ok", timestamp: new Date().toISOString() }));

  const port = Number(process.env.PORT ?? 3333);
  const host = process.env.HOST ?? "0.0.0.0";

  await app.listen({ port, host });
  app.log.info(`🚀 Backend rodando em http://${host}:${port}`);
}

bootstrap().catch((err) => {
  console.error(err);
  process.exit(1);
});
