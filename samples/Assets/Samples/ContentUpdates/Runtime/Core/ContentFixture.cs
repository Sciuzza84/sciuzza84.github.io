using System;
using System.Collections.Generic;

namespace GameplaySamples.ContentUpdates
{
    public sealed class ContentFixture
    {
        private readonly Dictionary<string, string> payloads;

        private ContentFixture(ContentPlatform platform, int version, Dictionary<string, string> payloads)
        {
            this.payloads = payloads;
            var entries = new List<ContentEntry>();
            foreach (KeyValuePair<string, string> pair in payloads)
                entries.Add(new ContentEntry(pair.Key, ContentDigest.For(pair.Value)));
            Manifest = new ContentManifest(platform, version, entries);
        }

        public ContentManifest Manifest { get; }

        public string Fetch(string name)
        {
            return payloads.TryGetValue(name, out string payload) ? payload : null;
        }

        public static ContentFixture Create(ContentPlatform platform, int version)
        {
            if (version != 1 && version != 2)
                throw new ArgumentOutOfRangeException(nameof(version));

            string device = platform == ContentPlatform.Android ? "android" : "ios";
            return new ContentFixture(platform, version, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["board.txt"] = device + " | tile layout",
                ["rules.txt"] = device + " | five turns",
                ["palette.txt"] = device + (version == 1 ? " | teal, ink" : " | teal, ink, amber")
            });
        }
    }
}
