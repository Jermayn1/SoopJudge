# Frontend: Node baut, nginx liefert aus und ist zugleich der Reverse Proxy
# vor dem Backend. Dieser Container ist der EINZIGE, der einen Port
# veröffentlicht.
#
# Build-Kontext ist dieser Ordner (context: ./src/SoopWorkshop.Frontend).

# ---------------------------------------------------------------- Bauen -------
FROM node:22-alpine AS build
WORKDIR /quelle

# Erst die Paketlisten, damit die Installationsschicht im Cache bleibt, solange
# sich keine Abhängigkeit ändert. npm ci statt npm install: es hält sich
# streng an package-lock.json, statt Versionen neu aufzulösen.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# LEER, und das ist die eigentliche Aussage: das Frontend spricht die API über
# denselben Ursprung an (relative Pfade), weil nginx unten /api weiterreicht.
# Damit steckt kein Hostname im Image, und dasselbe Image läuft unter jeder
# IP und jedem Namen.
#
# Die leere Zeichenkette muss ankommen - client.ts unterscheidet ausdrücklich
# zwischen "nicht gesetzt" (Entwicklung, localhost:5120) und "leer" (gleicher
# Ursprung).
ENV VITE_API_URL=""

# Enthält tsc -b: ein Typfehler gegen den erzeugten API-Vertrag lässt den
# Image-Build fallen, statt erst im Browser aufzufallen.
RUN npm run build

# --------------------------------------------------------------- Laufen -------
FROM nginx:alpine AS final

COPY --from=build /quelle/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

# Kein eigener Entrypoint: das nginx-Image startet von selbst richtig. Der
# Aufbau liefert reines http aus (Begründung in der README unter "Grenzen"),
# es gibt also kein Zertifikat zu erzeugen, einzuhängen oder zu prüfen - und
# openssl braucht das Image damit ebenfalls nicht.
