using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace OCIRegistry.Models
{
    public class ErrorResponse
    {
        public List<Error> Errors { get; set; } = new();


        public static ErrorResponse FromError(string code, string message)
        {
            return new ErrorResponse()
            {
                Errors = new List<Error>() { new Error() { Code = code, Message = message } }
            };
        }
    }

    public class Error
    {
        public required string Code { get; set; }
        public required string Message { get; set; }
        public string? Detail { get; set; }
    }

    public class ErrorCodes
    {
        public static readonly ErrorResponse BLOB_UNKNOWN = ErrorResponse.FromError("BLOB_UNKNOWN", "blob unknown to registry");
        public static readonly ErrorResponse BLOB_UPLOAD_INVALID = ErrorResponse.FromError("BLOB_UPLOAD_INVALID", "blob upload invalid");
        public static readonly ErrorResponse BLOB_UPLOAD_UNKNOWN = ErrorResponse.FromError("BLOB_UPLOAD_UNKNOWN", "blob upload unknown to registry");
        public static readonly ErrorResponse DIGEST_INVALID = ErrorResponse.FromError("DIGEST_INVALID", "provided digest did not match uploaded content");
        public static readonly ErrorResponse MANIFEST_BLOB_UNKNOWN = ErrorResponse.FromError("MANIFEST_BLOB_UNKNOWN", "manifest references a manifest or blob unknown to registry");
        public static readonly ErrorResponse MANIFEST_INVALID = ErrorResponse.FromError("MANIFEST_INVALID", "manifest invalid");
        public static readonly ErrorResponse MANIFEST_UNKNOWN = ErrorResponse.FromError("MANIFEST_UNKNOWN", "manifest unknown to registry");
        public static readonly ErrorResponse NAME_INVALID = ErrorResponse.FromError("NAME_INVALID", "invalid repository name");
        public static readonly ErrorResponse NAME_UNKNOWN = ErrorResponse.FromError("NAME_UNKNOWN", "repository name not known to registry");
        public static readonly ErrorResponse SIZE_INVALID = ErrorResponse.FromError("SIZE_INVALID", "provided length did not match content length");
        public static readonly ErrorResponse UNAUTHORIZED = ErrorResponse.FromError("UNAUTHORIZED", "authentication required");
        public static readonly ErrorResponse DENIED = ErrorResponse.FromError("DENIED", "requested access to the resource is denied");
        public static readonly ErrorResponse UNSUPPORTED = ErrorResponse.FromError("UNSUPPORTED", "the operation is unsupported");
        public static readonly ErrorResponse TOOMANYREQUESTS = ErrorResponse.FromError("TOOMANYREQUESTS", "too many requests");
    }
}
