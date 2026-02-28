import { FastifyInstance } from "fastify";
import { z } from "zod";
import { prisma } from "../lib/prisma";

const productBodySchema = z.object({
  title: z.string().min(1),
  author: z.string().min(1),
  price: z.number().positive(),
  originalPrice: z.number().positive().optional(),
  format: z.enum(["Fisico", "Ebook", "Kit"]),
  category: z.enum([
    "Inteligencia_Artificial",
    "Blockchain",
    "Ciberseguranca",
    "Machine_Learning",
    "Data_Science",
  ]),
  imageUrl: z.string().url(),
  inStock: z.boolean().default(true),
  stockCount: z.number().int().min(0).default(0),
  description: z.string().min(1),
});

export async function productsRoutes(app: FastifyInstance) {
  // GET /api/products - lista com filtros opcionais
  app.get("/", async (request, reply) => {
    const querySchema = z.object({
      category: z.string().optional(),
      format: z.string().optional(),
      search: z.string().optional(),
      inStock: z
        .string()
        .transform((v) => v === "true")
        .optional(),
    });

    const query = querySchema.parse(request.query);

    const products = await prisma.product.findMany({
      where: {
        ...(query.category && { category: query.category as any }),
        ...(query.format && { format: query.format as any }),
        ...(query.inStock !== undefined && { inStock: query.inStock }),
        ...(query.search && {
          OR: [
            { title: { contains: query.search, mode: "insensitive" } },
            { author: { contains: query.search, mode: "insensitive" } },
            { description: { contains: query.search, mode: "insensitive" } },
          ],
        }),
      },
      orderBy: { createdAt: "desc" },
    });

    return reply.send(products);
  });

  // GET /api/products/:id
  app.get("/:id", async (request, reply) => {
    const { id } = z.object({ id: z.string().uuid() }).parse(request.params);

    const product = await prisma.product.findUnique({ where: { id } });

    if (!product) {
      return reply.status(404).send({ error: "Produto não encontrado" });
    }

    return reply.send(product);
  });

  // POST /api/products
  app.post("/", async (request, reply) => {
    const body = productBodySchema.parse(request.body);

    const product = await prisma.product.create({ data: body });

    await prisma.activityLog.create({
      data: { action: "CREATE", entity: "Product", entityId: product.id, details: product.title },
    });

    return reply.status(201).send(product);
  });

  // PUT /api/products/:id
  app.put("/:id", async (request, reply) => {
    const { id } = z.object({ id: z.string().uuid() }).parse(request.params);
    const body = productBodySchema.partial().parse(request.body);

    const existing = await prisma.product.findUnique({ where: { id } });
    if (!existing) {
      return reply.status(404).send({ error: "Produto não encontrado" });
    }

    const product = await prisma.product.update({ where: { id }, data: body });

    await prisma.activityLog.create({
      data: { action: "UPDATE", entity: "Product", entityId: product.id, details: product.title },
    });

    return reply.send(product);
  });

  // DELETE /api/products/:id
  app.delete("/:id", async (request, reply) => {
    const { id } = z.object({ id: z.string().uuid() }).parse(request.params);

    const existing = await prisma.product.findUnique({ where: { id } });
    if (!existing) {
      return reply.status(404).send({ error: "Produto não encontrado" });
    }

    await prisma.product.delete({ where: { id } });

    await prisma.activityLog.create({
      data: { action: "DELETE", entity: "Product", entityId: id, details: existing.title },
    });

    return reply.status(204).send();
  });
}
