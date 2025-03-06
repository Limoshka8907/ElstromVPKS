using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class DocumentVersion
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Version { get; set; }

    public string FilePath { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;
}
