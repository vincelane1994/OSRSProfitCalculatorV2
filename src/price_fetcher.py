"""
Item price fetcher for OSRS items using the official OSRS API
"""

import requests
from typing import Optional, Dict


class ItemPriceFetcher:
    """Fetch item prices from the OSRS Grand Exchange API"""
    
    BASE_URL = "https://prices.runescape.com/api/v1/osrs"
    
    def __init__(self):
        self.session = requests.Session()
    
    def get_latest_price(self, item_id: int) -> Optional[Dict]:
        """
        Get the latest price for an item
        
        Args:
            item_id: The OSRS item ID
            
        Returns:
            Dictionary with price information or None if not found
        """
        try:
            url = f"{self.BASE_URL}/latest"
            response = self.session.get(url, timeout=10)
            response.raise_for_status()
            
            data = response.json()
            
            if 'data' in data and str(item_id) in data['data']:
                item_data = data['data'][str(item_id)]
                return {
                    'item_id': item_id,
                    'high': item_data.get('high'),
                    'low': item_data.get('low'),
                    'timestamp': data.get('timestamp')
                }
            
            return None
            
        except requests.RequestException as e:
            print(f"Error fetching price for item {item_id}: {e}")
            return None
    
    def get_average_price(self, item_id: int) -> Optional[int]:
        """
        Get the average price for an item
        
        Args:
            item_id: The OSRS item ID
            
        Returns:
            Average price or None if not available
        """
        price_data = self.get_latest_price(item_id)
        
        if price_data:
            high = price_data.get('high')
            low = price_data.get('low')
            
            if high and low:
                return (high + low) // 2
            elif high:
                return high
            elif low:
                return low
        
        return None
