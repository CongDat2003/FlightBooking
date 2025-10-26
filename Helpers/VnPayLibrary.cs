/*using System.Net;
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
            var data = new StringBuilder();

            // Sắp xếp theo thứ tự alphabet của key (quan trọng!)
            foreach (var kv in _requestData.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(kv.Key + "=" + kv.Value + "&");
                }
            }

            // Tạo query string cho URL
            var queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1); // Remove last &
            }

            // Tạo chữ ký từ raw data (không encode URL)
            var signData = queryString;
            var vnpSecureHash = HmacSHA512(vnpHashSecret, signData);

            // Tạo URL cuối cùng với encoding
            var finalUrl = baseUrl + "?";
            foreach (var kv in _requestData.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    finalUrl += WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&";
                }
            }

            finalUrl += "vnp_SecureHash=" + vnpSecureHash;

            return finalUrl;
        }

        private string HmacSHA512(string key, string inputData)
        {
            Console.WriteLine($"Key: {key}");
            Console.WriteLine($"Data: {inputData}");

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
            Console.WriteLine($"Hash: {result}");
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

            // Sắp xếp theo alphabet và loại bỏ vnp_SecureHash, vnp_SecureHashType
            var sortedData = _responseData
                .Where(kv => !kv.Key.Equals("vnp_SecureHash", StringComparison.InvariantCultureIgnoreCase)
                          && !kv.Key.Equals("vnp_SecureHashType", StringComparison.InvariantCultureIgnoreCase)
                          && !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key);

            foreach (var kv in sortedData)
            {
                data.Append(kv.Key + "=" + kv.Value + "&");
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1); // Remove last &
            }

            return data.ToString();
        }
    }
}
*/