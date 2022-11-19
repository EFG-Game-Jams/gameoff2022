namespace Game.Server.Models.Session;

public record class CreateSessionResponse(Guid Secret, string PlayerName);