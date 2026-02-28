import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

async function main() {
  console.log("🌱 Seeding database...");

  // Limpa tudo antes de seedar (útil em dev)
  await prisma.orderItem.deleteMany();
  await prisma.order.deleteMany();
  await prisma.product.deleteMany();
  await prisma.activityLog.deleteMany();

  const products = await prisma.product.createMany({
    data: [
      {
        title: "Fundamentos de Inteligência Artificial",
        author: "Dr. Carlos Mendes",
        price: 89.9,
        originalPrice: 129.9,
        format: "Fisico",
        category: "Inteligencia_Artificial",
        imageUrl:
          "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 45,
        description: "Uma introdução completa aos conceitos fundamentais de IA.",
      },
      {
        title: "Deep Learning na Prática",
        author: "Ana Paula Silva",
        price: 64.9,
        format: "Ebook",
        category: "Machine_Learning",
        imageUrl:
          "https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 999,
        description: "Guia prático para implementação de redes neurais profundas.",
      },
      {
        title: "Blockchain: Do Zero ao Avançado",
        author: "Ricardo Oliveira",
        price: 149.9,
        originalPrice: 199.9,
        format: "Kit",
        category: "Blockchain",
        imageUrl:
          "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 12,
        description: "Kit completo com livro físico + e-book + exercícios.",
      },
      {
        title: "Cibersegurança Moderna",
        author: "Fernanda Costa",
        price: 79.9,
        format: "Fisico",
        category: "Ciberseguranca",
        imageUrl:
          "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=400&h=560&fit=crop",
        inStock: false,
        stockCount: 0,
        description: "Técnicas avançadas de proteção digital.",
      },
      {
        title: "Python para Data Science",
        author: "Marcos Almeida",
        price: 54.9,
        format: "Ebook",
        category: "Data_Science",
        imageUrl:
          "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 999,
        description: "Aprenda Python aplicado à ciência de dados.",
      },
      {
        title: "Redes Neurais e NLP",
        author: "Juliana Torres",
        price: 94.9,
        originalPrice: 119.9,
        format: "Fisico",
        category: "Inteligencia_Artificial",
        imageUrl:
          "https://images.unsplash.com/photo-1655720828018-edd71de0b5ce?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 28,
        description: "Processamento de linguagem natural com redes neurais.",
      },
      {
        title: "Smart Contracts com Solidity",
        author: "Pedro Henrique",
        price: 69.9,
        format: "Ebook",
        category: "Blockchain",
        imageUrl:
          "https://images.unsplash.com/photo-1642104704074-907c0698cbd9?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 999,
        description: "Desenvolvimento de contratos inteligentes na Ethereum.",
      },
      {
        title: "Kit Machine Learning Completo",
        author: "COMPIA Editora",
        price: 189.9,
        originalPrice: 249.9,
        format: "Kit",
        category: "Machine_Learning",
        imageUrl:
          "https://images.unsplash.com/photo-1515879218367-8466d910auj7?w=400&h=560&fit=crop",
        inStock: true,
        stockCount: 8,
        description: "3 livros + materiais exclusivos sobre ML.",
      },
    ],
  });

  console.log(`✅ ${products.count} produtos criados`);

  // Seed de alguns pedidos de exemplo
  const allProducts = await prisma.product.findMany();

  const order1 = await prisma.order.create({
    data: {
      customerName: "João Silva",
      customerEmail: "joao@example.com",
      status: "Entregue",
      total: 154.8,
      createdAt: new Date("2026-02-22"),
      items: {
        create: [
          {
            productId: allProducts[0].id,
            quantity: 1,
            priceAtTime: allProducts[0].price,
          },
          {
            productId: allProducts[1].id,
            quantity: 1,
            priceAtTime: allProducts[1].price,
          },
        ],
      },
    },
  });

  const order2 = await prisma.order.create({
    data: {
      customerName: "Maria Santos",
      customerEmail: "maria@example.com",
      status: "Em_transito",
      total: 189.9,
      createdAt: new Date("2026-02-21"),
      items: {
        create: [
          {
            productId: allProducts[7].id,
            quantity: 1,
            priceAtTime: allProducts[7].price,
          },
        ],
      },
    },
  });

  const order3 = await prisma.order.create({
    data: {
      customerName: "Pedro Costa",
      customerEmail: "pedro@example.com",
      status: "Processando",
      total: 79.9,
      createdAt: new Date("2026-02-20"),
      items: {
        create: [
          {
            productId: allProducts[3].id,
            quantity: 1,
            priceAtTime: allProducts[3].price,
          },
        ],
      },
    },
  });

  console.log(`✅ 3 pedidos de exemplo criados`);

  await prisma.activityLog.createMany({
    data: [
      { action: "CREATE", entity: "Order", entityId: order1.id, details: "Pedido criado via seed" },
      { action: "UPDATE", entity: "Order", entityId: order1.id, details: "Status → Entregue" },
      { action: "CREATE", entity: "Order", entityId: order2.id, details: "Pedido criado via seed" },
      { action: "CREATE", entity: "Order", entityId: order3.id, details: "Pedido criado via seed" },
    ],
  });

  console.log("✅ Logs de atividade criados");
  console.log("🎉 Seed concluído!");
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
