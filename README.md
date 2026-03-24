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
| **Free Spin Bonus Round** | Scatter-triggered bonus simulates N free spins at a configurable multiplier |
| **Base/Bonus RTP Split** | Analytical and simulated RTP broken out by base game vs. bonus contribution |
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
[14:09:23 INF] Loaded game: default-game-v2 | Reels: 5 | Paylines: 20 | Bonus: 13 free spins @ 2x
[14:10:23 INF] Theoretical RTP: 96.3502 %  (Base: 74.6435 % + Bonus: 21.7067 %)
[14:10:23 INF] Starting Monte Carlo simulation: 1,000,000 spins

────────────────────────────────────────────────────────────
  Game               : default-game-v2
  Total Spins        : 1,000,000
  Total Wagered      : 20,000,000.00
  Total Paid         : 19,231,153.00
────────────────────────────────────────────────────────────
  Theoretical RTP    : 96.350%
    ├─ Base Game     : 74.644%
    └─ Bonus Round   : 21.707%
  Simulated RTP      : 96.156%
    ├─ Base Game     : 74.489%
    └─ Bonus Round   : 21.667%
  RTP Delta          : 0.1940%   ✓ PASS
────────────────────────────────────────────────────────────
  Hit Frequency      : 79.10%
  Average Win        : 24.311x
  Max Win Observed   : 1,785.0x
  Volatility Index   : 2.53
  Bonus Trigger Freq : 1 in 89
  Bonus Config       : 13 free spins × 2x  (max 5,000x cap)
  Duration           : 2,175ms
────────────────────────────────────────────────────────────
  Win Distribution:
    0x                 208,960
    0.01x-0.99x        554,601
    1x-4.99x           206,804
    5x-19.99x           24,704
    20x-99.99x           4,931
    100x-499.99x             0
    500x+                    0
────────────────────────────────────────────────────────────
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
      "A": { "3": 8, "4": 40, "5": 150 },
      "WILD": { "3": 15, "4": 75, "5": 500 }
    }
  },
  "bonusRoundConfig": {
    "freeSpinCount": 13,
    "winMultiplier": 2.0,
    "maxWinMultiplier": 5000.0
  }
}
```

Place config files in `src/SlotMathEngine.Runner/configs/` and reference with `--config`.

Included configs:
- `default-game.json` — 5 reels, 3 rows, 20 paylines, **96.35% RTP** with free spin bonus (13 spins @ 2×)
- `high-volatility.json` — same structure, top-heavy paytable with rare high payouts

---

## Architecture

```
SimulationRunner (CLI / API)
    └── MonteCarloSimulator          orchestrates N spins + bonus round simulation
            ├── ReelEngine           weighted stop draw (thread-local RNG)
            ├── PaylineEvaluator     left-to-right win eval + Wild substitution
            ├── SimulateFreeSpins    bonus: N free spins × multiplier, max-win cap
            └── StatisticsAggregator base vs. bonus RTP, win distribution, volatility

RtpCalculator                        analytical — independent of simulation (memoised)
    └── Enumerates all Π(N_r) stop combos, sums P(combo) × Payout(combo)
    └── Computes P(trigger) × FreeSpins × BaseRtp × Multiplier for bonus RTP

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
