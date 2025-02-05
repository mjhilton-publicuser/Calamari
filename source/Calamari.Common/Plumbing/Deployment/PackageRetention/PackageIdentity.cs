﻿using System;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Common.Plumbing.Variables;
using Calamari.Deployment.PackageRetention;
using Newtonsoft.Json;
using Octopus.Versioning;

namespace Calamari.Common.Plumbing.Deployment.PackageRetention
{
    public class PackageIdentity
    {
        public PackageId PackageId { get; }

        [JsonConverter(typeof(VersionConverter))]
        public IVersion Version { get; }
        public string? Path { get; }
        public long FileSizeBytes { get; private set; } = -1;

        public PackageIdentity(string packageId, string version, long fileSizeBytes, VersionFormat versionFormat = VersionFormat.Semver, string? path = null)
            : this(new PackageId(packageId), VersionFactory.CreateVersion(version, versionFormat), path)
        {
            FileSizeBytes = fileSizeBytes;
        }

        public PackageIdentity(string packageId, string version, VersionFormat versionFormat = VersionFormat.Semver, string? path = null)
            : this(new PackageId(packageId), VersionFactory.CreateVersion(version, versionFormat), path)
        {
        }

        public PackageIdentity(IVariables variables, VersionFormat versionFormat = VersionFormat.Semver)
        {
            if (variables == null) throw new ArgumentNullException(nameof(variables));

            var package = variables.Get(PackageVariables.PackageId) ?? throw new Exception("Package ID not found.");
            var version = variables.Get(PackageVariables.PackageVersion) ?? throw new Exception("Package Version not found.");
            Path = variables.Get(TentacleVariables.CurrentDeployment.PackageFilePath);

            var nullableVersion = VersionFactory.TryCreateVersion(version, versionFormat);
            Version = nullableVersion ?? throw new Exception("Unable to determine package version.");

            PackageId = new PackageId(package);
        }

        [JsonConstructor]
        public PackageIdentity(PackageId packageId, IVersion version, string? path = null)
        {
            PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Path = path;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj) || obj.GetType() != GetType())
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            var other = (PackageIdentity)obj;
            return this == other;
        }

        protected bool Equals(PackageIdentity other)
        {
            return Equals(PackageId, other.PackageId)
                   && Equals(Version, other.Version)
                   && Equals(Path, other.Path);
        }

        public override int GetHashCode()
        {
            unchecked
            {   //Generated by rider
                return (PackageId.GetHashCode() * 397) ^ Version.GetHashCode();
            }
        }

        public static bool operator == (PackageIdentity first, PackageIdentity second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(PackageIdentity first, PackageIdentity second)
        {
            return !(first == second);
        }

        public override string ToString()
        {
            return $"{PackageId} v{Version}";
        }

        public void UpdatePackageSize()
        {
            if (FileSizeBytes > 0) return;
            if (string.IsNullOrWhiteSpace(Path))
            {
                FileSizeBytes = -1;
                return;
            }

            FileSizeBytes = CalamariPhysicalFileSystem.GetPhysicalFileSystem().GetFileSize(Path);
        }
    }
}