# 1. Stage: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY . .
RUN npm install && npm run build

# 2. Stage: Serve with nginx
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
