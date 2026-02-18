namespace dizparc_elevate.Services
{
    /// <summary>
    /// Azure Automation runbook names. Must match the runbook names in the Automation Account exactly.
    /// </summary>
    public static class RunbookNames
    {
        public static readonly string UnlockActiveDirectoryAccount =
            Environment.GetEnvironmentVariable("Runbook_Unlock_ActiveDirectoryAccount") ?? "Runbook_Unlock_ActiveDirectoryAccount";
        public static readonly string CreateDeviceGroup =
            Environment.GetEnvironmentVariable("Runbook_Create_DeviceGroup") ?? "Runbook_Create_DeviceGroup";
        public static readonly string CreateElevateAccount =
            Environment.GetEnvironmentVariable("Runbook_Create_ElevateAccount") ?? "Runbook_Create_ElevateAccount";
    }
}
