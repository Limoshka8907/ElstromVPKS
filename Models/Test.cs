using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class Test
{
    public Guid Id { get; set; }

    public string TestName { get; set; } = null!;

    public string TestType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Parametrs { get; set; }

    public virtual ICollection<TestAssignment> TestAssignments { get; set; } = new List<TestAssignment>();

    public virtual ICollection<TestCustomerAssignment> TestCustomerAssignments { get; set; } = new List<TestCustomerAssignment>();

    public virtual ICollection<TestLog> TestLogs { get; set; } = new List<TestLog>();

    public virtual ICollection<TestView> TestViews { get; set; } = new List<TestView>();
}
