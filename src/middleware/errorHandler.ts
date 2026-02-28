import { FastifyInstance } from "fastify";
import { ZodError } from "zod";

export function registerErrorHandler(app: FastifyInstance) {
  app.setErrorHandler((error, _request, reply) => {
    // Erros de validação Zod
    if (error instanceof ZodError) {
      return reply.status(400).send({
        error: "Dados inválidos",
        issues: error.flatten().fieldErrors,
      });
    }

    // Erros do Prisma
    if (error.constructor.name === "PrismaClientKnownRequestError") {
      const prismaError = error as any;
      if (prismaError.code === "P2002") {
        return reply.status(409).send({ error: "Registro duplicado" });
      }
      if (prismaError.code === "P2025") {
        return reply.status(404).send({ error: "Registro não encontrado" });
      }
    }

    app.log.error(error);

    return reply.status(500).send({
      error: "Erro interno do servidor",
      ...(process.env.NODE_ENV === "development" && { message: error.message }),
    });
  });
}
