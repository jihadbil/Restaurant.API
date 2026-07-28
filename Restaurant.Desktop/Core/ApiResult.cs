namespace Restaurant.Desktop.Core
{
    public class ApiResult<T>
    {
        public T? Data { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }

        public static ApiResult<T> Success(T data, int statusCode = 200)
        {
            return new ApiResult<T>
            {
                Data = data,
                IsSuccess = true,
                StatusCode = statusCode
            };
        }

        public static ApiResult<T> Failure(string errorMessage, int statusCode = 400)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }
    }
}
