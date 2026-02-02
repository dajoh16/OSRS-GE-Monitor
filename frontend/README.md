# OSRS GE Monitor Frontend

## Prerequisites

- Node.js 18+ with npm

## Local development

```bash
cd frontend
npm install
npm run dev
```

The app runs on `http://localhost:5173` by default. To point at a backend running elsewhere,
set `VITE_API_BASE_URL` in your environment.

```bash
VITE_API_BASE_URL=http://localhost:8080 npm run dev
```

## Production build

```bash
npm run build
npm run preview
```

## Docker

Build the image and run the dev server in a container:

```bash
docker build -t osrs-ge-monitor-frontend .
docker run --rm -p 5173:5173 -e VITE_API_BASE_URL=http://host.docker.internal:8080 osrs-ge-monitor-frontend
```

If you need to install dependencies with a private registry, configure the appropriate npm
settings during the build.
