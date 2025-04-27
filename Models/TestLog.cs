using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class TestLog
{
    public Guid Id { get; set; }

    public Guid TestId { get; set; }

    public Guid UserId { get; set; }

    public string ActionDescription { get; set; } = null!;

    public DateTime? ActionTime { get; set; }

    public virtual Test Test { get; set; } = null!;

    public virtual Employee User { get; set; } = null!;
}
