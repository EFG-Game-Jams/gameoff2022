namespace Game.Server.Services.Models;

public class ItchProfile
{
    public ItchUser User { get; set; }
}

/* Example
{
  "user": {
    "username": "fasterthanlime",
    "gamer": true,
    "display_name": "Amos",
    "cover_url": "https://img.itch.zone/aW1hZ2UyL3VzZXIvMjk3ODkvNjkwOTAxLnBuZw==/100x100%23/JkrN%2Bv.png",
    "url": "https://fasterthanlime.itch.io",
    "press_user": true,
    "developer": true,
    "id": 29789
  }
}
*/