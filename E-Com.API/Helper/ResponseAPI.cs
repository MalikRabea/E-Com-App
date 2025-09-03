namespace E_Com.API.Helper
{
    public class ResponseAPI
    {
        public ResponseAPI(int statusCode, string message=null)
        {
            StatusCode = statusCode;
            Message = message?? GetMessageFormStatusCode(StatusCode);
        }

        private string GetMessageFormStatusCode(int satatuscode)
        {
            return satatuscode switch
            {
                200 => "Success",
                201 => "Created",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found res",
                500 => "Internal Server Error",
                _ => "Unknown Status Code",
            };
        }

        public int StatusCode { get; set; }

        public string? Message { get; set; }
    }
}
