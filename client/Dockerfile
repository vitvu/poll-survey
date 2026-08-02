# Stage 1: Build Vue app
FROM node:20-alpine AS build
WORKDIR /app

ARG VUE_APP_API_BASE_URL=http://localhost:5000
ENV VUE_APP_API_BASE_URL=$VUE_APP_API_BASE_URL

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2: Serve với nginx
FROM nginx:stable-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html

# Config nginx để xử lý SPA routing (fallback về index.html)
RUN printf 'server {\n\
    listen 80;\n\
    root /usr/share/nginx/html;\n\
    index index.html;\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
}\n' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
