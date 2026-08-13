import uuid
from datetime import datetime, timezone

from sqlalchemy import (
    Column, String, Integer, Boolean, DateTime, Text, JSON, Float, create_engine,
    ForeignKey, inspect, text
)
from sqlalchemy.orm import declarative_base, sessionmaker, relationship

from .config import DATABASE_URL

engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(bind=engine)
Base = declarative_base()


def utcnow():
    return datetime.now(timezone.utc)


class User(Base):
    __tablename__ = "users"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    username = Column(String, unique=True, index=True, nullable=False)
    display_name = Column(String, nullable=True)
    password_hash = Column(String, nullable=False)
    email = Column(String, nullable=True)
    firebase_uid = Column(String, unique=True, index=True, nullable=True)
    email_verified = Column(Boolean, default=False, nullable=False)
    created_at = Column(DateTime, default=utcnow)
    last_login = Column(DateTime, nullable=True)
    is_developer = Column(Boolean, default=False)
    is_rrplus = Column(Boolean, default=False)
    account_photo_url = Column(String, nullable=True)
    player_level = Column(Integer, default=1)
    player_xp = Column(Integer, default=0)
    bio = Column(Text, default="")
    country_code = Column(String, default="US")
    status_visibility = Column(Integer, default=0)


class PlayerSession(Base):
    __tablename__ = "player_sessions"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    token = Column(String, unique=True, index=True, nullable=False)
    created_at = Column(DateTime, default=utcnow)
    expires_at = Column(DateTime, nullable=False)
    is_active = Column(Boolean, default=True)
    current_room_id = Column(String, nullable=True)
    last_heartbeat = Column(DateTime, nullable=True)
    platform = Column(String, default="steam")
    platform_id = Column(String, nullable=True)
    is_in_room = Column(Boolean, default=False)
    region = Column(String, default="us")

    user = relationship("User")


class PlayerState(Base):
    __tablename__ = "player_states"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), unique=True, nullable=False)
    is_online = Column(Boolean, default=False)
    current_room_id = Column(String, nullable=True)
    current_instance_id = Column(String, nullable=True)
    last_room_id = Column(String, nullable=True)
    last_activity = Column(DateTime, nullable=True)
    game_server_region = Column(String, default="us")
    qos_data = Column(Text, nullable=True)

    user = relationship("User")


class PlayerSettings(Base):
    __tablename__ = "player_settings"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), unique=True, nullable=False)
    settings_json = Column(Text, default="{}")

    user = relationship("User")


class Relationship(Base):
    __tablename__ = "relationships"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    target_user_id = Column(String, ForeignKey("users.id"), nullable=False)
    relationship_type = Column(String, nullable=False)
    created_at = Column(DateTime, default=utcnow)


class Room(Base):
    __tablename__ = "rooms"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    room_id = Column(String, unique=True, index=True, nullable=False)
    name = Column(String, nullable=False)
    description = Column(Text, default="")
    owner_id = Column(String, ForeignKey("users.id"), nullable=False)
    is_private = Column(Boolean, default=False)
    is_featured = Column(Boolean, default=False)
    capacity = Column(Integer, default=40)
    player_count = Column(Integer, default=0)
    created_at = Column(DateTime, default=utcnow)
    updated_at = Column(DateTime, default=utcnow, onupdate=utcnow)
    room_version = Column(Integer, default=1)
    subroom_count = Column(Integer, default=1)
    tags = Column(Text, default="[]")
    image_url = Column(String, nullable=True)
    platform_mask = Column(Integer, default=0)
    room_type = Column(String, default="private")
    is_room_approved = Column(Boolean, default=True)
    supports_teleporter = Column(Boolean, default=True)
    supports_room_door = Column(Boolean, default=True)

    owner = relationship("User")


class RoomSave(Base):
    __tablename__ = "room_saves"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    room_id = Column(String, ForeignKey("rooms.room_id"), nullable=False)
    subroom_index = Column(Integer, default=0)
    save_data = Column(Text, default="{}")
    created_at = Column(DateTime, default=utcnow)
    updated_at = Column(DateTime, default=utcnow, onupdate=utcnow)
    save_version = Column(Integer, default=1)


class PlayerInventory(Base):
    __tablename__ = "player_inventory"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    item_id = Column(String, nullable=False)
    item_type = Column(String, nullable=False)
    quantity = Column(Integer, default=1)
    acquired_at = Column(DateTime, default=utcnow)
    item_data = Column(Text, default="{}")


class PlayerCurrency(Base):
    __tablename__ = "player_currencies"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    currency_type = Column(String, nullable=False)
    balance = Column(Integer, default=0)


class Outfit(Base):
    __tablename__ = "outfits"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    outfit_name = Column(String, nullable=False)
    outfit_data = Column(Text, default="{}")
    is_saved = Column(Boolean, default=True)
    created_at = Column(DateTime, default=utcnow)


class ChatThread(Base):
    __tablename__ = "chat_threads"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    thread_id = Column(String, unique=True, index=True, nullable=False)
    thread_type = Column(String, default="normal")
    created_at = Column(DateTime, default=utcnow)
    last_message_at = Column(DateTime, nullable=True)


class ChatMessage(Base):
    __tablename__ = "chat_messages"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    thread_id = Column(String, ForeignKey("chat_threads.thread_id"), nullable=False)
    sender_id = Column(String, ForeignKey("users.id"), nullable=False)
    content = Column(Text, nullable=False)
    sent_at = Column(DateTime, default=utcnow)
    message_type = Column(Integer, default=0)


class Notification(Base):
    __tablename__ = "notifications"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    notification_type = Column(String, nullable=False)
    title = Column(String, nullable=True)
    body = Column(Text, nullable=True)
    data = Column(Text, default="{}")
    is_read = Column(Boolean, default=False)
    created_at = Column(DateTime, default=utcnow)


class RoomVisit(Base):
    __tablename__ = "room_visits"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    room_id = Column(String, nullable=False)
    visited_at = Column(DateTime, default=utcnow)


class Club(Base):
    __tablename__ = "clubs"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    club_id = Column(String, unique=True, index=True, nullable=False)
    name = Column(String, nullable=False)
    description = Column(Text, default="")
    owner_id = Column(String, ForeignKey("users.id"), nullable=False)
    created_at = Column(DateTime, default=utcnow)
    member_count = Column(Integer, default=1)
    image_url = Column(String, nullable=True)


class ClubMember(Base):
    __tablename__ = "club_members"

    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    club_id = Column(String, ForeignKey("clubs.club_id"), nullable=False)
    user_id = Column(String, ForeignKey("users.id"), nullable=False)
    role = Column(String, default="member")
    joined_at = Column(DateTime, default=utcnow)


def init_db():
    Base.metadata.create_all(bind=engine)
    # create_all does not add columns to an existing SQLite database. Keep the
    # local install upgradeable without deleting players, rooms, or saves.
    inspector = inspect(engine)
    existing = {column["name"] for column in inspector.get_columns("users")}
    migrations = {
        "firebase_uid": "ALTER TABLE users ADD COLUMN firebase_uid VARCHAR",
        "email_verified": (
            "ALTER TABLE users ADD COLUMN email_verified BOOLEAN "
            "NOT NULL DEFAULT 0"
        ),
    }
    with engine.begin() as connection:
        for column, statement in migrations.items():
            if column not in existing:
                connection.execute(text(statement))
        connection.execute(
            text(
                "CREATE UNIQUE INDEX IF NOT EXISTS "
                "ix_users_firebase_uid ON users (firebase_uid)"
            )
        )


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
