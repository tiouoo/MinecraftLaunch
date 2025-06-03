namespace MinecraftLaunch.Base.Models.Network;

public record CurseforgeResource {
    public required int Id { get; init; }
    public required int ClassId { get; init; }
    public required int DownloadCount { get; init; }
    public required string Name { get; init; }
    public required string IconUrl { get; init; }
    public required string Summary { get; init; }
    public required string WebsiteUrl { get; init; }
    public required DateTime DateModified { get; init; }
    public required IEnumerable<string> Authors { get; init; }
    public required IEnumerable<string> Categories { get; init; }
    public required IEnumerable<string> Screenshots { get; init; }
}