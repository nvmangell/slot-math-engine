# Math Specification — SlotMathEngine

## Overview

This document describes the mathematical model used to calculate and validate slot game return-to-player (RTP).

---

## Reel Strip Probability

Each reel is defined by a strip of symbols. A random stop position is drawn uniformly from the strip, and the visible window shows the symbols at positions `[stop, stop+1, ..., stop+rows-1]` (wrapping around).

For a symbol `s` on reel `r` with strip length `N_r`:

```
P(s on reel r) = count(s in strip r) / N_r
```

Symbol stops are independent across reels.

---

## Payline Evaluation

A payline is a fixed path through the reels (one position per reel). Evaluation proceeds left-to-right:

1. Identify the **anchor symbol**: the first non-Wild, non-Scatter symbol from the left.
2. Count consecutive matching symbols from the left (Wilds count as any symbol).
3. Look up the payout in the paytable for `(anchorSymbol, matchCount)`.
4. Minimum match count for a win: **3**.

Wild symbols substitute for any paying symbol but not for Scatter.

---

## Theoretical RTP Calculation

The theoretical RTP is computed analytically by enumerating all possible reel stop combinations:

```
RTP = E[payout per spin] / E[wager per spin]

E[payout per spin] = Σ_{all combos} P(combo) × Payout(combo)

P(combo) = Π_{r=1}^{reels} (1 / strip_length_r)    [independent reels]

Payout(combo) = Σ_{paylines} win(payline, combo)
              + scatter_payout(scatter_count(combo))
```

Total combinations = `Π N_r` (product of all reel strip lengths).

For 5 reels with 32 stops each: 32^5 = 33,554,432 combinations enumerated.

---

## Bonus Round RTP Contribution

When a `BonusRoundConfig` is present, the theoretical RTP includes an analytical bonus contribution:

```
Bonus Contribution = P(trigger) × FreeSpinCount × BaseGameRtp × WinMultiplier

Total RTP = BaseGameRtp + BonusContribution
```

Where:
- `P(trigger)` = fraction of all reel combos where scatter_count ≥ BonusTriggerScatterCount
- `FreeSpinCount` = number of free spins awarded per trigger
- `WinMultiplier` = multiplier applied to all payouts during free spins
- Re-triggers during free spins are not modelled (conservative estimate)

**Example (default-game-v2):**
```
P(trigger)        ≈ 0.01118   (1 in ~89 spins)
FreeSpinCount     = 13
WinMultiplier     = 2.0×
BaseGameRtp       ≈ 74.64%

BonusContribution = 0.01118 × 13 × 0.7464 × 2.0 = 21.71%
Total RTP         = 74.64% + 21.71% = 96.35%
```

---

## Monte Carlo Validation

The simulator draws `N` random spins and computes:

```
Simulated RTP     = (BasePaid + BonusPaid) / TotalWagered
BaseGameRtp       = BasePaid / TotalWagered
BonusRtp          = BonusPaid / TotalWagered
```

**Convergence criterion:** `|Simulated RTP − Theoretical RTP| < ε`

For base-only games: `ε = 0.001` (0.1%) at N = 1,000,000 spins is tight.

For bonus games: `ε = 0.005` (0.5%) is appropriate, because bonus rounds introduce high variance — each trigger can pay 10–100× the average base-game win, so far more spins are needed to smooth the distribution. At 1M spins with a 1-in-89 trigger rate there are ~11,200 bonus samples, producing standard error of approximately ±0.2% on the bonus component alone.

---

## Volatility Index

The volatility index measures the spread of win outcomes relative to the average bet:

```
VI = σ(win per spin) / E[bet per spin]

where σ² = E[win²] − E[win]²
```

Typical ranges:
- **Low volatility**: VI < 5 (frequent small wins)
- **Medium volatility**: VI 5–15
- **High volatility**: VI > 15 (rare large wins)

---

## Hit Frequency

```
Hit Frequency = winning spins / total spins
```

A "winning spin" is any spin where `TotalWin > 0`.

---

## Scatter Evaluation

Scatter symbols are not payline-dependent. They are counted anywhere in the visible window across all reels. The scatter payout is:

```
scatter_payout = Paytable[SCATTER][count] × BetPerLine
```

Scatters with `count >= BonusTriggerScatterCount` also flag `IsBonusTrigger = true`.

---

## Default Game Configuration (default-game-v2)

| Parameter | Value |
|---|---|
| Reels × Rows | 5 × 3 |
| Paylines | 20 |
| Strip length | 32 stops per reel |
| Total combinations | 32^5 = 33,554,432 |
| **Theoretical Base RTP** | **74.64%** |
| **Bonus Contribution** | **21.71%** |
| **Total Theoretical RTP** | **96.35%** |
| Hit Frequency | ~79% |
| Volatility Index | ~2.5 |
| Bonus Trigger | 1 in ~89 spins |
| Free Spins | 13 spins @ 2× multiplier |
| Max Win Cap | 5,000× bet |

Symbol frequency per reel (32 stops): E ≈ 8–9, D ≈ 7–8, C ≈ 5–6, B = 4, A = 3, Wild = 2, Scatter = 1–2.
