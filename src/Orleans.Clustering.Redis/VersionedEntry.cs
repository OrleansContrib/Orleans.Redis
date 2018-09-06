using Newtonsoft.Json;

namespace Orleans.Clustering.Redis
{
    internal class VersionedEntry
    {
        public MembershipEntry Entry { get; set; }
        public TableVersion TableVersion { get; set; }
        public string ResourceVersion { get; set; }

        public VersionedEntry(MembershipEntry entry, TableVersion tableVersion)
        {
            Entry = entry;
            TableVersion = tableVersion;
            ResourceVersion = tableVersion.VersionEtag;
        }

        public VersionedEntry()
        {

        }
    }
}