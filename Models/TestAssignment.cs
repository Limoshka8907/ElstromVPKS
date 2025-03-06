using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class TestAssignment
{
    public Guid Id { get; set; }

    public Guid TestId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
