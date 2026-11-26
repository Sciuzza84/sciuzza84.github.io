using System;
using System.Collections.Generic;

namespace GameplaySamples.ContentUpdates
{
    public sealed class ContentFixture
    {
        private readonly Dictionary<string, string> payloads;
        private readonly string corruptionTarget;

        private ContentFixture(ContentPlatform platform, int version, (string Name, string Payload)[] files,
            string corruptionTarget)
        {
            this.corruptionTarget = corruptionTarget;
            payloads = new Dictionary<string, string>(StringComparer.Ordinal);
            var entries = new List<ContentEntry>();
            foreach (var file in files)
            {
                payloads.Add(file.Name, file.Payload);
                entries.Add(new ContentEntry(file.Name, ContentDigest.For(file.Payload)));
            }
            Manifest = new ContentManifest(platform, version, entries);
        }

        public ContentManifest Manifest { get; }

        public string Fetch(string name)
        {
            return payloads.TryGetValue(name, out string payload) ? payload : null;
        }

        public string FetchWithCorruption(string name)
        {
            string payload = Fetch(name);
            return payload != null && (corruptionTarget == null || name == corruptionTarget)
                ? payload + " (corrupted)"
                : payload;
        }

        public static ContentFixture Create(ContentPlatform platform, int version)
        {
            if (version < 1 || version > 3)
                throw new ArgumentOutOfRangeException(nameof(version));

            string device = platform == ContentPlatform.Android ? "android" : "ios";
            return new ContentFixture(platform, version, new[]
            {
                ("board.txt", device + (version == 3 ? " | expanded tile layout" : " | tile layout")),
                ("rules.txt", device + (version == 3 ? " | six turns" : " | five turns")),
                ("palette.txt", device + (version == 1 ? " | teal, ink" : " | teal, ink, amber"))
            }, version == 3 ? "rules.txt" : null);
        }
    }
}
