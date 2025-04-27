using System;
using System.Collections.Generic;

namespace ElstromVPKS.Models;

public partial class Employee
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string HashedPassword { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<DocumentAction> DocumentActions { get; set; } = new List<DocumentAction>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<TestAssignment> TestAssignments { get; set; } = new List<TestAssignment>();

    public virtual ICollection<TestCustomerAssignment> TestCustomerAssignments { get; set; } = new List<TestCustomerAssignment>();

    public virtual ICollection<TestLog> TestLogs { get; set; } = new List<TestLog>();
}
