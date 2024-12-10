using LibNexus.Core.Extensions;
using System.IO;

namespace LibNexus.Files.PackFiles;

public class PackVirtualPage
{
	public const uint Stride = 16;

	private readonly Stream _stream;

	private readonly PackHeader _header;

	public ulong Index { get; }

	private ulong _offset;
	private ulong _length;

	public ulong Offset
	{
		get => _offset;

		set
		{
			if (_offset == value)
				return;

			if (_offset % 16 != 0)
			{
				throw new System.Exception("Virtual page offset not at multiple of 16");
			}

			_offset = value;

			_stream.Position = (long)(Position + 0);
			_stream.WriteUInt64(value);
		}
	}

	public ulong Length
	{
		get => _length;

		set
		{
			if (_length == value)
				return;

            if (value % 16 != 0)
            {
                throw new System.Exception("Virtual page length not at multiple of 16");
            }

            _length = value;

			_stream.Position = (long)(Position + 8);
			_stream.WriteUInt64(value);
		}
	}

	public PackPhysicalPage PhysicalPage { get; set; }

	private ulong Position => _header.VirtualPagesOffset + Index * Stride;

	public PackVirtualPage(Stream stream, PackHeader header, ulong index)
	{
		_stream = stream;
		_header = header;
		Index = index;

		_offset = _stream.ReadUInt64();
		_length = _stream.ReadUInt64();
	}

	public static PackVirtualPage Create(Stream stream, PackHeader header, ulong index)
	{
		stream.WriteUInt64(0);
		stream.WriteUInt64(0);

		stream.Position -= 16;

		return new PackVirtualPage(stream, header, index);
	}
}
