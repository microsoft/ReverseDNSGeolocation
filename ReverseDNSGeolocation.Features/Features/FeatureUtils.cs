namespace ReverseDNSGeolocation.Features
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class FeatureUtils
    {
        public static int EstimateObjectSizeInBytes(object targetObj)
        {
            if (targetObj == null)
            {
                return 0;
            }

            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            byte[] Array;
            bf.Serialize(ms, targetObj);
            Array = ms.ToArray();
            return Array.Length;
        }
    }
}
