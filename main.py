#!/usr/bin/env python3
"""
Main CLI entry point for OSRS Profit Calculator V2
"""

import logging
from src.calculator import ProfitCalculator

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)


def format_gp(amount: int) -> str:
    """Format gold pieces with commas"""
    return f"{amount:,} gp"


def main():
    """Main CLI function"""
    print("=" * 50)
    print("OSRS Profit Calculator V2")
    print("=" * 50)
    print()
    
    calculator = ProfitCalculator()
    
    # Example: Calculate profit for flipping Nature Runes (item ID: 561)
    print("Example 1: Flipping Nature Runes")
    print("-" * 50)
    
    result = calculator.calculate_flip_profit(item_id=561, quantity=1000)
    
    if result:
        print(f"Item ID: {result['item_id']}")
        print(f"Buy Price: {format_gp(result['buy_price'])}")
        print(f"Sell Price: {format_gp(result['sell_price'])}")
        print(f"Quantity: {result['quantity']:,}")
        print(f"Profit per item: {format_gp(result['profit_per_item'])}")
        print(f"Total Profit: {format_gp(result['total_profit'])}")
        print(f"ROI: {result['roi']:.2f}%")
    else:
        print("Could not fetch price data")
    
    print()
    print("=" * 50)


if __name__ == "__main__":
    main()
