using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Restaurant.Desktop.Core
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(AppSettings.Instance.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void ApplyToken()
        {
            var token = SessionManager.Instance.Token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SessionManager.Instance.ClearSession();
                return ApiResult<T>.Failure("انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى.", 401);
            }

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>();
                    if (data != null)
                    {
                        return ApiResult<T>.Success(data, (int)response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    return ApiResult<T>.Failure($"خطأ في معالجة البيانات: {ex.Message}", (int)response.StatusCode);
                }
            }

            // Attempt to read error message from body
            string errorContent = "";
            try
            {
                errorContent = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(errorContent))
                {
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        errorContent = msgProp.GetString() ?? errorContent;
                    }
                }
            }
            catch
            {
                // ignore and use raw/default
            }

            if (string.IsNullOrWhiteSpace(errorContent))
            {
                errorContent = $"فشل الطلب مع كود: {response.StatusCode}";
            }

            return ApiResult<T>.Failure(errorContent, (int)response.StatusCode);
        }

        private async Task<ApiResult<bool>> HandleNoContentResponseAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SessionManager.Instance.ClearSession();
                return ApiResult<bool>.Failure("انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى.", 401);
            }

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Success(true, (int)response.StatusCode);
            }

            string errorContent = "";
            try
            {
                errorContent = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(errorContent))
                {
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        errorContent = msgProp.GetString() ?? errorContent;
                    }
                }
            }
            catch
            {
                // ignore
            }

            if (string.IsNullOrWhiteSpace(errorContent))
            {
                errorContent = $"فشل الطلب مع كود: {response.StatusCode}";
            }

            return ApiResult<bool>.Failure(errorContent, (int)response.StatusCode);
        }

        public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                ApplyToken();
                var response = await _httpClient.GetAsync(endpoint);
                return await HandleResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Failure($"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
        {
            try
            {
                ApplyToken();
                var response = await _httpClient.PostAsJsonAsync(endpoint, body);
                return await HandleResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResult<TResponse>.Failure($"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> PutAsync<TRequest>(string endpoint, TRequest body)
        {
            try
            {
                ApplyToken();
                var response = await _httpClient.PutAsJsonAsync(endpoint, body);
                return await HandleNoContentResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> DeleteAsync(string endpoint)
        {
            try
            {
                ApplyToken();
                var response = await _httpClient.DeleteAsync(endpoint);
                return await HandleNoContentResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> PostNoContentAsync<TRequest>(string endpoint, TRequest body)
        {
            try
            {
                ApplyToken();
                var response = await _httpClient.PostAsJsonAsync(endpoint, body);
                return await HandleNoContentResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<ApiResult<TResponse>> PostFileAsync<TResponse>(string endpoint, string filePath)
        {
            try
            {
                ApplyToken();
                using (var content = new MultipartFormDataContent())
                {
                    var fileStream = System.IO.File.OpenRead(filePath);
                    var fileContent = new StreamContent(fileStream);
                    
                    // Determine content type based on extension
                    string ext = System.IO.Path.GetExtension(filePath).ToLower();
                    string mimeType = ext == ".png" ? "image/png" : "image/jpeg";
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                    
                    var fileName = System.IO.Path.GetFileName(filePath);
                    content.Add(fileContent, "file", fileName);

                    var response = await _httpClient.PostAsync(endpoint, content);
                    return await HandleResponseAsync<TResponse>(response);
                }
            }
            catch (Exception ex)
            {
                return ApiResult<TResponse>.Failure($"خطأ في رفع الملف: {ex.Message}");
            }
        }
    }

    public class ImageUploadResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
