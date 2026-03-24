# slot-math-engine

A Monte Carlo simulation engine for slot game mathematics. Computes theoretical return-to-player (RTP) analytically, simulates millions of spins, and validates convergence between analytical and simulated results.

Built to production standards: configurable paytables, structured logging, statistical validation suite, REST API, and CI pipeline.

![CI](https://github.com/nvmangell/slot-math-engine/actions/workflows/ci.yml/badge.svg)

---

## What It Does

| Capability | Details |
|---|---|
| **Theoretical RTP** | Analytically enumerates all reel combinations, sums probability-weighted payouts |
| **Monte Carlo Simulation** | Runs up to 50M spins via REST API or CLI |
| **Convergence Validation** | Asserts `\|simulated − theoretical RTP\| < ε` at configurable tolerance |
| **Statistics** | Hit frequency, average win, max win, volatility index, bonus trigger frequency |
| **Win Distribution** | Bucketed breakdown of win multipliers across all spins |
| **Wild Substitution** | Left-to-right payline eval with Wild symbol support |
| **Scatter Bonus** | Scatter count triggers bonus flag independently of paylines |
| **REST API** | ASP.NET Core — trigger simulations and query paytables over HTTP |
| **CSV Output** | Spin-level records for downstream analysis |

---

## Quick Start

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/nvmangell/slot-math-engine
cd slot-math-engine
dotnet build

# Run CLI simulation (100K spins, default game)
cd src/SlotMathEngine.Runner
dotnet run -- --config configs/default-game.json --spins 100000

# Run API (Swagger UI at https://localhost:5001/swagger)
cd src/SlotMathEngine.Api
dotnet run
```

---

## Sample Output

```
[12:07:07 INF] Loading game config from configs/default-game.json
[12:07:07 INF] Loaded game: default-game-v1 | Reels: 5 | Paylines: 20
[12:07:08 INF] Calculating theoretical RTP...
[12:07:08 INF] Theoretical RTP: 42.2272%
[12:07:08 INF] Starting Monte Carlo simulation: 1,000,000 spins

────────────────────────────────────────────────────
  Game               : default-game-v1
  Total Spins        : 1,000,000
  Total Wagered      : 20,000,000.00
  Total Paid         : 8,443,210.00
────────────────────────────────────────────────────
  Theoretical RTP    : 42.227%
  Simulated RTP      : 42.216%
  RTP Delta          : 0.0110%   ✓ PASS (threshold: 0.10%)
────────────────────────────────────────────────────
  Hit Frequency      : 39.48%
  Average Win        : 21.41x
  Max Win Observed   : 4960.0x
  Volatility Index   : 1.27
  Bonus Trigger Freq : 1 in 71
  Duration           : 1,843ms
────────────────────────────────────────────────────
  Win Distribution:
    0x                 605,241
    0.01x-0.99x        280,893
    1x-4.99x            97,742
    5x-19.99x           15,301
    20x-99.99x             811
    100x-499.99x            12
    500x+                    0
────────────────────────────────────────────────────
```

See [`sample-outputs/`](sample-outputs/) for real output files.

---

## CLI Usage

```bash
dotnet run -- [options]

Options:
  --config <path>     Path to game config JSON   (default: configs/default-game.json)
  --spins  <count>    Number of spins to simulate (default: 1,000,000)
  --output <dir>      Output directory            (default: sample-outputs)
```

**Examples:**

```bash
# 1M spins, default game
dotnet run -- --spins 1000000

# High-volatility config, 5M spins
dotnet run -- --config configs/high-volatility.json --spins 5000000
```

---

## REST API

Start the API:
```bash
cd src/SlotMathEngine.Api
dotnet run
```

Swagger UI: `https://localhost:5001/swagger`

### `POST /api/simulation/run`

Trigger a simulation and return a full report.

**Request:**
```json
{
  "gameId": "default-game-v1",
  "spinCount": 1000000,
  "convergenceTolerance": 0.001
}
```

**Response:**
```json
{
  "report": {
    "gameId": "default-game-v1",
    "totalSpins": 1000000,
    "simulatedRtp": 0.42216,
    "theoreticalRtp": 0.42227,
    "rtpDelta": 0.00011,
    "hitFrequency": 0.3948,
    "averageWin": 21.41,
    "maxWin": 4960.0,
    "volatilityIndex": 1.27,
    "convergenceStatus": "PASS"
  },
  "theoreticalRtp": 0.42227
}
```

### `GET /api/analytics/paytable/{gameId}`

Returns the paytable and per-reel symbol probability distribution.

### `GET /health`

Returns API health status.

---

## Game Configuration

Games are defined as JSON and loaded at runtime — no recompilation needed to test new paytables.

```json
{
  "gameId": "my-game-v1",
  "reels": 5,
  "rows": 3,
  "betPerLine": 1.0,
  "reelStrips": [
    { "reelIndex": 0, "symbols": ["A","A","B","B","B","C","C","WILD","SCATTER"] }
  ],
  "paylines": [
    { "id": 1, "rowPositions": [1, 1, 1, 1, 1] }
  ],
  "paytable": {
    "payouts": {
      "A": { "3": 5, "4": 25, "5": 100 },
      "WILD": { "3": 10, "4": 50, "5": 500 }
    }
  }
}
```

Place config files in `src/SlotMathEngine.Runner/configs/` and reference with `--config`.

Included configs:
- `default-game.json` — 5 reels, 3 rows, 20 paylines, mixed volatility
- `high-volatility.json` — same structure, top-heavy paytable with rare high payouts

---

## Architecture

```
SimulationRunner (CLI / API)
    └── MonteCarloSimulator          orchestrates N spins
            ├── ReelEngine           weighted stop draw (thread-local RNG)
            ├── PaylineEvaluator     left-to-right win eval + Wild substitution
            └── StatisticsAggregator running totals, win distribution, volatility

RtpCalculator                        analytical — independent of simulation
    └── Enumerates all Π(N_r) stop combos
    └── Sums P(combo) × Payout(combo) across all paylines + scatters

CsvReportWriter                      spin-level CSV + summary JSON output
GameConfigLoader                     validates and deserializes game JSON configs
```

See [`docs/math-spec.md`](docs/math-spec.md) for full mathematical documentation.

---

## Testing

```bash
# Unit tests (fast)
dotnet test --filter "Category!=Statistical"

# All tests including convergence (slower — runs 1M+ spins)
dotnet test
```

**Test coverage:**
- `ReelEngineTests` — dimensions, symbol validity, frequency distribution
- `PaylineEvaluatorTests` — exact wins, no-match, Wild substitution, scatter count
- `RtpCalculatorTests` — hand-verified known-answer paytable, reproducibility
- `RtpConvergenceTests` — simulated RTP converges to theoretical at 1M spins

---

## Tech Stack

| Layer | Technology |
|---|---|
| Core engine | C# / .NET 9 |
| API | ASP.NET Core 9 (Controllers) |
| Logging | Serilog (console + file sinks) |
| API docs | Swashbuckle / Swagger UI |
| CSV output | CsvHelper |
| Tests | xUnit |
| CI | GitHub Actions |

---

## Project Structure

```
slot-math-engine/
├── src/
│   ├── SlotMathEngine.Core/        Math engine, models, simulation logic
│   ├── SlotMathEngine.Api/         ASP.NET Core REST API
│   └── SlotMathEngine.Runner/      CLI runner + game configs
├── tests/
│   ├── SlotMathEngine.UnitTests/   Fast unit tests
│   └── SlotMathEngine.StatisticalTests/  Convergence tests
├── sample-outputs/                 Real output from simulation runs
├── docs/
│   └── math-spec.md               Full mathematical specification
└── .github/workflows/ci.yml       GitHub Actions CI pipeline
```
