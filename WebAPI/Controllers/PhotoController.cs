namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using WebAPI.Data;
    using WebAPI.Models;
    

    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                // Конфигурация доступа
                string tenantId = "c1276381-16fc-4751-aef2-30c63a746cfa"; 
                string accessKey = "9bd8e2c5d87e29647cb941d4a38f679c";
                string secretKey = "9f92f1a0932ae5fe65799f256687ae32";
                string region = "ru-central-1";
                string service = "s3";
                string bucketName = "bucket-8845e1";
                string host = "s3.cloud.ru";

                // Создание экземпляра S3RequestSigner
                var signer = new S3RequestSigner(accessKey, secretKey, region, service);

                // Получение текущего времени в формате ISO 8601
                string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
                string date = DateTime.UtcNow.ToString("yyyyMMdd");

                // Генерация подписывающего ключа
                var signingKey = signer.GenerateSigningKey(date);

                // Чтение содержимого файла в byte[]
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.OpenReadStream().CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Вычисляем хэш содержимого файла
                string payloadHash = S3RequestSigner.ToHexString(S3RequestSigner.SHA256Hash(fileBytes));

                // Создание канонического запроса
                string method = "PUT"; 
                string uri = $"/{bucketName}/{file.FileName}";
                string canonicalRequest = signer.CreateCanonicalRequest(
                    method,
                    uri,
                    host,
                    "", // Пустой query string
                    payloadHash,
                    timestamp
                );

                // Создание строки для подписи
                string credentialScope = $"{date}/{region}/{service}/aws4_request";
                string stringToSign = signer.CreateStringToSign(timestamp, credentialScope, canonicalRequest);

                // Подпись строки с помощью SigningKey
                string signature = signer.SignString(stringToSign, signingKey);

                // Формируем заголовки с подписью
                var headers = new Dictionary<string, string>
        {
            { "x-amz-content-sha256", payloadHash },
            { "x-amz-date", timestamp },
            { "Authorization", $"AWS4-HMAC-SHA256 Credential={tenantId}:{accessKey}/{credentialScope}, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature={signature}" }
        };

                // Создание HTTP-запроса вручную с подписанными заголовками
                using (var client = new HttpClient())
                {
                    using (var fileStream = file.OpenReadStream())
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/{bucketName}/{file.FileName}")
                        {
                            Content = new StreamContent(fileStream)
                        };

                        // Установка заголовков
                        foreach (var header in headers)
                        {
                            requestMessage.Headers.Add(header.Key, header.Value);
                        }

                        // Отправка запроса
                        var response = await client.SendAsync(requestMessage);
                        if (response.IsSuccessStatusCode)
                        {
                            return Ok("File uploaded successfully.");
                        }
                        else
                        {
                            return StatusCode((int)response.StatusCode, $"Error uploading file: {response.ReasonPhrase}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
