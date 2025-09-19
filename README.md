# PeP 5.0 — Web Client Examination Platform

A full-stack AI-powered programming examination IDE. The Blazor Server frontend delivers a locked-down, exam-ready coding environment; the Node.js backend generates realistic data-driven Java exams (via OpenAI or curated samples), runs code, accepts submissions, and auto-evaluates results.

- Frontend: .NET 9 Blazor Server + MudBlazor UI
- Backend: Node.js (Express)
- Language under test: Java (JDK required on the host where code runs)
- AI: OpenAI Chat Completions for exam generation and grading (with strict JSON-only contracts)

---

## Features

- Single-click exam generation with realistic CSV datasets (1–2 files; relationships supported)
- Four task prompts per exam with clear, on-screen-only output constraints
- Integrated editor (Ace) with tabbed files, syntax highlighting, and guardrails
- Strict client-side interdictions (copy/paste, selection, context menu, drag/drop, zoom, refresh, devtools shortcuts)
- Fullscreen enforcement with overlay guidance; visual watermark and timing chip
- Console overlay with color-coded output and auto-show on first run
- CSV viewer overlay with virtualization and column metadata
- Run/Reset/Submit flow; submission confirmation and evaluating splash
- Auto-evaluation pipeline: GPT-first grading (JSON-only), heuristic fallback
- Results page with final grade (`10(ten)` format), per-task details, domain/overview, CSV files, code files, and time spent

---

## Repository layout

```
V5Old.sln
AIExamIDE/
  docker-compose.yml                  # Local two-container stack (frontend + backend)
  client/                             # Blazor Server app
    Dockerfile                        # Multi-stage .NET 9 build/publish
    Program.cs                        # Entry & DI (ApiClient, ExamState)
    Components/                       # Razor components (IDE UI)
      Pages/
        Home.razor                    # Main IDE. Single-flight exam generation & overlays
        Results.razor                 # Final evaluation view
      Shared/                         # Solution explorer, editor tabs, task panel, overlays
    Services/
      ApiClient.cs                    # Calls: /exam, /run, /reset, /submit, /evaluate
      ExamState.cs                    # App state: files, tabs, timers, evaluation
    wwwroot/
      js/                             # fullscreen, guard, telemetry, tablock, etc.
      css/                            # generated Tailwind app.css
    package.json                      # Tailwind build scripts
    AIExamIDE.csproj                  # .NET 9 Blazor Server app

  server/                             # Node.js backend
    Dockerfile                        # Node 18 + docker client (optional)
    package.json                      # express, axios, openai
    server.js                         # endpoints and exam/eval logic
    workspace/                        # ephemeral workspace (code + data)
    submissions/                      # saved submissions
```

---

## Prerequisites

- Windows, macOS, or Linux
- .NET SDK 9.0+
- Node.js 18+
- Java JDK 17+ on the backend host (used for `javac/java` when running code)
- OpenAI API key (for AI features) if you want GPT-based generation/evaluation

Optional:
- Docker Desktop (to run via `docker-compose`)

---

## Quick start (local)

Run backend (Node):

```powershell
# from repo root
cd AIExamIDE/server
npm install
$env:OPENAI_API_KEY = "<your-openai-key>"   # optional; fallback sample exams are used if not set
node server.js # or: npx nodemon server.js
```

Run frontend (Blazor Server):

```powershell
# new terminal, from repo root
cd AIExamIDE/client
# build CSS (optional if already generated)
npx tailwindcss -i ./src/input.css -o ./wwwroot/css/app.css
# run the app
dotnet run
```

Open the app:
- Frontend: http://localhost:5000 (if running via Docker) or the URL printed by `dotnet run` (typically http://localhost:5183)
- Backend API: http://localhost:3000

---

## Using Docker (optional)

You can run both services via Docker.

```powershell
# from repo root
cd AIExamIDE
$env:OPENAI_API_KEY = "<your-openai-key>"
docker compose up --build
```

- Frontend: http://localhost:5000
- Backend:  http://localhost:3000

Notes:
- The backend image expects `OPENAI_API_KEY` in the environment; if not set, it will use a rules-based sample exam fallback.
- A named volume `examdata` is created to persist the per-exam workspace (optional).

---

## How it works

1. Generate exam
   - The frontend calls `POST /exam` once on the first interactive render.
   - A single-flight guard in `Home.razor` plus `localStorage` caching ensure only one generation per session and zero duplicate calls on reconnect/refresh.
2. Work the tasks
   - Files are shown in a tree and opened in Ace tabs. CSV files open in a virtualized viewer.
   - The console auto-opens on first run to show output; lines are color-coded by severity.
3. Run code
   - The frontend calls `POST /run` with current files and target main file. The backend compiles and runs Java.
4. Submit
   - You’re prompted to confirm. Files are sent to `POST /submit` and saved under `server/submissions`.
5. Evaluate
   - The frontend calls `POST /evaluate` with files and exam metadata.
   - Backend tries GPT-based grading first (strict JSON), then falls back to structured heuristics.
6. Results
   - You’re redirected to the Results page, which displays the final grade (e.g., `10(ten)`), per-task scoring, and metadata.

---

## Security and exam guardrails

Client-side protections (see `wwwroot/js/guard.js` and `wwwroot/js/fullscreen.js`):
- Block context menu, copy/cut/paste, selection, drag/drop, Ctrl+scroll zoom, and common navigation shortcuts.
- Intercept F12, Ctrl+R, F5, and `Backspace` outside inputs.
- Beforeunload confirmation to reduce accidental exits.
- Fullscreen enforcement with a high z-index overlay and a mask that dims the app when not fullscreen.

Server-side:
- Admin routes protected by a token / session (see `/admin/login` + middleware).
- Basic rate limits for sensitive endpoints.

Important: As with any client-side control, treat these as deterrents, not absolute security.

---

## Configuration

Frontend (`AIExamIDE/client/Program.cs`):
- `ApiClient` is configured with `BaseAddress = http://localhost:3000` by default. Adjust for your deployment.

Backend (`AIExamIDE/server/server.js`):
- `PORT` (default `3000`)
- `OPENAI_API_KEY` (required for GPT generation/evaluation). Use environment variable; do not hardcode keys.
- Workspace paths: `workspace/src` for source files and `workspace/data` for CSVs.

Docker Compose (`AIExamIDE/docker-compose.yml`):
- Frontend exposed at `5000`, backend at `3000`.
- `OPENAI_API_KEY` must be set in the environment or a `.env` file.

---

## Development tips

- If you see the loading splash but no exam, check the backend logs and ensure the frontend can reach `http://localhost:3000`.
- If multiple exams get generated, clear the cache and reload:
  ```js
  localStorage.removeItem('examCacheV1'); location.reload();
  ```
  The frontend has guards to prevent duplicates; verify only one `POST /exam` in the Network tab.
- The build may fail with a file lock if the previous frontend instance is still running. Stop it, then rebuild:
  ```powershell
  # stop running app (Ctrl+C), then
  dotnet build
  dotnet run
  ```

---

## API overview (selected)

- `POST /exam` → generates an exam and returns `{ exam, files, examId }`
- `POST /run`  → compiles/runs Java with provided files
- `POST /reset` → clears workspace and resets files (frontend then re-generates a fresh exam)
- `POST /submit` → stores submission under `server/submissions/<timestamp>`
- `POST /evaluate` → returns JSON-only evaluation with fields:
  - `exam`: `{ domain, csv_files, overview }`
  - `evaluation`: `{ task1..task4: { percentage, status, explanation } }`
  - `final_grade`: integer 2–10
  - `timestamp`: ISO string

---

## Roadmap / ideas

- Replace any hardcoded secrets with env vars everywhere
- Persist generated exams and evaluation results in database storage
- Add unit/integration tests and CI
- Enhance heuristics and add plagiarism detectors
- Add session-aware authentication and proctoring hooks

---

## License

MIT
