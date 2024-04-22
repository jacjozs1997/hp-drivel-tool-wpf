using GalaSoft.MvvmLight;
using System.Collections.Generic;

namespace HP_Driver_Tool.Models
{
    public class ProductNumberInfos : ObservableObject
    {
        public List<MatcheProductNumber> matches { get; set; }
        public int totalCount { get; set; }
        public class MatcheProductNumber
        {
            public string pmSeriesOid { get; set; }
            public string pmNameOid { get; set; }
            public string pmNumberOid { get; set; }
            public string activeWebSupportFlag { get; set; }
            public string matchScore { get; set; }
            public string productname { get; set; }
            public string productId { get; set; }
            public string description { get; set; }
        }
    }
}
