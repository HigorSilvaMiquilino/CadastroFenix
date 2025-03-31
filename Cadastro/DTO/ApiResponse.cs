namespace Cadastro.DTO
{
    public class ApiResponse
    {
        /// <summary>
        /// The HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A message describing the result of the operation.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A dictionary of validation errors, if any.
        /// </summary>
        public Dictionary<string, string>? Errors { get; set; }

        /// <summary>
        /// Additional metadata, such as rate-limiting details (e.g., retryAfter, limit, window).
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
