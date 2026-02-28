#!/bin/sh
set -e

echo "⏳ Aguardando banco de dados..."

until npx prisma db push --accept-data-loss 2>/dev/null; do
  echo "🔄 Banco não disponível, tentando em 2s..."
  sleep 2
done

echo "✅ Banco disponível"
echo "🔄 Rodando migrations..."
npx prisma migrate deploy

echo "🌱 Verificando seed..."
node -e "
const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();
prisma.product.count().then(count => {
  process.exit(count === 0 ? 0 : 1);
}).catch(() => process.exit(0));
" && npx tsx prisma/seed.ts || true

echo "🚀 Iniciando servidor..."
exec node dist/index.js
