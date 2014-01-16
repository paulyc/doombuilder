﻿using System.IO;
using SharpCompress.Compressor;
using SharpCompress.Compressor.BZip2;

namespace CodeImp.DoomBuilder.GZBuilder.Data
{
	internal static class SharpCompressHelper
	{
		internal static MemoryStream CompressStream(Stream stream) {
			byte[] arr = new byte[stream.Length];
			stream.Read(arr, 0, (int)stream.Length);

			MemoryStream ms = new MemoryStream();
			BZip2Stream bzip = new BZip2Stream(ms, CompressionMode.Compress, true);

			bzip.Write(arr, 0, arr.Length);
			bzip.Close();

			return ms;
		}

		internal static MemoryStream DecompressStream(Stream stream) {
			BZip2Stream bzip = new BZip2Stream(stream, CompressionMode.Decompress, false);

			byte[] buffer = new byte[16 * 1024];
			MemoryStream ms = new MemoryStream();

			int read;
			while ((read = bzip.Read(buffer, 0, buffer.Length)) > 0)
				ms.Write(buffer, 0, read);

			return ms;
		}
	}
}