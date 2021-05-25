﻿using CmlLib.Core.Downloader;
using CmlLib.Core.Version;
using CmlLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CmlLib.Core.Files
{
    public class ModChecker : IFileChecker
    {
        public bool CheckHash { get; set; } = true;
        public ModFile[] Mods { get; set; }

        public DownloadFile[] CheckFiles(MinecraftPath path, MVersion version,
            IProgress<DownloadFileChangedEventArgs> progress)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            return CheckFilesTaskAsync(path, version, progress)
                .GetAwaiter().GetResult();
        }

        public async Task<DownloadFile[]> CheckFilesTaskAsync(MinecraftPath path, MVersion version,
            IProgress<DownloadFileChangedEventArgs> progress)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            string lastModName = "";
            int progressed = 0;
            var files = new List<DownloadFile>(Mods.Length);
            foreach (ModFile mod in Mods)
            {
                if (await CheckDownloadRequireAsync(path, mod).ConfigureAwait(false))
                {
                    files.Add(new DownloadFile
                    {
                        Type = MFile.Others,
                        Name = mod.Name,
                        Path = Path.Combine(path.BasePath, mod.Path),
                        Url = mod.Url
                    });
                    lastModName = mod.Name;
                }

                progressed++;
                progress?.Report(new DownloadFileChangedEventArgs(
                    MFile.Others, mod.Name, Mods.Length, progressed));
            }

            progress?.Report(new DownloadFileChangedEventArgs(
    MFile.Others, lastModName, Mods.Length, Mods.Length));

            return files.Distinct().ToArray();
        }

        private async Task<bool> CheckDownloadRequireAsync(MinecraftPath path, ModFile mod)
        {
            return !string.IsNullOrEmpty(mod.Url)
                && !string.IsNullOrEmpty(mod.Path)
                && !await IOUtil.CheckFileValidationAsync(Path.Combine(path.BasePath, mod.Path), mod.Hash, CheckHash).ConfigureAwait(false);
        }
    }
}
