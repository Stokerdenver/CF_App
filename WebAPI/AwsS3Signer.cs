namespace WebAPI
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class S3RequestSigner
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _region;
        private readonly string _service;
        private readonly string _requestType = "aws4_request";
        private readonly string _algorithm = "AWS4-HMAC-SHA256";

        public S3RequestSigner(string accessKey, string secretKey, string region, string service)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;
            _region = region;
            _service = service;
        }

        // 1. Генерация подписывающего ключа
        public byte[] GenerateSigningKey(string date)
        {
            byte[] dateKey = HmacSHA256("AWS4" + _secretKey, date);
            byte[] dateRegionKey = HmacSHA256(dateKey, _region);
            byte[] dateRegionServiceKey = HmacSHA256(dateRegionKey, _service);
            return HmacSHA256(dateRegionServiceKey, _requestType);
        }

        // 2. Создание канонического запроса
        public string CreateCanonicalRequest(string method, string uri, string host, string queryString, string payloadHash, string timestamp)
        {
            string canonicalHeaders = $"host:{host}\n" +
                                      $"x-amz-content-sha256:{payloadHash}\n" +
                                      $"x-amz-date:{timestamp}\n";

            string signedHeaders = "host;x-amz-content-sha256;x-amz-date";

            // Формируем канонический запрос по шагам:
            return $"{method}\n{uri}\n{queryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        }

        // 3. Генерация строки для подписи
        public string CreateStringToSign(string timestamp, string credentialScope, string canonicalRequest)
        {
            string canonicalRequestHash = ToHexString(SHA256Hash(Encoding.UTF8.GetBytes(canonicalRequest)));

            return $"{_algorithm}\n{timestamp}\n{credentialScope}\n{canonicalRequestHash}";
        }

        // 4. Подпись строки ключом
        public string SignString(string stringToSign, byte[] signingKey)
        {
            byte[] signature = HmacSHA256(signingKey, stringToSign);
            return ToHexString(signature);
        }

        // Вспомогательный метод для HMAC-SHA256
        public static byte[] HmacSHA256(byte[] key, string data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            }
        }

        public static byte[] HmacSHA256(string key, string data)
        {
            return HmacSHA256(Encoding.UTF8.GetBytes(key), data);
        }

        // Метод для SHA256-хэша
        public static byte[] SHA256Hash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        // Вспомогательный метод для перевода в шестнадцатеричную строку
        public static string ToHexString(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }


}
