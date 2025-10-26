namespace FlightBooking.Configuration
{
/*    public class VNPayConfig
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string Url { get; set; }
        public string ReturnUrl { get; set; }
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
    }*/

    public class MoMoConfig
    {
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Endpoint { get; set; }
        public string ReturnUrl { get; set; }
        public string IpnUrl { get; set; }
        public string RequestType { get; set; } = "captureWallet";
    }

    public class ZaloPayConfig
    {
        public string AppId { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string Endpoint { get; set; }
        public string ReturnUrl { get; set; }
        public string CallbackUrl { get; set; }
    }
}
