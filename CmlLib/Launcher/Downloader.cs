﻿using System;
using System.IO.Compression;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace CmlLib.Launcher
{
    public delegate void MChangeDownloadProgress(ChangeProgressEventArgs e);

    /// <summary>
    /// 게임 실행에 필요한 라이브러리, 게임, 리소스 등을 다운로드합니다.
    /// </summary>
    public class MDownloader
    {
        public event MChangeDownloadProgress ChangeProgressEvent;
        public event DownloadProgressChangedEventHandler ChangeFileProgressEvent;

        MProfile profile;

        /// <summary>
        /// 실행에 필요한 파일들을 profile 에서 불러옵니다.
        /// </summary>
        /// <param name="_profile">불러올 프로파일</param>
        public MDownloader(MProfile _profile)
        {
            ChangeProgressEvent += delegate { };
            ChangeFileProgressEvent += delegate { };
            this.profile = _profile;
        }

        /// <summary>
        /// Download All files that require to launch
        /// </summary>
        /// <param name="resource"></param>
        public void DownloadAll(bool resource = true)
        {
            DownloadLibraries();

            if (resource)
            {
                DownloadIndex();
                DownloadResource();
            }

            DownloadMinecraft();
        }

        /// <summary>
        /// 실행에 필요한 라이브러리들을 프로파일에서 가져와 모두 다운로드합니다.
        /// </summary>
        public void DownloadLibraries()
        {
            using (var wc = new WebClient()) // 웹클라이언트 객체생성, 이벤트등록
            {
                wc.DownloadProgressChanged += Library_DownloadProgressChanged;
                wc.DownloadFileCompleted += wcd;

                int index = 0; // 현재 다운로드중인 파일의 순서 (이벤트 생성용)
                int maxCount = profile.Libraries.Count; // 모든 파일의 갯수
                foreach (var item in profile.Libraries) // 프로파일의 모든 라이브러리 반복
                {
                    try
                    {
                        l(MFile.Library, item.Name, maxCount, index); // 이벤트 발생
                        if (item.IsRequire &&
                            item.Path != "" &&
                            !File.Exists(item.Path) &&
                            item.Url != "") // 파일이 존재하지 않을 때만
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(item.Path)); //파일 다운로드
                            d(wc, item.Url, item.Path);
                        }
                        index++;
                    }
                    catch { }
                }
            }
        }

        // 아래 코드는 비동기 코드를 동기적으로 실행하는 코드

        bool iscom = false;
        void d(WebClient wc, string a, string b)
        {
            if (a == null) return;

            iscom = false;
            wc.DownloadFileAsync(new Uri(a), b);
            while (!iscom)
            {
                Thread.Sleep(50);
            }
        }
        private void wcd(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            iscom = true;
        }
        private void Library_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ChangeFileProgressEvent(sender, e);
        }

        ////////////////////////////////////////////////////

        /// <summary>
        /// 다운로드 받아야 할 리소스 파일들이 저장된 인덱스 파일을 다운로드합니다.
        /// </summary>
        public void DownloadIndex()
        {
            string path = Minecraft.Index + profile.AssetId + ".json"; //로컬 인덱스파일의 경로

            if (!File.Exists(path) &&
                profile.AssetUrl != "") //로컬에 없을때
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)); //폴더생성
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(profile.AssetUrl, path); //파일 다운로드
                }
            }
        }

        /// <summary>
        /// BGM, 효과음, 언어팩 등 리소스파일들을 다운로드합니다.
        /// </summary>
        public void DownloadResource()
        {
            var indexpath = Minecraft.Index + profile.AssetId + ".json";
            if (!File.Exists(indexpath)) return;

            using (var wc = new WebClient())
            {
                bool Isvirtual = false;

                var json = File.ReadAllText(indexpath);
                var index = JObject.Parse(json);

                try
                {
                    if (index["virtual"].ToString().ToLower() == "true") //virtual 이 true 인지 확인
                        Isvirtual = true;
                }
                catch { }

                var list = (JObject)index["objects"]; //리소스 리스트를 생성 ('objects' 오브젝트)

                int pi = 0;
                foreach (var item in list)
                {
                    pi++;
                    l(MFile.Resource, "", list.Count, pi);
                    JObject job = (JObject)item.Value;
                    string path = job["hash"].ToString()[0].ToString() + job["hash"].ToString()[1].ToString() + "/" + job["hash"].ToString(); //리소스 경로를 설정 ex) a9\a9ea생략85ad93d
                    string hashpath = (Minecraft.Assets + "objects\\" + path).Replace("/", "\\"); //해쉬 리소스 경로 설정
                    string filepath = (Minecraft.Assets + "virtual\\legacy\\" + item.Key).Replace("/", "\\"); //legacy 폴더에 저장할 리소스경로 설정
                    Directory.CreateDirectory(Path.GetDirectoryName(hashpath)); //폴더생성

                    if (!File.Exists(hashpath)) //해쉬 리소스 경로에 파일이 없을때
                    {
                        wc.DownloadFile("http://resources.download.minecraft.net/" + path, hashpath); //다운로드
                    }

                    if (Isvirtual && !File.Exists(filepath)) //virtual 이 true 이고 파일이 없을떄
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                        File.Copy(hashpath, filepath, true); //다운로드
                    }
                }
            }
        }

        bool iscomp = false;

        /// <summary>
        /// 마인크래프트를 다운로드합니다.
        /// </summary>
        public void DownloadMinecraft()
        {
            if (profile.ClientDownloadUrl == "") return;

            string id = profile.Id;
            if (!File.Exists(Minecraft.Versions + id + "\\" + id + ".jar")) //파일이 없을때
            {
                Directory.CreateDirectory(Minecraft.Versions + id); //폴더생성
                using (var wc = new WebClient())
                {
                    iscomp = false;
                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                    l(MFile.Minecraft, "", 1, 0);
                    wc.DownloadFileAsync(new Uri(profile.ClientDownloadUrl), Minecraft.Versions + id + "\\" + id + ".jar");

                    while (!iscomp)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            iscomp = true;
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ChangeFileProgressEvent(sender, e);
            l(MFile.Minecraft, "", 1, 1);
        }

        private void l(MFile filetype, string filename, int max, int value)
        {
            try
            {
                ChangeProgressEvent(new ChangeProgressEventArgs()
                {
                    FileKind = filetype,
                    FileName = filename,
                    MaxValue = max,
                    CurrentValue = value
                });
            }
            catch { }
        }
    }
}