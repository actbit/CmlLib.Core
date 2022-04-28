﻿using CmlLib.Utils;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace CmlLib.Core.Downloader
{
    public class SequenceDownloader : IDownloader
    {
        private readonly HttpClientDownloadHelper downloader;

        public bool IgnoreInvalidFiles { get; set; } = true;

        public SequenceDownloader() : this(HttpUtil.HttpClient)
        {

        }

        public SequenceDownloader(HttpClient client)
        {
            downloader = new HttpClientDownloadHelper(client);
        }

        public async Task DownloadFiles(DownloadFile[] files, 
            IProgress<DownloadFileChangedEventArgs>? fileProgress,
            IProgress<ProgressChangedEventArgs>? downloadProgress)
        {
            if (files.Length == 0)
                return;

            var byteProgress = new Progress<DownloadFileByteProgress>(progress =>
            {
                var percent = (float)progress.ProgressedBytes / progress.TotalBytes * 100;
                downloadProgress?.Report(new ProgressChangedEventArgs((int)percent, null));
            });

            fileProgress?.Report(
                new DownloadFileChangedEventArgs(files[0].Type, this, null, files.Length, 0));

            for (int i = 0; i < files.Length; i++)
            {
                DownloadFile file = files[i];

                try
                {
                    await downloader.DownloadFileAsync(file, byteProgress).ConfigureAwait(false);

                    if (file.AfterDownload != null)
                    {
                        foreach (var item in file.AfterDownload)
                        {
                            await item().ConfigureAwait(false);
                        }
                    }
                    
                    fileProgress?.Report(
                        new DownloadFileChangedEventArgs(file.Type, this, file.Name, files.Length, i));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());

                    if (!IgnoreInvalidFiles)
                        throw new MDownloadFileException(ex.Message, ex, files[i]);
                }
            }
        }
    }
}
