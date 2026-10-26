FROM node:22 AS build
WORKDIR /src/Front

RUN curl -fsSL https://bun.sh/install | bash
ENV BUN_INSTALL=/root/.bun
ENV PATH=$BUN_INSTALL/bin:$PATH

COPY Front/package.json Front/bun.lock ./
RUN bun install --frozen-lockfile

COPY Front/ ./
RUN bun run build

FROM nginx:1.27-alpine
WORKDIR /usr/share/nginx/html

ARG APP_ENV=prod
ARG VERSION
ARG RUN_NUMBER
LABEL version="${VERSION}"
LABEL run_number="${RUN_NUMBER}"

COPY --from=build /src/Front/dist/Front/browser/ ./
COPY Front/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
