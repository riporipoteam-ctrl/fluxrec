import json
from functools import lru_cache
from pathlib import Path
from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from ..database import get_db, User, PlayerInventory, PlayerCurrency
from .auth_routes import get_current_user, get_optional_user

router = APIRouter()

AVATAR_CATALOG_PATH = Path(__file__).resolve().parent.parent / "data" / "avataritems.json"


@lru_cache(maxsize=1)
def load_avatar_catalog() -> list[dict]:
    """Load real AvatarItemDesc values understood by the legacy game client."""
    if not AVATAR_CATALOG_PATH.exists():
        return []

    with AVATAR_CATALOG_PATH.open("r", encoding="utf-8") as catalog_file:
        source_items = json.load(catalog_file)

    catalog: list[dict] = []
    seen: set[str] = set()
    for source in source_items:
        description = str(source.get("AvatarItemDesc", "")).strip()
        if not description or description in seen:
            continue
        seen.add(description)
        item_id = description.split(",", 1)[0]
        friendly_name = str(source.get("FriendlyName", "")).strip() or "Avatar Item"
        catalog.append(
            {
                "ItemId": item_id,
                "AvatarItemId": item_id,
                "AvatarItemDesc": description,
                "ItemType": "avatar",
                "Type": "avatar",
                "Name": friendly_name,
                "FriendlyName": friendly_name,
                "Quantity": 1,
                "Owned": True,
            }
        )
    return catalog


DEFAULT_AVATAR_ITEMS = load_avatar_catalog()


@router.get("/roomInventory/room/{room_id}")
async def room_inventory(room_id: str):
    return {"RoomId": room_id, "Items": [], "ItemCount": 0}


@router.get("/roomInventory/room/{room_id}/player")
async def player_room_inventory(room_id: str, user: User = Depends(get_current_user)):
    return {"RoomId": room_id, "PlayerId": user.id, "Items": [], "ItemCount": 0}


@router.get("/roomInventoryItemTags/room/{room_id}")
async def room_inventory_tags(room_id: str):
    return {"Tags": []}


@router.get("/roomOffer/room/{room_id}")
async def room_offers(room_id: str):
    return {"Offers": [], "OfferCount": 0}


@router.get("/roomOffer/room/{room_id}/purchaseCounts")
async def room_offer_purchase_counts(room_id: str):
    return {}


@router.get("/roomGiftDropShops/room/{room_id}")
async def room_gift_drop_shops(room_id: str):
    return {"GiftDropShops": [], "Count": 0}


@router.get("/roomEconConfig/{config_id}")
async def room_econ_config(config_id: str):
    return {"Id": config_id, "IsEnabled": True, "CurrencyType": "token", "StartingBalance": 0}


@router.get("/api/catalog/v1/all")
async def catalog_all(onlyAvailableSkus: bool = True):
    return {"Catalog": DEFAULT_AVATAR_ITEMS, "Items": DEFAULT_AVATAR_ITEMS, "CatalogCount": len(DEFAULT_AVATAR_ITEMS)}


@router.get("/purchasecampaign/allcurrent/v2")
async def purchase_campaigns():
    return []


@router.get("/api/storefronts/v4/balance/{currency_type}")
async def currency_balance(
    currency_type: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    currency = db.query(PlayerCurrency).filter(
        PlayerCurrency.user_id == user.id,
        PlayerCurrency.currency_type == currency_type,
    ).first()
    balance = currency.balance if currency else 0
    return {"CurrencyType": currency_type, "Balance": balance, "UserId": user.id}


@router.get("/api/storefronts/v3/giftdropstore/{store_id}")
async def gift_drop_store(store_id: str):
    return {"StoreId": store_id, "Items": [], "RefreshTime": None}


@router.get("/api/avatar/v1/defaultunlocked")
@router.get("/api/avatar/v1/defaultunlockedavataritems")
async def default_unlocked_avatar_items():
    return {"Items": DEFAULT_AVATAR_ITEMS, "AvatarItems": DEFAULT_AVATAR_ITEMS, "Count": len(DEFAULT_AVATAR_ITEMS)}


@router.get("/api/avatar/v2/items")
@router.get("/api/avatar/v3/items")
@router.get("/api/avatar/v4/items")
async def avatar_items_v4(user: User | None = Depends(get_optional_user), db: Session = Depends(get_db)):
    if user is None:
        return DEFAULT_AVATAR_ITEMS
    inventory = db.query(PlayerInventory).filter(
        PlayerInventory.user_id == user.id,
        PlayerInventory.item_type == "avatar",
    ).all()
    owned = [
        {
            "ItemId": item.item_id,
            "AvatarItemId": item.item_id,
            "ItemType": item.item_type,
            "Type": item.item_type,
            "Quantity": item.quantity,
            "Owned": True,
            "ItemData": json.loads(item.item_data) if item.item_data else {},
        }
        for item in inventory
    ]
    return owned or DEFAULT_AVATAR_ITEMS


@router.get("/api/avatar/v1/defaultbaseavataritems")
async def default_base_avatar_items():
    return {
        "Items": DEFAULT_AVATAR_ITEMS,
        "AvatarItems": DEFAULT_AVATAR_ITEMS,
        "Count": len(DEFAULT_AVATAR_ITEMS),
    }


@router.get("/api/timeLimitedEvents/{event_id}")
async def time_limited_events(event_id: str):
    return {"EventId": event_id, "IsActive": False, "StartTime": None, "EndTime": None}


@router.get("/purchaseRestriction/isplayerrestricted")
async def purchase_restriction(user: User = Depends(get_current_user)):
    return False


@router.get("/api/keepsakes/globalconfig")
async def keepsakes_global_config():
    return {"IsEnabled": True, "MaxKeepsakes": 100, "MaxKeepsakeSlots": 10}


@router.get("/api/keepsakes/categories")
async def keepsakes_categories():
    return {
        "Categories": [
            {"Id": "trophy", "Name": "Trophies", "IconUrl": ""},
            {"Id": "collectible", "Name": "Collectibles", "IconUrl": ""},
            {"Id": "memory", "Name": "Memories", "IconUrl": ""},
        ]
    }
