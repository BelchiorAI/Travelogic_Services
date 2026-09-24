# Welcome to your Lovable project

This project was built with [Lovable](https://lovable.dev).

## Build with Lovable

Open your project in the [Lovable editor](https://lovable.dev) and keep building.

- **Ship faster**: describe what you want to build and Lovable handles the code.
- **Stay in sync**: connect the project to GitHub and every change made in Lovable is committed straight to your repository.
- **Full ownership**: this code is yours. Push to your repository and your changes sync back into Lovable, ready for your next prompt.

## Development

Prefer working locally? You need Node.js and npm — [install with nvm](https://github.com/nvm-sh/nvm#installing-and-updating).

```sh
git clone <this-repository-url>
cd <repository-name>
npm i
npm run dev
```

## Built with

- TanStack Start
- TypeScript
- React
- Tailwind CSS

## Environment variables

Supplier Hub is a frontend-only app that talks to an external ASP.NET Core REST API.

| Variable | Default | Purpose |
| --- | --- | --- |
| `VITE_API_BASE_URL` | `http://localhost:5000` | Base URL of the Supplier Hub API. Endpoints are called as `<base>/api/v1/...`. |
| `VITE_USE_MOCKS` | `true` | When `"true"`, all API calls are served by an in-memory mock (six South African suppliers, simulated latency, pagination, search, type filtering, AI extraction and a 400 validation example). Set to `false` to call the real API. |

Create a `.env.local` to point the app at a running API:

```sh
VITE_API_BASE_URL=https://localhost:5001
VITE_USE_MOCKS=false
```

### Mock validation example

With mocks on, saving a supplier whose email ends in `@example.com`, whose name is `Test`,
or with a service price above 1 000 000 returns an RFC 7807 `ProblemDetails` 400 response,
so the field-level error mapping (`Services[0].Price` -> `services.0.price`) can be tested.
