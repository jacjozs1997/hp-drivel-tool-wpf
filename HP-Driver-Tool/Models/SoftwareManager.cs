using HP_Driver_Tool.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private static string m_platform;
        #endregion
        #region Properties
        public static ObservableConcurrentCollection<SoftwareType> Softwares => m_softwares;
        public static ObservableConcurrentCollection<string> OsVersions => m_osVersions;
        public static ObservableConcurrentCollection<string> OsPlatforms => m_osPlatforms;
        public static ProductNumberInfos ProductNumberInfos => m_productNumberInfos;
        public static SoftwareOsVersions SoftwareOsVersions => m_softwareOsVersions;
        #endregion

        public static void GetOsInfos(string productNumber = null, Action afterAction = null, Action afterFailAction = null)
        {
            if (productNumber == null && App.DeviceProductNumber != null)
            {
                productNumber = App.DeviceProductNumber;
            }
            else if (productNumber == null) return;

            m_softwares.Clear();
            Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    var response = client.DownloadData($"https://support.hp.com/typeahead?q={productNumber}&resultLimit=1&store=tmsstore&languageCode=hu,en&printFields=tmspmseriesvalue,tmspmnamevalue,tmspmnumbervalue,activewebsupportflag,description");
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
                    m_osPlatforms.Clear();

                    m_osVersions.Clear();

                    m_osPlatforms.AddFromEnumerable(m_softwareOsVersions.data.osversions.Select(os => os.name));
                    afterAction?.Invoke();
                }
                else
                {
                    afterFailAction?.Invoke();
                }
            });
        }
        public static void UpdateOsVersion(string platform)
        {
            if (platform == null) return;
            m_osVersions.Clear();
            MainWindowViewModel.SelectedSoftwares.Clear();
            m_softwares.Clear();
            
            m_osVersions.AddFromEnumerable(m_softwareOsVersions.data.osversions.First(pl => pl.name.Equals(platform, StringComparison.OrdinalIgnoreCase)).osVersionList.Select(os => os.name));
            m_platform = platform;
        }
        public static void GetDrivers(string version, Action afterAction = null)
        {
            if (version == null) return;
            Task.Run(() =>
            {
                var platformId = m_softwareOsVersions.data.osversions.First(pl => pl.name.Equals(m_platform, StringComparison.OrdinalIgnoreCase)).osVersionList.First(os => os.name.Equals(version, StringComparison.OrdinalIgnoreCase)).id;

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
                m_softwares.Clear();
                var swResponse = t.Result;
                foreach (var sw in swResponse.data.softwareTypes)
                {
                    foreach (var driver in sw.softwareDriversList)
                    {
                        driver.Parent = sw;
                        driver.latestVersionDriver.Parent = driver;
                        driver.latestVersionDriver.SoftwareId = sw.id;
                    }
                }

                m_softwares.AddFromEnumerable(swResponse.data.softwareTypes);
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
