using System;
using System.Diagnostics;
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
                using var mc = new ManagementClass("win32_processor");
                var moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    string? id = mo["processorID"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return id!;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MachineCodeHelper Error] Không thể lấy CPU ID qua WMI: {ex.Message}");
            }

            return "unknownCPU";
        }
    }
}
