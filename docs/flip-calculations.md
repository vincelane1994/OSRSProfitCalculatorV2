# Flip Calculations — How It Works

## Data Source

All price and volume data comes from the [OSRS Wiki Real-Time Prices API](https://oldschool.runescape.wiki/w/RuneScape:Real-time_Prices) at `prices.runescape.wiki`. This API is powered by crowdsourced trade data from RuneLite client users.

### Endpoints Used

| Endpoint | What it provides |
|---|---|
| `/mapping` | Item metadata: name, ID, members flag, GE buy limit, high alch value |
| `/latest` | Most recent instant-buy and instant-sell prices with timestamps |
| `/5m` | 5-minute averaged prices and volumes |
| `/1h` | 1-hour averaged prices and volumes |
| `/6h` | 6-hour averaged prices and volumes |
| `/24h` | 24-hour averaged prices and volumes |

### API Terminology

The API uses counterintuitive naming:
- **"high" price** = instant-buy price (a buyer overpaid to fill immediately)
- **"low" price** = instant-sell price (a seller undercut to fill immediately)

For a patient flip, we do the opposite — buy low and sell high:
- **Recommended buy price** is derived from instant-sell ("low") data
- **Recommended sell price** is derived from instant-buy ("high") data

---

## How Prices Are Determined

### Recommended Buy & Sell Prices

Prices are calculated as a **weighted average** across all four time windows:

| Window | Weight |
|---|---|
| 5 minute | 10% |
| 1 hour | 35% |
| 6 hour | 35% |
| 24 hour | 20% |

If a window has no data (null or zero price), its weight is **redistributed proportionally** across the remaining windows. For example, if the 5-minute window is missing, the other three windows share that extra 10% in proportion to their original weights.

### Margin

```
Margin = Recommended Sell Price - Recommended Buy Price
```

This is the gross margin before tax.

### Tax

The GE charges a 2% tax on the sell price, capped at 5,000,000 GP:

```
Tax = floor(Sell Price * 0.02), max 5,000,000
```

### Profit Per Unit

```
Profit Per Unit = Margin - Tax
```

### Buy Limit

Sourced directly from the `/mapping` endpoint. This is the maximum number of an item you can buy from the GE every 4 hours.

### Volumes

Each time window endpoint returns:
- **Buy volume** (`highPriceVolume`) — number of instant-buy transactions
- **Sell volume** (`lowPriceVolume`) — number of instant-sell transactions

**Known issue:** The API sometimes reports lower volume in longer windows (e.g., 24h < 6h). This is a data quality quirk of how the API aggregates data over longer periods — it does not mean fewer trades actually occurred.

---

## How Confidence Is Determined

Confidence rates data quality from 0.0 to 1.0. It answers: "How much should we trust this recommendation?"

### Formula

```
Confidence = Volume Score (40%) + Stability Score (35%) + Window Score (25%)
```

### Components

**Volume Score (40% weight)**
```
Volume Score = min(Volume24Hr / 50,000, 1.0)
```
Items trading 50,000+ units/day get a full volume score. Low-volume items are riskier because prices can move against you before your offer fills.

**Price Stability Score (35% weight)**

Compares the 5-minute average price to the 6-hour average price. The larger deviation (buy or sell side) determines the stability tier:

| Deviation | Score |
|---|---|
| 0–5% | 1.0 |
| 5–15% | 0.7 |
| 15–30% | 0.4 |
| > 30% | 0.1 |

Volatile items get low stability scores because the recommended prices may be stale by the time your offer fills.

**Window Coverage Score (25% weight)**
```
Window Score = min(Windows Used / 3, 1.0)
```
Uses the minimum of buy and sell windows available. Items with data in 3+ windows get a full score. Items with only 1 window of data are unreliable.

---

## How Score Is Determined

Score ranks flip candidates from 0.0 to 10.0. It answers: "How good is this flip opportunity?"

### Formula

```
Score = Raw Score * Confidence * 10
```

### Raw Score Components

```
Raw Score = Volume (20%) + Profit/Cycle (35%) + ROI (20%) + GP/Hour (25%)
```

**Volume Sub-Score (20% weight)**

Interpolated from breakpoints:

| 24h Volume | Score |
|---|---|
| 1,000 | 0.1 |
| 10,000 | 0.3 |
| 50,000 | 0.6 |
| 200,000 | 1.0 |

Values between breakpoints are linearly interpolated.

**Profit Per Cycle Sub-Score (35% weight)**

```
Profit Per Cycle = Profit Per Unit * Buy Limit
```

This is the maximum GP you can earn per 4-hour GE cycle. Interpolated from breakpoints:

| Profit/Cycle | Score |
|---|---|
| 50,000 GP | 0.1 |
| 250,000 GP | 0.3 |
| 1,000,000 GP | 0.6 |
| 3,000,000 GP | 0.8 |
| 8,000,000 GP | 1.0 |

**ROI Sub-Score (20% weight)**

```
ROI% = (Profit Per Unit / Buy Price) * 100
```

The breakpoints account for the 2% GE tax — items near the tax break-even point score very low:

| ROI% | Score |
|---|---|
| 0.0% | 0.00 |
| 1.0% | 0.05 |
| 3.0% | 0.30 |
| 5.0% | 0.60 |
| 10.0% | 0.85 |
| 20.0% | 1.00 |

**GP/Hour Sub-Score (25% weight)**

```
GP/Hour Score = min(GP Per Hour / 1,000,000, 1.0)
```

Items earning 1M+ GP/hr get a full score.

GP/Hour is calculated as:
```
GP/Hour = Total Profit / Estimated Fill Hours
```

Fill hours are floored at the 4-hour buy limit cycle — even if an item fills in minutes, you can't restart buying until the cycle resets.

**Estimated Fill Time**

```
Hourly Volume = max(Volume24Hr / 24, 1)
Buy Hours = Buy Limit / Hourly Volume
Sell Hours = Quantity / Hourly Volume
Raw Fill Hours = Buy Hours + Sell Hours
Estimated Fill Hours = max(Raw Fill Hours, 4.0)
```

The floor of 4 hours reflects the GE buy limit cycle — even if your offers fill in minutes, you cannot restart buying the same item until the 4-hour cycle resets. This prevents GP/Hour from being artificially inflated on fast-filling items.

Example: An item with a 1,000 buy limit trading 200,000/day (~8,333/hr) would calculate `Buy Hours = 1000/8333 = 0.12h`, `Sell Hours = 0.12h`, `Raw Fill = 0.24h`. Without the floor, GP/Hour would be massively overstated. With the floor, `Estimated Fill Hours = 4.0h`, giving an accurate earnings rate per cycle.

### Confidence as Multiplier

The final score is multiplied by confidence. A perfect raw score with 0.5 confidence caps at 5.0/10. This ensures low-data items never rank above well-supported ones.
