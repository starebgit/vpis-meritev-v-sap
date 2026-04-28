using System;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    public static class SapConnectionSmokeTester
    {
        public static SapTrialResult RunTrial()
        {
            try
            {
                var destination = RfcDestinationManager.GetDestination("SAP_DEFAULT");
                destination.Ping();

                return new SapTrialResult
                {
                    Success = true,
                    Message = "SAP ping ok. Connector DLLs loaded and destination reachable.",
                    TimestampUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new SapTrialResult
                {
                    Success = false,
                    Message = ex.Message,
                    TimestampUtc = DateTime.UtcNow
                };
            }
        }
    }

    public class SapTrialResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
