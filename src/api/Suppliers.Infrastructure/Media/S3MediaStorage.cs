using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Suppliers.Application.Abstractions;

namespace Suppliers.Infrastructure.Media;

/// <summary>Bound from "Media:S3". Works with AWS S3 and any S3-compatible store (the local compose stack uses Versity S3 Gateway).</summary>
public sealed class S3MediaOptions
{
    public const string SectionName = "Media:S3";

    public string BucketName { get; init; } = "supplier-media";

    public string Region { get; init; } = "us-east-1";

    /// <summary>Endpoint the API calls, e.g. "http://s3:7070". Leave empty for AWS S3.</summary>
    public string? ServiceUrl { get; init; }

    /// <summary>Endpoint browsers use, when it differs (inside Docker the API reaches the store by its service name). Defaults to <see cref="ServiceUrl"/>.</summary>
    public string? PublicServiceUrl { get; init; }

    /// <summary>Leave both empty on AWS to use the default credential chain (environment, profile or IAM role).</summary>
    public string? AccessKey { get; init; }

    public string? SecretKey { get; init; }

    /// <summary>Local development convenience; production buckets are created by infrastructure code.</summary>
    public bool CreateBucketIfMissing { get; init; }
}

/// <summary>Stores media in an S3 bucket. The API only signs URLs and inspects objects; browsers move the bytes.</summary>
internal sealed class S3MediaStorage : IMediaStorage, IDisposable
{
    private readonly S3MediaOptions _options;
    private readonly IAmazonS3 _client;
    private readonly IAmazonS3 _signingClient;
    private readonly Protocol _publicProtocol;

    public S3MediaStorage(IOptions<S3MediaOptions> options)
    {
        _options = options.Value;
        _client = CreateClient(_options.ServiceUrl);

        var publicUrl = string.IsNullOrWhiteSpace(_options.PublicServiceUrl) ? _options.ServiceUrl : _options.PublicServiceUrl;
        // Signing is a local computation, but the signature covers the host, so it must use the address browsers see.
        _signingClient = publicUrl == _options.ServiceUrl ? _client : CreateClient(publicUrl);
        _publicProtocol = publicUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTP : Protocol.HTTPS;
    }

    private AmazonS3Client CreateClient(string? serviceUrl)
    {
        var config = new AmazonS3Config
        {
            // Only send checksums where S3 requires them; S3-compatible stores differ in what they accept.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region);
        }
        else
        {
            config.ServiceURL = serviceUrl;
            config.AuthenticationRegion = _options.Region;
            config.ForcePathStyle = true; // Most S3-compatible stores use path-style URLs.
        }

        return string.IsNullOrWhiteSpace(_options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);
    }

    public PresignedUpload CreateUploadUrl(string key, string contentType, TimeSpan lifetime)
    {
        var expires = DateTime.UtcNow.Add(lifetime);
        var url = _signingClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expires,
            Protocol = _publicProtocol,
        });

        return new PresignedUpload(
            new Uri(url),
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            new DateTimeOffset(expires, TimeSpan.Zero));
    }

    public Uri CreateDownloadUrl(string key, TimeSpan lifetime) =>
        new(_signingClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime),
            Protocol = _publicProtocol,
        }));

    public async Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _client.GetObjectMetadataAsync(_options.BucketName, key, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<byte[]> ReadStartAsync(string key, int length, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                ByteRange = new ByteRange(0, length - 1),
            }, cancellationToken);

            var buffer = new byte[length];
            var read = await response.ResponseStream.ReadAtLeastAsync(buffer, length, throwOnEndOfStream: false, cancellationToken);
            return buffer[..read];
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            return []; // An empty object.
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);

    public async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.BucketName))
            return;

        // Set the region explicitly: with a custom endpoint the SDK can derive a bogus one from the host name.
        // S3 wants no location constraint at all for us-east-1.
        await _client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _options.BucketName,
            UseClientRegion = false,
            BucketRegionName = _options.Region == "us-east-1" ? null : _options.Region,
        }, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
        if (!ReferenceEquals(_signingClient, _client))
            _signingClient.Dispose();
    }
}
