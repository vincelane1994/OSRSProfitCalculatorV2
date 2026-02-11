# OSRSProfitCalculatorV2

A Python-based profit calculator for Old School RuneScape (OSRS) that helps players calculate potential profits from various in-game activities.

## Features

- **Price Fetching**: Fetches real-time item prices from the official OSRS Grand Exchange API
- **Flip Profit Calculator**: Calculate potential profits from flipping items (buying low, selling high)
- **Craft Profit Calculator**: Calculate profits from crafting activities
- **ROI Analysis**: Calculate return on investment for flipping activities

## Installation

1. Clone the repository:
```bash
git clone https://github.com/vincelane1994/OSRSProfitCalculatorV2.git
cd OSRSProfitCalculatorV2
```

2. Install dependencies:
```bash
pip install -r requirements.txt
```

## Usage

Run the example calculator:
```bash
python main.py
```

### Using the ProfitCalculator Class

```python
from src.calculator import ProfitCalculator

calculator = ProfitCalculator()

# Calculate flip profit for an item
flip_result = calculator.calculate_flip_profit(item_id=561, quantity=1000)
print(f"Total Profit: {flip_result['total_profit']} gp")

# Calculate crafting profit
craft_result = calculator.calculate_craft_profit(
    output_item_id=1234,  # Crafted item ID
    input_items={
        123: 1,  # Input item ID: quantity
        456: 2   # Another input item: quantity
    },
    quantity=10
)
print(f"Profit per item: {craft_result['profit_per_item']} gp")
```

## Project Structure

```
OSRSProfitCalculatorV2/
├── src/
│   ├── __init__.py          # Package initialization
│   ├── price_fetcher.py     # OSRS API price fetcher
│   └── calculator.py        # Profit calculation logic
├── main.py                  # Example CLI application
├── requirements.txt         # Python dependencies
└── README.md               # This file
```

## API Reference

### ItemPriceFetcher

Fetches item prices from the OSRS Grand Exchange API.

- `get_latest_price(item_id: int)`: Get the latest high/low prices for an item
- `get_average_price(item_id: int)`: Get the average price for an item

### ProfitCalculator

Calculate profits for various OSRS activities.

- `calculate_flip_profit(item_id: int, quantity: int)`: Calculate profit from flipping
- `calculate_craft_profit(output_item_id: int, input_items: Dict, quantity: int)`: Calculate profit from crafting

## Contributing

This is a personal project for testing GitHub Copilot integration. Feel free to fork and experiment!

## License

MIT