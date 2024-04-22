using System;
using System.Collections.Generic;

namespace HP_Driver_Tool.Models
{
    public class SoftwareOsVersions
    {
        public int timeElapsedInMS { get; set; }
        public int statusCode { get; set; }
        public string statusMessage { get; set; }
        public SoftwareOsVersionArray data { get; set; }
        public class SoftwareOsVersionArray
        {
            public List<SoftwareOsVersionGroup> osversions { get; set; }
        }
    }
    public class SoftwareOsVersionGroup
    {
        public string name { get; set; }
        public SortedSet<SoftwareOsVersion> osVersionList { get; set; }
    }
    public class SoftwareOsVersion : IComparer<SoftwareOsVersion>, IComparable
    {
        public string id { get; set; }
        public string name { get; set; }
        public int order { get; set; }

        public int Compare(SoftwareOsVersion x, SoftwareOsVersion y)
        {
            return x.order.CompareTo(y.order);
        }

        public int CompareTo(object obj)
        {
            return order.CompareTo((obj as SoftwareOsVersion).order);
        }

        public override bool Equals(object obj)
        {
            return obj is SoftwareOsVersion version &&
                   id == version.id;
        }

        public override int GetHashCode()
        {
            return 1877310944 + EqualityComparer<string>.Default.GetHashCode(id);
        }
    }
}
