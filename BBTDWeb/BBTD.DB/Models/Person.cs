using System;
using System.Collections.Generic;

namespace BBTD.DB.Models;

public partial class Person
{
    public int Id { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Description { get; set; }
}
