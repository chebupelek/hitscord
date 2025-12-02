using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace hitscord.Utils;

public class MinioSettings
{
	public string Endpoint { get; set; } = null!;
	public string AccessKey { get; set; } = null!;
	public string SecretKey { get; set; } = null!;
	public string BucketName { get; set; } = null!;
	public bool UseSSL { get; set; }
}

public class MinioService
{
	private readonly IMinioClient _minio;
	private readonly string _bucket;
	private readonly string _endpoint;
	private readonly bool _useSSL;
	private readonly ILogger<MinioService> _logger;

	public MinioService(IOptions<MinioSettings> options, ILogger<MinioService> logger)
	{
		var settings = options.Value;
		_bucket = settings.BucketName;
		_endpoint = settings.Endpoint;
		_useSSL = settings.UseSSL;

		_minio = new MinioClient()
			.WithEndpoint(settings.Endpoint)
			.WithCredentials(settings.AccessKey, settings.SecretKey)
			.WithSSL(settings.UseSSL)
			.Build();

		_logger = logger;
	}

	public async Task UploadFileAsync(string objectName, byte[] data, string contentType)
	{
		_logger.LogInformation("1 1");
		_logger.LogInformation(_bucket);
		_logger.LogInformation(_endpoint);
		_logger.LogInformation(_useSSL.ToString());
		using var stream = new MemoryStream(data);
		_logger.LogInformation("1 2");
		bool found = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
		_logger.LogInformation("1 3");
		if (!found)
		{
			_logger.LogInformation("1 4");
			await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
		}
		_logger.LogInformation("1 5");
		await _minio.PutObjectAsync(new PutObjectArgs()
			.WithBucket(_bucket)
			.WithObject(objectName)
			.WithStreamData(stream)
			.WithObjectSize(data.Length)
			.WithContentType(contentType));
	}

	public async Task<byte[]> GetFileAsync(string objectName)
	{
		using var ms = new MemoryStream();
		await _minio.GetObjectAsync(new GetObjectArgs()
			.WithBucket(_bucket)
			.WithObject(objectName)
			.WithCallbackStream(stream => stream.CopyTo(ms)));
		return ms.ToArray();
	}

	public async Task DeleteFileAsync(string objectName)
	{
		bool found = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
		if (!found)
			throw new Exception($"Bucket '{_bucket}' does not exist");

		await _minio.RemoveObjectAsync(new RemoveObjectArgs()
			.WithBucket(_bucket)
			.WithObject(objectName));
	}

	public async Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds = 3600)
	{
		var args = new PresignedGetObjectArgs()
			.WithBucket(_bucket)
			.WithObject(objectName)
			.WithExpiry(expirySeconds);

		return await _minio.PresignedGetObjectAsync(args);
	}

	public string GetFileUrl(string objectName)
	{
		var protocol = _useSSL ? "https" : "http";
		return $"{protocol}://{_endpoint}/{_bucket}/{objectName}";
	}

	public async Task StatFileAsync(string objectName)
	{
		await _minio.StatObjectAsync(new StatObjectArgs()
			.WithBucket(_bucket)
			.WithObject(objectName));
	}
}
