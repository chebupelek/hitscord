FROM node:18-alpine AS build
WORKDIR /app
COPY ap/package*.json ./
RUN npm install
COPY ap/ ./
RUN npm run build
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]