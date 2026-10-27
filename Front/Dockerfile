FROM node:22 AS build
WORKDIR /src/Front

RUN curl -fsSL https://bun.sh/install | bash
ENV BUN_INSTALL=/root/.bun
ENV PATH=$BUN_INSTALL/bin:$PATH

COPY Front/package.json Front/bun.lock Front/.npmrc ./
RUN --mount=type=secret,id=NODE_AUTH_TOKEN \
    printf '\n//npm.pkg.github.com/:_authToken=%s\n' "$(cat /run/secrets/NODE_AUTH_TOKEN)" >> .npmrc \
    && bun install --frozen-lockfile \
    && rm -f .npmrc

COPY Front/ ./
ARG VERSION
RUN if [ -n "$VERSION" ]; then printf 'export const siteVersion = "%s";\n' "$VERSION" > src/app/version.ts; fi \
    && bun run build

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
