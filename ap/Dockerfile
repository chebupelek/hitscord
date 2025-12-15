FROM node:18-alpine AS build
WORKDIR /app
COPY ap/package*.json ./
RUN npm install
COPY ap/. . 
COPY .env .env
RUN npm run build
