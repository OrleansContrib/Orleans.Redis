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
            this.Entry = entry;
            this.TableVersion = tableVersion;
            this.ResourceVersion = tableVersion.VersionEtag;
        }
        public VersionedEntry()
        {

        }
    }
}