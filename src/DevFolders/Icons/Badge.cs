using System.Windows.Media;

namespace DevFolders.Icons;

public abstract record Badge(BadgeArtwork Artwork);

public sealed record ImageBadge(BadgeArtwork Artwork) : Badge(Artwork);

public sealed record GlyphBadge(BadgeArtwork Artwork, Color Background) : Badge(Artwork);
