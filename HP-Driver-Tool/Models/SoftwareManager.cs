﻿using HP_Driver_Tool.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HP_Driver_Tool.Models
{
    internal class SoftwareManager
    {
        #region Variables
        private static ProductNumberInfos m_productNumberInfos;
        private static SoftwareOsVersions m_softwareOsVersions;
        private static ObservableConcurrentCollection<SoftwareType> m_softwares = new ObservableConcurrentCollection<SoftwareType>();
        private static ObservableConcurrentCollection<string> m_osPlatforms = new ObservableConcurrentCollection<string>();
        private static ObservableConcurrentCollection<string> m_osVersions = new ObservableConcurrentCollection<string>();
        private static string m_productNumber;
        private static string m_platform;
        #endregion
        #region Properties
        public static ObservableConcurrentCollection<SoftwareType> Softwares => m_softwares;
        public static ObservableConcurrentCollection<string> OsVersions => m_osVersions;
        public static ObservableConcurrentCollection<string> OsPlatforms => m_osPlatforms;
        public static ProductNumberInfos ProductNumberInfos => m_productNumberInfos;
        public static SoftwareOsVersions SoftwareOsVersions => m_softwareOsVersions;
        public static string ProductNumber => m_productNumber;
        #endregion

        public static void GetOsInfos(string productNumber = null, Action afterAction = null, Action<string> afterFailAction = null)
        {
            OsPlatforms.Clear();
            OsVersions.Clear();
            Softwares.Clear();
            m_platform = null;
            if (productNumber == null && App.DeviceProductNumber != null)
            {
                productNumber = App.DeviceProductNumber;
            }
            else if (productNumber == null) return;

            if (productNumber.Contains('#'))
            {
                productNumber = productNumber.Substring(0, productNumber.IndexOf('#'));
            }

            if (!Regex.IsMatch(productNumber, @"^\w{7}$"))
            {
                afterFailAction?.Invoke("Invalid product number");
                return;
            }

            m_productNumber = productNumber;

            Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    var response = client.DownloadData($"https://support.hp.com/typeahead?q={m_productNumber}&resultLimit=1&store=tmsstore&languageCode=hu,en&printFields=tmspmseriesvalue,tmspmnamevalue,tmspmnumbervalue,activewebsupportflag,description");
                    m_productNumberInfos = JsonConvert.DeserializeObject<ProductNumberInfos>(Encoding.UTF8.GetString(response));

                    if (m_productNumberInfos.totalCount > 0)
                    {
                        response = client.DownloadData($"https://support.hp.com/wcc-services/swd-v2/osVersionData?cc=hu&lc=hu&productOid={m_productNumberInfos.matches[0].pmNameOid}");
                        Console.WriteLine(Encoding.UTF8.GetString(response));
                        m_softwareOsVersions = JsonConvert.DeserializeObject<SoftwareOsVersions>(Encoding.UTF8.GetString(response));
                        return true;
                    }
                    return false;
                }
            }).ContinueWith(t =>
            {
                if (t.Result)
                {
                    OsPlatforms.Clear();

                    OsVersions.Clear();

                    OsPlatforms.AddFromEnumerable(m_softwareOsVersions.data.osversions.Select(os => os.name));
                    afterAction?.Invoke();
                }
                else
                {
                    OsPlatforms.Clear();

                    OsVersions.Clear();

                    afterFailAction?.Invoke("Not found product number");
                }
            });
        }
        public static void UpdateOsVersion(string platform)
        {
            bool startUpSearch = platform == null;

            if (platform == null && App.DeviceOS != null)
            {
                platform = App.DeviceOS;
            }
            else if (platform == null) return;

            m_platform = platform;

            OsVersions.Clear();
            MainWindowViewModel.SelectedSoftwares.Clear();
            Softwares.Clear();

            var osPlatformVersions = m_softwareOsVersions.data.osversions.First(pl => pl.name.Equals(m_platform, StringComparison.OrdinalIgnoreCase)).osVersionList;

            OsVersions.AddFromEnumerable(osPlatformVersions.Select(os => os.name));
        }
        private static int HammingDistance(string firstStrand, string secondStrand)
        {
            if (firstStrand.Length < secondStrand.Length)
            {
                var diff = secondStrand.Length - firstStrand.Length;
                for (int i = 0; i < diff; i++)
                {
                    firstStrand += "*";
                }
            } 
            else if (secondStrand.Length < firstStrand.Length)
            {
                var diff = firstStrand.Length - secondStrand.Length;
                for (int i = 0; i < diff; i++)
                {
                    secondStrand += "*";
                }
            }

            return firstStrand.Zip(secondStrand, (c, b) => c != b).Count(f => f);
        }
        public static void GetDrivers(string version, out string finalVersion, Action afterAction = null)
        {
            finalVersion = null;

            if (m_platform == null) {
                afterAction?.Invoke();
                return; 
            }
            if (version == null && App.DeviceOsVersion != null)
            {
                version = App.DeviceOsVersion;
            }
            else if (version == null)
            {
                afterAction?.Invoke();
                return;
            }

            if (m_productNumberInfos.matches == null)
            {
                afterAction?.Invoke();
                return;
            }

            var osPlatformVersions = m_softwareOsVersions.data.osversions.First(pl => pl.name.Equals(m_platform, StringComparison.OrdinalIgnoreCase)).osVersionList;

            int min = short.MaxValue;
            int hamming = 0;
            string platformId = "";
            foreach (var osVersion in osPlatformVersions) {
                hamming = HammingDistance(osVersion.name.Replace(" ", null).ToLower(), $"{m_platform}{version}".Replace(" ", null).ToLower());
                if (hamming < min)
                {
                    hamming = min;
                    platformId = osVersion.id;
                    finalVersion = osVersion.name;
                }
                if (hamming == 0) break;
            }

            Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://support.hp.com/wcc-services/swd-v2/driverDetails");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        //  write your json content here

                        string json = JsonConvert.SerializeObject(new SoftwaresPostBody()
                        {
                            lc = "hu",
                            cc = "hu",
                            osTMSId = platformId,
                            osName = m_platform,
                            productNumberOid = m_productNumberInfos.matches[0].productId,
                            productSeriesOid = m_productNumberInfos.matches[0].pmSeriesOid,
                            platformId = platformId,
                            productNameOid = m_productNumberInfos.matches[0].pmNameOid,
                        });


                        streamWriter.Write(json);
                    }
                    string response = null;
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        response = streamReader.ReadToEnd();
                    }
                    return JsonConvert.DeserializeObject<SoftwaresResponse>(response);
                }
            }).ContinueWith(t =>
            {
                Softwares.Clear();
                var swResponse = t.Result;
                foreach (var sw in swResponse.data.softwareTypes)
                {
                    foreach (var driver in sw.softwareDriversList)
                    {
                        driver.Parent = sw;
                        driver.latestVersionDriver.Parent = driver;
                        driver.latestVersionDriver.SoftwareId = sw.id;
                    }
                    sw.softwareDriversList.Sort((a, b) => b.latestVersionDriver.VersionUpdatedDate.CompareTo(a.latestVersionDriver.VersionUpdatedDate));
                }
                Softwares.AddFromEnumerable(swResponse.data.softwareTypes);
                afterAction?.Invoke();
            });
        }
        class SoftwaresPostBody
        {
            public string lc { get; set; }
            public string cc { get; set; }
            public string osTMSId { get; set; }
            public string osName { get; set; }
            public string productNumberOid { get; set; }
            public string productSeriesOid { get; set; }
            public string platformId { get; set; }
            public string productNameOid { get; set; }
        }
    }
}
