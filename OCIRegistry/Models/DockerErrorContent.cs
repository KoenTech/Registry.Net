using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace OCIRegistry.Models
{
    public class DockerErrorContent
    {
        public List<Error> Errors { get; set; } = new();


        public static DockerErrorContent FromError(string code, string message)
        {
            return new DockerErrorContent()
            {
                Errors = new List<Error>() { new Error() { Code = code, Message = message } }
            };
        }
    }

    public static class DockerErrorResponse
    {
        public static readonly ObjectResult BlobUnknown = new ObjectResult(ErrorCodes.BLOB_UNKNOWN) { StatusCode = 404 };
        public static readonly ObjectResult BlobUploadInvalid = new ObjectResult(ErrorCodes.BLOB_UPLOAD_INVALID) { StatusCode = 400 };
        public static readonly ObjectResult BlobUploadUnknown = new ObjectResult(ErrorCodes.BLOB_UPLOAD_UNKNOWN) { StatusCode = 404 };
        public static readonly ObjectResult DigestInvalid = new ObjectResult(ErrorCodes.DIGEST_INVALID) { StatusCode = 400 };
        public static readonly ObjectResult ManifestBlobUnknown = new ObjectResult(ErrorCodes.MANIFEST_BLOB_UNKNOWN) { StatusCode = 404 };
        public static readonly ObjectResult ManifestInvalid = new ObjectResult(ErrorCodes.MANIFEST_INVALID) { StatusCode = 400 };
        public static readonly ObjectResult ManifestUnknown = new ObjectResult(ErrorCodes.MANIFEST_UNKNOWN) { StatusCode = 404 };
        public static readonly ObjectResult NameInvalid = new ObjectResult(ErrorCodes.NAME_INVALID) { StatusCode = 400 };
        public static readonly ObjectResult NameUnknown = new ObjectResult(ErrorCodes.NAME_UNKNOWN) { StatusCode = 404 };
        public static readonly ObjectResult SizeInvalid = new ObjectResult(ErrorCodes.SIZE_INVALID) { StatusCode = 400 };
        public static readonly ObjectResult Unauthorized = new ObjectResult(ErrorCodes.UNAUTHORIZED) { StatusCode = 401 };
        public static readonly ObjectResult Denied = new ObjectResult(ErrorCodes.DENIED) { StatusCode = 403 };
        public static readonly ObjectResult Unsupported = new ObjectResult(ErrorCodes.UNSUPPORTED) { StatusCode = 400 };
        public static readonly ObjectResult TooManyRequests = new ObjectResult(ErrorCodes.TOOMANYREQUESTS) { StatusCode = 429 };
    }

    public class Error
    {
        public required string Code { get; set; }
        public required string Message { get; set; }
        public string? Detail { get; set; }
    }

    public class ErrorCodes
    {
        public static readonly DockerErrorContent BLOB_UNKNOWN = DockerErrorContent.FromError("BLOB_UNKNOWN", "blob unknown to registry");
        public static readonly DockerErrorContent BLOB_UPLOAD_INVALID = DockerErrorContent.FromError("BLOB_UPLOAD_INVALID", "blob upload invalid");
        public static readonly DockerErrorContent BLOB_UPLOAD_UNKNOWN = DockerErrorContent.FromError("BLOB_UPLOAD_UNKNOWN", "blob upload unknown to registry");
        public static readonly DockerErrorContent DIGEST_INVALID = DockerErrorContent.FromError("DIGEST_INVALID", "provided digest did not match uploaded content");
        public static readonly DockerErrorContent MANIFEST_BLOB_UNKNOWN = DockerErrorContent.FromError("MANIFEST_BLOB_UNKNOWN", "manifest references a manifest or blob unknown to registry");
        public static readonly DockerErrorContent MANIFEST_INVALID = DockerErrorContent.FromError("MANIFEST_INVALID", "manifest invalid");
        public static readonly DockerErrorContent MANIFEST_UNKNOWN = DockerErrorContent.FromError("MANIFEST_UNKNOWN", "manifest unknown to registry");
        public static readonly DockerErrorContent NAME_INVALID = DockerErrorContent.FromError("NAME_INVALID", "invalid repository name");
        public static readonly DockerErrorContent NAME_UNKNOWN = DockerErrorContent.FromError("NAME_UNKNOWN", "repository name not known to registry");
        public static readonly DockerErrorContent SIZE_INVALID = DockerErrorContent.FromError("SIZE_INVALID", "provided length did not match content length");
        public static readonly DockerErrorContent UNAUTHORIZED = DockerErrorContent.FromError("UNAUTHORIZED", "authentication required");
        public static readonly DockerErrorContent DENIED = DockerErrorContent.FromError("DENIED", "requested access to the resource is denied");
        public static readonly DockerErrorContent UNSUPPORTED = DockerErrorContent.FromError("UNSUPPORTED", "the operation is unsupported");
        public static readonly DockerErrorContent TOOMANYREQUESTS = DockerErrorContent.FromError("TOOMANYREQUESTS", "too many requests");
    }
}
