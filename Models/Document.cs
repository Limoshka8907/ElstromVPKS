using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class Document
{
    public Guid Id { get; set; }

    public string DocumentName { get; set; } = null!;

    public int? DocumentTypeId { get; set; }

    public int Version { get; set; }

    public string FilePath { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<DocumentAction> DocumentActions { get; set; } = new List<DocumentAction>();

    public virtual DocumentType? DocumentType { get; set; }

    public virtual ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();

    public virtual Test Owner { get; set; } = null!;
}
