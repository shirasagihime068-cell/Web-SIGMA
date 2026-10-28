namespace PTK.SIGMA.Domain.SigmaService;

public sealed class StoredFileDto
{
    public byte[] Content { get; init; } = [];
    public string ContentType { get; init; } = "application/octet-stream";
}
