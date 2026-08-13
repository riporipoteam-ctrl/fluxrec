from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session
from sqlalchemy import desc

from ..database import get_db, User, Relationship, ChatThread, ChatMessage, Club, ClubMember
from .auth_routes import get_current_user

router = APIRouter()


@router.get("/api/relationships/v2/get")
async def relationships_get(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    relationships = db.query(Relationship).filter(Relationship.user_id == user.id).all()

    friends = []
    ignored = []
    muted = []

    for rel in relationships:
        target = db.query(User).filter(User.id == rel.target_user_id).first()
        if not target:
            continue
        entry = {
            "PlayerId": target.id,
            "Username": target.username,
            "DisplayName": target.display_name or target.username,
            "PhotoUrl": target.account_photo_url or "",
            "IsOnline": False,
            "RelationshipType": rel.relationship_type,
        }
        if rel.relationship_type == "friend":
            friends.append(entry)
        elif rel.relationship_type == "ignore":
            ignored.append(entry)
        elif rel.relationship_type == "mute":
            muted.append(entry)

    return {
        "Friends": friends,
        "Ignored": ignored,
        "Muted": muted,
        "FriendCount": len(friends),
        "IgnoredCount": len(ignored),
        "MutedCount": len(muted),
    }


@router.post("/api/relationships/v1/ignore")
async def ignore_player():
    return {"status": "ok"}


@router.post("/api/relationships/v1/unignore")
async def unignore_player():
    return {"status": "ok"}


@router.post("/api/relationships/v1/mute")
async def mute_player():
    return {"status": "ok"}


@router.post("/api/relationships/v1/unmute")
async def unmute_player():
    return {"status": "ok"}


@router.get("/api/messages/v1/friendOnlineStatus")
async def friend_online_status():
    return []


@router.get("/api/messages/v2/get")
async def messages_get():
    return []


@router.get("/api/externalfriendinvite/v1/getplatformreferrers")
async def platform_referrers():
    return []


@router.get("/thread")
async def chat_threads(
    maxCount: int = 50,
    mode: int = 0,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    threads = db.query(ChatThread).order_by(desc(ChatThread.last_message_at)).limit(maxCount).all()
    result = []
    for t in threads:
        messages = db.query(ChatMessage).filter(
            ChatMessage.thread_id == t.thread_id
        ).order_by(desc(ChatMessage.sent_at)).limit(10).all()
        result.append({
            "ThreadId": t.thread_id,
            "ThreadType": t.thread_type,
            "LastMessageAt": t.last_message_at.isoformat() if t.last_message_at else None,
            "Messages": [
                {
                    "Id": m.id,
                    "SenderId": m.sender_id,
                    "Content": m.content,
                    "SentAt": m.sent_at.isoformat() if m.sent_at else None,
                    "MessageType": m.message_type,
                }
                for m in reversed(messages)
            ],
        })
    return result


@router.get("/thread/party")
async def party_thread():
    return []


@router.get("/thread/chatPrivacySetting")
async def chat_privacy_setting():
    return {"PrivacySetting": 0}


@router.get("/club/mine/member")
async def my_club_memberships(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    memberships = db.query(ClubMember).filter(ClubMember.user_id == user.id).all()
    result = []
    for m in memberships:
        club = db.query(Club).filter(Club.club_id == m.club_id).first()
        if club:
            result.append({
                "ClubId": club.club_id,
                "Name": club.name,
                "Description": club.description,
                "MemberCount": club.member_count,
                "Role": m.role,
                "ImageUrl": club.image_url or "",
            })
    return result


@router.get("/announcements/v2/mine/unread")
async def unread_announcements():
    return []


@router.get("/announcements/v2/subscription/mine/unread")
async def unread_subscription_announcements():
    return []


@router.get("/club/home/me")
async def club_home_me():
    return {"Clubs": [], "TotalCount": 0}


@router.get("/api/playerevents/v1/all")
async def player_events_all():
    return []


@router.get("/api/playerevents/v1/tagfilters")
async def player_event_tag_filters():
    return []


@router.get("/api/communityboard/v2/current")
async def community_board_current():
    return {"Boards": [], "CurrentBoard": None}
