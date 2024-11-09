namespace SpinArchipelago
{
    // ReSharper disable once DefaultStructEqualityIsUsed.Global
#pragma warning disable CS0660, CS0661
    public struct ArchipelagoSongId
#pragma warning restore CS0660, CS0661
    {
        public int Id;
        public string Title;
        public string Subtitle;
        public string Artist;
        public string FeaturingArtist;
        public string Charter;

        public ArchipelagoSongId(int id, MetadataHandle handle)
        {
            Id = id;
            var info = handle.TrackInfoMetadata;
            Title = info.title;
            Subtitle = info.subtitle;
            Artist = info.artistName;
            FeaturingArtist = info.featArtists;
            Charter = info.charter;
        }
        
        public static bool operator ==(ArchipelagoSongId song, MetadataHandle handle)
        {
            if (handle == null) return false;
            var info = handle.TrackInfoMetadata;
            return song.Title == info.title &&
                   song.Subtitle == info.subtitle &&
                   song.FeaturingArtist == info.featArtists &&
                   song.Artist == info.artistName &&
                   song.Charter == info.charter;
        }

        public static bool operator !=(ArchipelagoSongId song, MetadataHandle handle)
        {
            return !(song == handle);
        }
    }
}
