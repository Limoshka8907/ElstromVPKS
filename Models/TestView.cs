using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class TestView
{
    public Guid Id { get; set; }

    public Guid TestId { get; set; }

    public Guid CustomerId { get; set; }

    public DateTime? ViewedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
