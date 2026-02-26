namespace ForumWebsite.Models.Common
{
    /// <summary>
    /// Uniform envelope for every JSON response.
    /// Consistent shape makes it easy for API consumers and tests to handle results.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool         Success { get; set; }
        public string       Message { get; set; } = string.Empty;
        public T?           Data    { get; set; }
        public List<string> Errors  { get; set; } = new();

        // Factory helpers keep controller code concise
        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors ?? new() };
    }
}
