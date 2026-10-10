# Stage 1: Build React SPA với pnpm
FROM node:22-alpine AS build
WORKDIR /app

# Bật pnpm thông qua corepack có sẵn của Node.js
RUN corepack enable

# Copy file định nghĩa package và lockfile của pnpm
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
RUN pnpm run build

# Stage 2: Nginx phục vụ file tĩnh trong mạng nội bộ
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx-internal.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]