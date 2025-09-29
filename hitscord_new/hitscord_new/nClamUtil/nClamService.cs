using hitscord.Models.other;
using Microsoft.Extensions.Options;
using nClam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace hitscord.nClamUtil;

public class nClamService
{
	private readonly ClamClient _clamClient;

	public nClamService(IOptions<ClamAVOptions> options)
	{
		var clamOptions = options.Value;
		_clamClient = new ClamClient(clamOptions.Host, clamOptions.Port);
	}

	public async Task<ClamScanResult> ScanFileAsync(byte[] fileBytes)
	{
		using var ms = new MemoryStream(fileBytes);
		var scanResult = await _clamClient.SendAndScanFileAsync(ms);
		return scanResult;
	}

	public async Task<ClamScanResult> ScanFileAsync(string filePath)
	{
		var scanResult = await _clamClient.SendAndScanFileAsync(filePath);
		return scanResult;
	}
}