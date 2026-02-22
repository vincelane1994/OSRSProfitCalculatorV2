# Scoring & Confidence Rating — How They Work and How to Use Them

## Overview

Every flip candidate shown by the calculator has two key quality signals:

| Signal | Range | What It Tells You |
|--------|-------|-------------------|
| **Flip Score** | 0.0 – 10.0 | Overall quality of the flip opportunity |
| **Confidence Rating** | 0.0 – 1.0 | How trustworthy the underlying price data is |

Understanding both signals — and how they interact — lets you make smarter flipping decisions.

---

## Confidence Rating

### What It Measures

Confidence measures **data reliability**, not profitability. A score of 1.0 means the price recommendation is based on rich, high-volume data from multiple time windows. A low score means the prices were estimated from sparse or limited data.

### Formula

```
windowScore  = MIN(windowsUsed / 3, 1.0) × 0.6
volumeScore  = MIN(volume24Hr / 50,000, 1.0) × 0.4

ConfidenceRating = MIN(windowScore + volumeScore, 1.0)
```

### Input Factors

| Factor | Weight | Description |
|--------|--------|-------------|
| **Windows used** | 60% | Number of price time windows (5m, 1h, 6h, 24h) with valid data |
| **24h Volume** | 40% | Total items traded in the last 24 hours |

**Thresholds for full credit:**
- 3+ time windows → full window credit
- 50,000+ volume → full volume credit

### Confidence Examples

| Situation | windowScore | volumeScore | Confidence |
|-----------|-------------|-------------|------------|
| 4 windows + 200,000 vol | 0.60 | 0.40 | **1.00** — best possible |
| 3 windows + 25,000 vol | 0.60 | 0.20 | **0.80** — solid |
| 2 windows + 50,000 vol | 0.40 | 0.40 | **0.80** — acceptable |
| 1 window + 10,000 vol | 0.20 | 0.08 | **0.28** — poor |
| 0 windows + 0 vol | 0.00 | 0.00 | **0.00** — no data |

### What Low Confidence Means in Practice

When confidence is low:
- The recommended buy/sell prices are extrapolated from fewer data points
- The true market price may be different from what's shown
- Your margin may be smaller (or negative) once you're in the market
- The item may not fill at the expected rate

---

## Flip Score

### What It Measures

Flip Score is a **composite ranking signal** (0.0–10.0) that combines volume, margin, ROI, and GP/hr into a single number — then scales it down by confidence. It is designed to surface the best *overall* opportunities, not just the highest margins.

### Formula

```
rawScore = (volumeScore × 0.30)
         + (marginScore × 0.25)
         + (roiScore × 0.20)
         + (gpHrScore × 0.25)

FlipScore = ROUND(rawScore × confidence × 10.0, 1)
```

### The Four Component Scores (each 0.0–1.0)

#### 1. Volume Score — 30% weight

Measures how liquid the item is. Higher volume means faster fills and less risk of getting stuck.

| 24h Volume | Score |
|------------|-------|
| 1,000 | 0.10 |
| 10,000 | 0.30 |
| 50,000 | 0.60 |
| 200,000+ | 1.00 |

Values between breakpoints are linearly interpolated. For example, 25,000 volume → 0.45.

**Volume is the highest-weighted component** because a flip with no buyers or sellers is worthless regardless of the margin.

#### 2. Margin Score — 25% weight

Measures how much raw GP profit you earn per unit (after the 2% GE tax).

| Margin (GP) | Score |
|-------------|-------|
| 5 | 0.05 |
| 50 | 0.20 |
| 200 | 0.50 |
| 1,000 | 0.80 |
| 5,000+ | 1.00 |

**Margin** = RecommendedSellPrice − RecommendedBuyPrice

#### 3. ROI Score — 20% weight

Measures your return on investment as a percentage. This matters because high margins on expensive items can represent poor capital efficiency.

| ROI % | Score |
|-------|-------|
| 0.5% | 0.10 |
| 2.0% | 0.30 |
| 5.0% | 0.60 |
| 15%+ | 1.00 |

**ROI** = (ProfitPerUnit / BuyPrice) × 100

#### 4. GP/hr Score — 25% weight

Measures estimated profit per hour, accounting for fill rate and buy limits. This is the most practical metric for active flippers.

```
gpHrScore = MIN(GpPerHour / 1,000,000, 1.0)
```

| GP/hr | Score |
|-------|-------|
| 500,000 | 0.50 |
| 1,000,000+ | 1.00 |

#### How GP/hr Is Calculated

```
HourlyVolume       = MAX(volume24Hr / 24, 1)
BuyFillHours       = MIN(BuyLimit / HourlyVolume, 4)
SellFillHours      = MIN(Quantity / HourlyVolume, 4)
EstimatedFillHours = BuyFillHours + SellFillHours

Tax                = MIN(ProfitPerUnit × 0.02, 5,000,000)
TotalProfit        = (ProfitPerUnit − Tax) × Quantity

GpPerHour          = TotalProfit / EstimatedFillHours
```

Defaults: MaxInvestment = 10M GP, BuyLimitCycle = 4 hours.

### How Confidence Multiplies the Score

Confidence acts as a **penalty multiplier**. A flip with a raw score of 0.9 but confidence of 0.5 becomes:

```
FlipScore = 0.9 × 0.5 × 10 = 4.5
```

The same flip with full confidence (1.0) would score 9.0. This means:
- **High-confidence items are naturally ranked higher**, even if their raw metrics are similar
- A suspiciously high Flip Score on a low-volume item is mathematically unlikely

---

## Pre-Filtering (Before Scoring)

Candidates that fail any of these checks are excluded before scoring:

| Filter | Default | Purpose |
|--------|---------|---------|
| Min 24h Volume | 10,000 | Exclude illiquid items |
| Min Buy Limit | 100 | Exclude heavily restricted items |
| Min Margin | 10 GP | Exclude negligible opportunities |
| Max Investment | 10M GP | Capital constraint |
| Manipulation detection | 50% deviation OR 10x volume imbalance | Exclude price-manipulated items |

### Manipulation Detection

Two checks are run:

1. **Price deviation:** If the 5-minute price deviates more than 50% from the 24-hour average, the item is flagged as suspicious.
2. **Volume imbalance:** If the ratio of buy-to-sell volume (or vice versa) exceeds 10:1, the item is flagged.

Flagged items are excluded from results entirely.

---

## How Results Are Ranked

Items are **sorted by GP/hr** (highest first), not by Flip Score. Flip Score is a quality signal you can use to evaluate and compare candidates — it does not directly control the ordering.

This means:
- The top result is always the highest estimated GP/hr
- Two items with similar GP/hr may have very different Flip Scores — use Flip Score to pick the more reliable one

---

## How to Use These Signals When Flipping

### Reading the Signals Together

| Flip Score | Confidence | Interpretation |
|-----------|------------|----------------|
| 8.0–10.0 | 0.85–1.00 | Strong, reliable opportunity — act on it |
| 6.0–8.0 | 0.70–0.85 | Good opportunity, do a quick price check in-game first |
| 6.0–8.0 | 0.40–0.70 | Potentially good, but verify prices manually before committing capital |
| < 6.0 | Any | Use only if GP/hr is compelling and you understand the risk |
| Any | < 0.40 | Treat as speculative — the price data is thin |

### Practical Flipping Guidelines

#### For High-Capital Flippers (5M+ per flip)
- **Prioritize Confidence ≥ 0.80** before acting on a recommendation
- Use Flip Score as a secondary filter: prefer 7.0+ when confidence is borderline
- The GP/hr estimate becomes more accurate at high volume — check that volume24Hr justifies your position size

#### For Low-Capital / New Flippers
- **Stick to Flip Score 7.0+ and Confidence 0.85+** to reduce uncertainty
- Items with high volume (50,000+) fill faster and are more forgiving if prices shift
- Smaller margins (50–500 GP) with high confidence are safer than large margins with low confidence

#### For Active (Frequent) Flippers
- Focus on **GP/hr** as your primary metric — this already accounts for fill rate
- Use Flip Score to break ties between similar GP/hr candidates
- Watch for items where Score is high but Volume Score is low — they may fill slowly

#### For Passive (Set-and-Forget) Flippers
- **Volume Score** matters most — you need items that fill while you're away
- Prefer items with 24h volume ≥ 50,000 and buy limits ≥ 1,000
- Confidence should be ≥ 0.80 to trust that prices hold over a longer window

### Red Flags to Watch For

| Signal | Warning |
|--------|---------|
| Confidence < 0.40 | Prices are estimated from very little data — verify in-game before committing |
| High ROI (>15%) + Low Volume | Could be a manipulation target or an illiquid niche item — tread carefully |
| High Margin + Low Confidence | Margin may be stale or based on anomalous data |
| GP/hr >> historical norms for the item | Fill time estimate may be optimistic |

### Margin of Error Awareness

Because prices are weighted averages across time windows, the **recommended prices are starting points**, not guarantees:

- Place your **buy offer 1–5 GP above** the recommended buy price to undercut other buyers
- Place your **sell offer 1–5 GP below** the recommended sell price to undercut other sellers
- The actual margin you realize may be 5–15% narrower than the displayed margin, especially at low confidence

---

## Summary Reference

```
Flip Score (0–10) = rawScore × confidence × 10

rawScore = volumeScore(0.30) + marginScore(0.25) + roiScore(0.20) + gpHrScore(0.25)

confidence = windowComponent(0.60) + volumeComponent(0.40)
           = MIN(windowsUsed/3, 1.0)×0.6 + MIN(vol24h/50000, 1.0)×0.4

Results ordered by: GP/hr (descending)
```

**Rule of thumb:** Trust items with Flip Score ≥ 7.0 and Confidence ≥ 0.80. Verify prices manually when either signal is below those thresholds.
