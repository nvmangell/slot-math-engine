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

For 5 reels with 25 stops each: 25^5 = 9,765,625 combinations enumerated.

---

## Monte Carlo Validation

The simulator draws `N` random spins and computes:

```
Simulated RTP = TotalPaid / TotalWagered
```

**Convergence criterion:** `|Simulated RTP − Theoretical RTP| < ε`

Where `ε = 0.001` (0.1%) at N = 1,000,000 spins.

By the Central Limit Theorem, the standard error of the simulated RTP decreases as `1/√N`. At 1M spins with typical slot variance, the 95% confidence interval is approximately ±0.05% around the true RTP, making convergence within 0.1% highly reliable.

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

## Demo Paytable Note

The included `default-game.json` and `high-volatility.json` configs are demonstration paytables. Their theoretical RTPs (~42% and ~35% respectively) are intentionally simplified for fast analytical enumeration with small reel strips. Production slot games target 92–96% RTP with much larger reel strips.

To model a production-spec game, increase reel strip lengths and adjust paytable multipliers accordingly.
