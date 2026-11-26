using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GameplaySamples.ContentUpdates
{
    public enum ContentPlatform
    {
        Android,
        Ios
    }

    public enum UpdateOutcome
    {
        Applied,
        Cancelled,
        InvalidPayload
    }

    public sealed class ContentEntry
    {
        public ContentEntry(string name, string digest)
        {
            Name = name;
            Digest = digest;
        }

        public string Name { get; }
        public string Digest { get; }
    }

    public sealed class ContentManifest
    {
        public ContentManifest(ContentPlatform platform, int version, IEnumerable<ContentEntry> entries)
        {
            Platform = platform;
            Version = version;
            var names = new HashSet<string>(StringComparer.Ordinal);
            var files = new List<ContentEntry>();
            foreach (ContentEntry entry in entries)
            {
                if (!names.Add(entry.Name))
                    throw new ArgumentException("The manifest names the same file twice.", nameof(entries));
                files.Add(entry);
            }
            Files = files.AsReadOnly();
        }

        public ContentPlatform Platform { get; }
        public int Version { get; }
        public IReadOnlyList<ContentEntry> Files { get; }
    }

    public sealed class InstalledContent
    {
        private readonly Dictionary<string, StoredFile> files;

        private InstalledContent(ContentPlatform platform, int version, Dictionary<string, StoredFile> files)
        {
            this.files = files;
            Platform = platform;
            Version = version;
        }

        public ContentPlatform Platform { get; }
        public int Version { get; }
        public int Count => files.Count;

        public static InstalledContent Empty(ContentPlatform platform)
        {
            return new InstalledContent(platform, 0, new Dictionary<string, StoredFile>(StringComparer.Ordinal));
        }

        public bool Contains(ContentEntry entry)
        {
            return files.TryGetValue(entry.Name, out StoredFile file) && file.Digest == entry.Digest;
        }

        public string Read(string name)
        {
            return files.TryGetValue(name, out StoredFile file) ? file.Payload : null;
        }

        public static UpdateResult Update(InstalledContent current, ContentManifest target,
            Func<string, string> download, Func<bool> isCancelled)
        {
            if (current.Platform != target.Platform)
                throw new ArgumentException("The manifest platform must match the installation.", nameof(target));

            var staged = new Dictionary<string, StoredFile>(StringComparer.Ordinal);
            int downloaded = 0;
            int reused = 0;
            foreach (ContentEntry entry in target.Files)
            {
                if (isCancelled())
                    return new UpdateResult(UpdateOutcome.Cancelled, current, downloaded, reused);

                if (current.Contains(entry))
                {
                    staged.Add(entry.Name, current.files[entry.Name]);
                    reused++;
                    continue;
                }

                string payload = download(entry.Name);
                downloaded++;
                if (payload == null || ContentDigest.For(payload) != entry.Digest)
                    return new UpdateResult(UpdateOutcome.InvalidPayload, current, downloaded, reused, entry.Name);

                staged.Add(entry.Name, new StoredFile(entry.Digest, payload));
            }

            if (isCancelled())
                return new UpdateResult(UpdateOutcome.Cancelled, current, downloaded, reused);

            // Commit the entire staged view in one step. A failed attempt retains the previous snapshot.
            var installed = new InstalledContent(target.Platform, target.Version, staged);
            return new UpdateResult(UpdateOutcome.Applied, installed, downloaded, reused);
        }

        private readonly struct StoredFile
        {
            public StoredFile(string digest, string payload)
            {
                Digest = digest;
                Payload = payload;
            }

            public string Digest { get; }
            public string Payload { get; }
        }
    }

    public readonly struct UpdateResult
    {
        public UpdateResult(UpdateOutcome outcome, InstalledContent installed, int downloaded, int reused,
            string failedFile = null)
        {
            Outcome = outcome;
            Installed = installed;
            Downloaded = downloaded;
            Reused = reused;
            FailedFile = failedFile;
        }

        public UpdateOutcome Outcome { get; }
        public InstalledContent Installed { get; }
        public int Downloaded { get; }
        public int Reused { get; }
        public string FailedFile { get; }
    }

    public static class ContentDigest
    {
        public static string For(string payload)
        {
            using (SHA256 sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
