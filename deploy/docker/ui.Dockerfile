FROM node:22-alpine AS build
WORKDIR /app

COPY web/ingest-control-plane/package.json web/ingest-control-plane/package-lock.json ./
RUN npm ci

COPY web/ingest-control-plane/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY deploy/docker/ui.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
