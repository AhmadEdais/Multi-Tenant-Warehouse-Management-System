FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM caddy:2-alpine
COPY Caddyfile.local /etc/caddy/Caddyfile
COPY Caddyfile.production /etc/caddy/Caddyfile.production
COPY --from=build /src/dist/wms-frontend/browser/ /srv/
