using Microsoft.AspNetCore.Http;

namespace SimpleCmsApi.Models;

public class FileChunkBaseDto
{
    /// <summary>Parent folder</summary>
    public Guid ParentId { get; set; }

    /// <summary>Unique identifier for the upload</summary>
    public Guid FileId { get; set; }

    /// <summary>The file name</summary>
    public string Name { get; set; } = string.Empty;
}

public class FileChunkListDto : FileChunkBaseDto
{
    /// <summary>List of block IDs makeing up this upload</summary>
    public List<string> BlockIds { get; set; } = [];

    /// <summary>MIME Content type reported by the browser (can not be trusted)</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of the file</summary>
    public long Size { get; set; }

    /// <summary>Description of the uploaded file</summary>
    public string Description { get; set; } = string.Empty;
}

public class FileChunkDto : FileChunkBaseDto
{
    /// <summary>Unique identifier for the block being uploaded</summary>
    public string BlockId { get; set; } = string.Empty;

    /// <summary>The chunk data</summary>
    public Stream Data { get; set; } = null!;
}