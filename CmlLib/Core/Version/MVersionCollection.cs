﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using CmlLib.Core.VersionMetadata;

namespace CmlLib.Core.Version
{
    // Collection for MVersionMetadata
    // return MVersion object from MVersionMetadata
    public class MVersionCollection : IEnumerable<MVersionMetadata>
    {
        public MVersionCollection(MVersionMetadata[] versions)
            : this(versions, null, null, null)
        {

        }

        public MVersionCollection(MVersionMetadata[] versions, MinecraftPath originalPath)
            : this(versions, originalPath, null, null)
        {

        }

        public MVersionCollection(
            IEnumerable<MVersionMetadata> versions,
            MinecraftPath? originalPath,
            MVersionMetadata? latestRelease,
            MVersionMetadata? latestSnapshot)
        {
            if (versions == null)
                throw new ArgumentNullException(nameof(versions));

            Versions = new OrderedDictionary();
            foreach (var item in versions)
            {
                Versions.Add(item.Name, item);
            }

            MinecraftPath = originalPath;
            LatestReleaseVersion = latestRelease;
            LatestSnapshotVersion = latestSnapshot;
        }

        // Use OrderedDictionary to keep version order
        protected OrderedDictionary Versions;

        public MVersionMetadata? LatestReleaseVersion { get; private set; }
        public MVersionMetadata? LatestSnapshotVersion { get; private set; }
        public MinecraftPath? MinecraftPath { get; private set; }
        
        public MVersionMetadata this[int index] => (MVersionMetadata)Versions[index]!;

        public MVersionMetadata GetVersionMetadata(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            // Versions[name] will return null if index does not exists
            // Casting from null to MVersionMetadata does not throw NullReferenceException
            MVersionMetadata? versionMetadata = (MVersionMetadata?)Versions[name];
            if (versionMetadata == null)
                throw new KeyNotFoundException("Cannot find " + name);

            return versionMetadata;
        }

        public MVersionMetadata[] ToArray(MVersionSortOption option)
        {
            var sorter = new MVersionMetadataSorter(option);
            return sorter.Sort(this);
        }

        public virtual Task<MVersion> GetVersionAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var versionMetadata = GetVersionMetadata(name);
            return GetVersionAsync(versionMetadata);
        }

        // get MVersion from MVersionMetadata
        public virtual async Task<MVersion> GetVersionAsync(MVersionMetadata versionMetadata)
        {
            if (versionMetadata == null)
                throw new ArgumentNullException(nameof(versionMetadata));

            MVersion startVersion;
            if (MinecraftPath == null)
                startVersion = await versionMetadata.GetVersionAsync()
                    .ConfigureAwait(false);
            else
                startVersion = await versionMetadata.GetVersionAsync(MinecraftPath)
                    .ConfigureAwait(false);

            if (startVersion.IsInherited && !string.IsNullOrEmpty(startVersion.ParentVersionId))
            {
                if (startVersion.ParentVersionId == startVersion.Id) // prevent StackOverFlowException
                    throw new InvalidDataException(
                        "Invalid version json file : inheritFrom property is equal to id property.");

                var baseVersion = await GetVersionAsync(startVersion.ParentVersionId)
                    .ConfigureAwait(false);
                startVersion.InheritFrom(baseVersion);
            }
            
            return startVersion;
        }

        public void AddVersion(MVersionMetadata version)
        {
            Versions[version.Name] = version;
        }

        public bool Contains(string? versionName)
            => !string.IsNullOrEmpty(versionName) && Versions.Contains(versionName);

        public virtual void Merge(MVersionCollection from)
        {
            foreach (var item in from)
            {
                var version = (MVersionMetadata?)Versions[item.Name];
                if (version == null)
                {
                    Versions[item.Name] = item;
                }
                else
                {
                    if (string.IsNullOrEmpty(version.Type))
                    {
                        version.Type = item.Type;
                        version.MType = MVersionTypeConverter.FromString(item.Type);
                    }

                    if (string.IsNullOrEmpty(version.ReleaseTimeStr))
                        version.ReleaseTimeStr = item.ReleaseTimeStr;
                }
            }

            if (this.MinecraftPath == null && from.MinecraftPath != null)
                this.MinecraftPath = from.MinecraftPath;

            if (this.LatestReleaseVersion == null && from.LatestReleaseVersion != null)
                this.LatestReleaseVersion = from.LatestReleaseVersion;

            if (this.LatestSnapshotVersion == null && from.LatestSnapshotVersion != null)
                this.LatestSnapshotVersion = from.LatestSnapshotVersion;
        }

        public IEnumerator<MVersionMetadata> GetEnumerator()
        {
            foreach (DictionaryEntry? item in Versions)
            {
                var entry = item.Value;
                
                var version = (MVersionMetadata)entry.Value!;
                yield return version;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (DictionaryEntry? item in Versions)
            {
                yield return item.Value;
            }
        }
    }
}
