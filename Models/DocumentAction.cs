using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class DocumentAction
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }

    public string UserType { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public DateTime? ActionTime { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual Employee User { get; set; } = null!;
}
