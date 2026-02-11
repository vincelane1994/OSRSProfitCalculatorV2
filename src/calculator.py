"""
Profit calculator for OSRS items
"""

from typing import Dict, Optional
from .price_fetcher import ItemPriceFetcher


class ProfitCalculator:
    """Calculate profit for OSRS activities"""
    
    def __init__(self):
        self.price_fetcher = ItemPriceFetcher()
    
    def calculate_craft_profit(self, 
                               output_item_id: int,
                               input_items: Dict[int, int],
                               quantity: int = 1) -> Optional[Dict]:
        """
        Calculate profit from crafting
        
        Args:
            output_item_id: The item ID of the crafted output
            input_items: Dictionary mapping item IDs to quantities needed
            quantity: Number of items to craft
            
        Returns:
            Dictionary with profit information
        """
        # Get output item price
        output_price = self.price_fetcher.get_average_price(output_item_id)
        if output_price is None:
            return None
        
        # Calculate input cost
        total_input_cost = 0
        input_details = []
        
        for item_id, item_quantity in input_items.items():
            price = self.price_fetcher.get_average_price(item_id)
            if price is None:
                return None
            
            cost = price * item_quantity
            total_input_cost += cost
            input_details.append({
                'item_id': item_id,
                'quantity': item_quantity,
                'unit_price': price,
                'total_cost': cost
            })
        
        # Calculate profit
        revenue = output_price * quantity
        cost = total_input_cost * quantity
        profit = revenue - cost
        profit_per_item = profit // quantity if quantity > 0 else 0
        
        return {
            'output_item_id': output_item_id,
            'output_price': output_price,
            'quantity': quantity,
            'revenue': revenue,
            'total_cost': cost,
            'profit': profit,
            'profit_per_item': profit_per_item,
            'input_details': input_details
        }
    
    def calculate_flip_profit(self,
                             item_id: int,
                             quantity: int = 1) -> Optional[Dict]:
        """
        Calculate potential profit from flipping (buy low, sell high)
        
        Args:
            item_id: The item ID to flip
            quantity: Number of items to flip
            
        Returns:
            Dictionary with flip profit information
        """
        price_data = self.price_fetcher.get_latest_price(item_id)
        if price_data is None:
            return None
        
        buy_price = price_data.get('low')
        sell_price = price_data.get('high')
        
        if buy_price is None or sell_price is None:
            return None
        
        profit_per_item = sell_price - buy_price
        total_profit = profit_per_item * quantity
        
        return {
            'item_id': item_id,
            'buy_price': buy_price,
            'sell_price': sell_price,
            'quantity': quantity,
            'profit_per_item': profit_per_item,
            'total_profit': total_profit,
            'roi': profit_per_item / buy_price * 100.0 if buy_price > 0 else 0.0
        }
