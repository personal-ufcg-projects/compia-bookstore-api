import { FastifyInstance } from "fastify";
import { prisma } from "../lib/prisma";

export async function statsRoutes(app: FastifyInstance) {
  // GET /api/stats - métricas para o dashboard admin
  app.get("/", async (_request, reply) => {
    const now = new Date();
    const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

    const [
      totalProducts,
      ordersThisMonth,
      revenueThisMonth,
      recentLogs,
      ordersByStatus,
    ] = await prisma.$transaction([
      prisma.product.count(),

      prisma.order.count({
        where: { createdAt: { gte: startOfMonth } },
      }),

      prisma.order.aggregate({
        _sum: { total: true },
        where: {
          createdAt: { gte: startOfMonth },
          status: { not: "Cancelado" },
        },
      }),

      prisma.activityLog.findMany({
        orderBy: { createdAt: "desc" },
        take: 20,
      }),

      prisma.order.groupBy({
        by: ["status"],
        _count: { status: true },
      }),
    ]);

    // Calcula crescimento de receita vs mês anterior
    const startOfLastMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const endOfLastMonth = new Date(now.getFullYear(), now.getMonth(), 0);

    const revenueLastMonth = await prisma.order.aggregate({
      _sum: { total: true },
      where: {
        createdAt: { gte: startOfLastMonth, lte: endOfLastMonth },
        status: { not: "Cancelado" },
      },
    });

    const currentRevenue = Number(revenueThisMonth._sum.total ?? 0);
    const lastRevenue = Number(revenueLastMonth._sum.total ?? 0);
    const growth =
      lastRevenue === 0
        ? null
        : (((currentRevenue - lastRevenue) / lastRevenue) * 100).toFixed(1);

    return reply.send({
      totalProducts,
      ordersThisMonth,
      revenueThisMonth: currentRevenue,
      growth: growth ? `${Number(growth) > 0 ? "+" : ""}${growth}%` : null,
      ordersByStatus: Object.fromEntries(
        ordersByStatus.map((s) => [s.status, s._count.status])
      ),
      recentLogs,
    });
  });
}
