using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class TestCustomerAssignment
{
    public Guid Id { get; set; }

    public Guid TestId { get; set; }

    public Guid CustomerId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public Guid AssignedBy { get; set; }

    public virtual Employee AssignedByNavigation { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
