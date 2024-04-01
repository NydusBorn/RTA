FROM node:current-alpine
LABEL authors="nydus"

ADD .output /webapp/.output
WORKDIR /webapp/.output

EXPOSE 3000
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=3000

CMD ["node", "server/index.mjs"]