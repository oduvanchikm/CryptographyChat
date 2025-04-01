# Используем официальный образ Node.js для сборки проекта
FROM node:18 AS build

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем package.json и package-lock.json из текущей директории (frontend/)
COPY frontend/package*.json ./

# Устанавливаем зависимости
RUN npm install

# Копируем весь проект из директории frontend в рабочую директорию
COPY frontend/ /app/

# Собираем React-приложение
RUN npm run build

# Создаем финальный образ для запуска с Nginx
FROM nginx:alpine

# Копируем собранное приложение в директорию для статики Nginx
COPY --from=build /app/build /usr/share/nginx/html

# Открываем порт 80 для Nginx
EXPOSE 80

# Запускаем Nginx
CMD ["nginx", "-g", "daemon off;"]
