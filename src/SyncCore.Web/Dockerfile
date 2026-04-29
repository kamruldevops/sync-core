# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Env vars are injected at build time via --build-arg
ARG VITE_API_BASE_URL=http://localhost:5001
ARG VITE_B2C_CLIENT_ID=
ARG VITE_B2C_INSTANCE=
ARG VITE_B2C_DOMAIN=
ARG VITE_B2C_POLICY=B2C_1_signupsignin
ARG VITE_B2C_SCOPE=
ARG VITE_B2C_REDIRECT_URI=http://localhost:5173
ARG VITE_B2C_KNOWN_AUTHORITY=

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_B2C_CLIENT_ID=$VITE_B2C_CLIENT_ID
ENV VITE_B2C_INSTANCE=$VITE_B2C_INSTANCE
ENV VITE_B2C_DOMAIN=$VITE_B2C_DOMAIN
ENV VITE_B2C_POLICY=$VITE_B2C_POLICY
ENV VITE_B2C_SCOPE=$VITE_B2C_SCOPE
ENV VITE_B2C_REDIRECT_URI=$VITE_B2C_REDIRECT_URI
ENV VITE_B2C_KNOWN_AUTHORITY=$VITE_B2C_KNOWN_AUTHORITY

RUN npm run build

# --- Runtime stage ---
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html

# Single-page app routing: fall back to index.html for all routes
RUN printf 'server {\n\
    listen 80;\n\
    root /usr/share/nginx/html;\n\
    index index.html;\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
}\n' > /etc/nginx/conf.d/default.conf

EXPOSE 80
