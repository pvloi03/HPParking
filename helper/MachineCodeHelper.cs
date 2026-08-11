using System.Management;

namespace HPParking.Helper
{
    public static class MachineCodeHelper
    {
        public static string GetMachineCode()
        {
            string cpuId = GetCpuId();

            return $"{cpuId}";
        }

        private static string GetCpuId()
        {
            try
            {
                using (var mc = new ManagementClass("win32_processor"))
                {
                    var moc = mc.GetInstances();
                    foreach (var mo in moc)
                    {
                        return mo["processorID"]?.ToString();
                    }
                }
            }
            catch { }
            return "unknownCPU";
        }
    }
}
