import { FastifyInstance } from "fastify";
import { z } from "zod";
import { prisma } from "../lib/prisma";

const createOrderSchema = z.object({
  customerName: z.string().min(1),
  customerEmail: z.string().email(),
  items: z
    .array(
      z.object({
        productId: z.string().uuid(),
        quantity: z.number().int().positive(),
      })
    )
    .min(1),
});

export async function ordersRoutes(app: FastifyInstance) {
  // GET /api/orders - lista todos os pedidos (admin)
  app.get("/", async (request, reply) => {
    const querySchema = z.object({
      status: z.string().optional(),
      page: z.string().transform(Number).default("1"),
      limit: z.string().transform(Number).default("20"),
    });

    const { status, page, limit } = querySchema.parse(request.query);

    const [orders, total] = await prisma.$transaction([
      prisma.order.findMany({
        where: { ...(status && { status: status as any }) },
        include: {
          items: {
            include: { product: { select: { title: true } } },
          },
        },
        orderBy: { createdAt: "desc" },
        skip: (page - 1) * limit,
        take: limit,
      }),
      prisma.order.count({
        where: { ...(status && { status: status as any }) },
      }),
    ]);

    return reply.send({ orders, total, page, limit });
  });

  // GET /api/orders/:id
  app.get("/:id", async (request, reply) => {
    const { id } = z.object({ id: z.string().uuid() }).parse(request.params);

    const order = await prisma.order.findUnique({
      where: { id },
      include: {
        items: {
          include: { product: true },
        },
      },
    });

    if (!order) {
      return reply.status(404).send({ error: "Pedido não encontrado" });
    }

    return reply.send(order);
  });

  // POST /api/orders - criar pedido (checkout)
  app.post("/", async (request, reply) => {
    const body = createOrderSchema.parse(request.body);

    // Busca todos os produtos de uma vez
    const products = await prisma.product.findMany({
      where: { id: { in: body.items.map((i) => i.productId) } },
    });

    if (products.length !== body.items.length) {
      return reply.status(400).send({ error: "Um ou mais produtos não encontrados" });
    }

    // Verifica estoque
    for (const item of body.items) {
      const product = products.find((p) => p.id === item.productId)!;
      if (!product.inStock || (product.stockCount < 999 && product.stockCount < item.quantity)) {
        return reply.status(400).send({
          error: `Produto "${product.title}" sem estoque suficiente`,
        });
      }
    }

    // Calcula total
    const total = body.items.reduce((sum, item) => {
      const product = products.find((p) => p.id === item.productId)!;
      return sum + Number(product.price) * item.quantity;
    }, 0);

    // Cria pedido e decrementa estoque em transação
    const order = await prisma.$transaction(async (tx) => {
      const newOrder = await tx.order.create({
        data: {
          customerName: body.customerName,
          customerEmail: body.customerEmail,
          total,
          items: {
            create: body.items.map((item) => {
              const product = products.find((p) => p.id === item.productId)!;
              return {
                productId: item.productId,
                quantity: item.quantity,
                priceAtTime: product.price,
              };
            }),
          },
        },
        include: { items: true },
      });

      // Decrementa estoque (ignora e-books com stockCount = 999)
      for (const item of body.items) {
        const product = products.find((p) => p.id === item.productId)!;
        if (product.stockCount < 999) {
          await tx.product.update({
            where: { id: item.productId },
            data: {
              stockCount: { decrement: item.quantity },
              inStock: { set: product.stockCount - item.quantity > 0 },
            },
          });
        }
      }

      return newOrder;
    });

    await prisma.activityLog.create({
      data: {
        action: "CREATE",
        entity: "Order",
        entityId: order.id,
        details: `Pedido de ${body.customerName} - R$ ${total.toFixed(2)}`,
      },
    });

    return reply.status(201).send(order);
  });

  // PATCH /api/orders/:id/status - atualizar status (admin)
  app.patch("/:id/status", async (request, reply) => {
    const { id } = z.object({ id: z.string().uuid() }).parse(request.params);
    const { status } = z
      .object({
        status: z.enum(["Processando", "Em_transito", "Entregue", "Cancelado"]),
      })
      .parse(request.body);

    const existing = await prisma.order.findUnique({ where: { id } });
    if (!existing) {
      return reply.status(404).send({ error: "Pedido não encontrado" });
    }

    const order = await prisma.order.update({
      where: { id },
      data: { status },
    });

    await prisma.activityLog.create({
      data: {
        action: "UPDATE",
        entity: "Order",
        entityId: id,
        details: `Status → ${status}`,
      },
    });

    return reply.send(order);
  });
}
