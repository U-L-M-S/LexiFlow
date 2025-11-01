FROM node:20-alpine AS build
WORKDIR /app

ENV npm_config_update_notifier=false

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS final

RUN apk add --no-cache curl gettext

WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist .
RUN cp config.js config.template.js

COPY nginx.conf /etc/nginx/nginx.conf
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
