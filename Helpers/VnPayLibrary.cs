using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FlightBooking.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new();
        private readonly SortedList<string, string> _responseData = new();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            // Build percent-encoded query (RFC 3986 style via WebUtility.UrlEncode) and sign it
            var dataBuilder = new StringBuilder();
            foreach (var kv in _requestData.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    dataBuilder.Append(WebUtility.UrlEncode(kv.Key))
                               .Append("=")
                               .Append(WebUtility.UrlEncode(kv.Value))
                               .Append("&");
                }
            }

            var encodedQuery = dataBuilder.ToString();
            if (encodedQuery.Length > 0)
            {
                encodedQuery = encodedQuery.Remove(encodedQuery.Length - 1, 1);
            }

            var vnpSecureHash = HmacSHA512(vnpHashSecret, encodedQuery);
            var finalUrl = baseUrl + "?" + encodedQuery + "&vnp_SecureHash=" + vnpSecureHash + "&vnp_SecureHashType=HmacSHA512";
            return finalUrl;
        }

        private string HmacSHA512(string key, string inputData)
        {
            // Log để debug (có thể bỏ trong production)
            Console.WriteLine($"VNPay Key: {key}");
            Console.WriteLine($"VNPay Raw Data: {inputData}");

            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            var result = hash.ToString();
            Console.WriteLine($"VNPay Generated Hash: {result}");
            return result;
        }

        // Helpers/VnPayLibrary.cs - Sửa method ValidateSignature
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = HmacSHA512(secretKey, rspRaw);

            // Log để debug
            Console.WriteLine($"Input Hash: {inputHash}");
            Console.WriteLine($"Calculated Hash: {myChecksum}");
            Console.WriteLine($"Raw Data: {rspRaw}");

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            var sortedData = _responseData
                .Where(kv => !kv.Key.Equals("vnp_SecureHash", StringComparison.InvariantCultureIgnoreCase)
                          && !kv.Key.Equals("vnp_SecureHashType", StringComparison.InvariantCultureIgnoreCase)
                          && !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key);

            foreach (var kv in sortedData)
            {
                data.Append(WebUtility.UrlEncode(kv.Key))
                    .Append("=")
                    .Append(WebUtility.UrlEncode(kv.Value))
                    .Append("&");
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
    }
}