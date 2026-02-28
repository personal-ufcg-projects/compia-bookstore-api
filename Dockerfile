# ---- Build ----
FROM node:20-alpine AS builder

WORKDIR /app

COPY package*.json ./
COPY prisma ./prisma/

RUN npm ci

COPY . .

RUN npx prisma generate
RUN npm run build

# ---- Production ----
FROM node:20-alpine AS runner

WORKDIR /app

ENV NODE_ENV=production

COPY --from=builder /app/package*.json ./
COPY --from=builder /app/prisma ./prisma/
COPY --from=builder /app/dist ./dist/
COPY --from=builder /app/node_modules ./node_modules/
COPY entrypoint.sh ./
RUN chmod +x entrypoint.sh

EXPOSE 3333

ENTRYPOINT ["./entrypoint.sh"]
