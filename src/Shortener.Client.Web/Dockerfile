# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
# VITE_SHORT_LINK_ORIGIN defaults to window.location.origin at runtime when unset
RUN npm run build

# Runtime stage - nginx to serve static assets and proxy API
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Remove default nginx static content
RUN rm -rf /usr/share/nginx/html/*

# Copy built assets from build stage
COPY --from=build /app/dist .

# Custom nginx config: serve SPA, proxy /api and short-code redirects to the API
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
