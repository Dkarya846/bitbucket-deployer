<div align="center">

# Bitbucket Deployer

**Trigger Bitbucket Pipelines across multiple repositories from one dashboard — instead of opening each repository by hand.**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-22-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![Angular Material](https://img.shields.io/badge/UI-Angular%20Material-757575?logo=angular&logoColor=white)](https://material.angular.dev/)
[![SQLite](https://img.shields.io/badge/Storage-SQLite-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Bitbucket Cloud](https://img.shields.io/badge/Powered%20by-Bitbucket%20Cloud-0052CC?logo=bitbucket&logoColor=white)](https://bitbucket.org/)

</div>

---

Bitbucket Cloud remains the source of truth for repositories, branches, pipeline definitions, and pipeline execution — this app does not implement its own CI/CD. It only discovers what's available via Bitbucket's REST API, lets a user pick an environment/branch/pipeline per repository, triggers the pipelines, and tracks their status.

## Contents

- [How it works](#how-it-works)
- [Project layout](#project-layout)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
- [Using the app](#using-the-app)
- [Configuration reference](#configuration-reference)
- [Running tests](#running-tests)
- [Known limitations](#known-limitations)

---

## How it works

```
Angular SPA  →  ASP.NET Core API  →  Bitbucket Cloud REST API
                       ↓
                  SQLite (local)
           (deployment history + OAuth sessions)
```

- **Frontend**: Angular 22, standalone components, Angular Material, signals-based state.
- **Backend**: ASP.NET Core (.NET 10) Web API.
- **Auth**: Per-user login via Bitbucket OAuth 2.0 — there's no shared API token. Each person logs in with their own Bitbucket account; their session's access token is used for their Bitbucket calls, and deployments are attributed to them.
- **Storage**: SQLite, used only for deployment history and login sessions. No separate database server. Bitbucket itself is never mirrored or cached beyond a single request's lifetime.

---

## Project layout

```
backend/
  src/BitbucketDeployer.Api/
    Deployment/   Repository/branch/pipeline discovery, validation, triggering, status, history
    Auth/         Bitbucket OAuth login flow, session storage
    ErrorHandling/
  tests/BitbucketDeployer.Api.Tests/   (mirrors the src/ tree)

frontend/
  bitbucket-deployer-ui/
    src/app/
      core/        HTTP interceptors, auth service/guard
      auth/login/  Login page
      deployment/  Dashboard, deployment details/history, all deployment-related components
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) and npm
- A Bitbucket Cloud workspace you have at least member access to
- Ability to create a Bitbucket OAuth consumer (see below) — either workspace admin rights, or your own personal Bitbucket workspace

---

## Setup

### 1. Create a Bitbucket OAuth consumer

The app needs this to let users log in with their Bitbucket account.

1. In Bitbucket, go to a workspace you administer → **Settings** (cog) → **Workspace settings** → **Apps and features** → **OAuth consumers**. (No admin rights on any team workspace? Your own personal workspace — the one named after your username — works too; you're the owner of that one by default.)
2. Click **Add consumer**.
3. **Callback URL**: `http://localhost:5149/api/auth/callback` for local development.
4. **Permissions**: Repositories → Read, Pipelines → Read and Write.
5. Save, then expand the consumer to copy its **Key** (Client ID) and **Secret**.

### 2. Configure the backend

Secrets go in [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), never in `appsettings.json`:

```bash
cd backend/src/BitbucketDeployer.Api
dotnet user-secrets set "Bitbucket:Workspace" "<your-workspace-slug>"
dotnet user-secrets set "Bitbucket:OAuth:ClientId" "<key-from-above>"
dotnet user-secrets set "Bitbucket:OAuth:ClientSecret" "<secret-from-above>"
```

The workspace slug is the segment right after `bitbucket.org/` in your repository URLs (e.g. `bitbucket.org/your-workspace/some-repo` → `your-workspace`).

Run the backend:

```bash
dotnet run
```

It listens on `http://localhost:5149`. On first run it creates `App_Data/deployments.db` (SQLite) automatically — no migration step needed.

### 3. Run the frontend

```bash
cd frontend/bitbucket-deployer-ui
npm install
npm start
```

Opens at `http://localhost:4400`. It's pre-configured (`src/environments/`) to call the backend at `http://localhost:5149/api`.

### 4. Log in

Visit `http://localhost:4400`, click **Login with Bitbucket**, and authorize the consumer you created. You're redirected back and land on the deployment dashboard.

---

## Using the app

1. **Dashboard** (`/deploy`): pick an environment, search and select one or more repositories, then choose a branch and pipeline for each. If a pipeline declares variables in its `bitbucket-pipelines.yml`, matching input fields appear automatically.
2. **Validate**: checks that each repository/branch/pipeline combination is real and triggerable, and that you're allowed to deploy to the selected environment, without triggering anything.
3. **Deploy**: triggers the selected pipeline for every selected repository (in parallel) and takes you to a details page that polls Bitbucket for live status until everything reaches a final state.
4. **History** (`/deployments`): past deployments triggered through this app. Limited to what's in the local SQLite database — it does not query Bitbucket's own pipeline history.

---

## Configuration reference

All of these live in `backend/src/BitbucketDeployer.Api/appsettings.json` (non-secret defaults) or user-secrets (anything sensitive):

| Key | Purpose |
|---|---|
| `Bitbucket:Workspace` | The Bitbucket workspace this app manages |
| `Bitbucket:OAuth:ClientId` / `ClientSecret` | The OAuth consumer created above |
| `ConnectionStrings:Deployments` | SQLite connection string for deployment history + sessions |
| `Frontend:BaseUrl` | Where the backend redirects back to after login |
| `Cors:AllowedOrigins` | Must include the frontend's origin for the session cookie to work |
| `Authorization:ProductionApprovedUsers` | Usernames allowed to deploy to the `production` environment |
| `Environments` | The list of deployment targets offered in the UI (id/name/whether it needs extra confirmation) |

---

## Running tests

```bash
cd backend && dotnet test
cd frontend/bitbucket-deployer-ui && npm test
```

---

## Known limitations

- SQLite is a single local file — fine for one backend instance, not for horizontal scaling without moving to a shared database.
- No production hosting is set up yet. Deploying this app itself (not to be confused with the deployments it triggers) requires a host that can run a persistent ASP.NET Core process with a stable public HTTPS URL — see the OAuth callback URL above, which would need to change from `localhost` to that public URL.
- Deployment history only covers what was triggered through this app; it isn't a general Bitbucket Pipelines history viewer.

---

<div align="center">

Built for internal use — deployments still run on Bitbucket Pipelines, this just makes triggering them less tedious.

</div>
