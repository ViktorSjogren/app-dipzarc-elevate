using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAdmin
{
    public int ElevateAdminsId { get; set; }

    public int CustomerId { get; set; }

    public string AdminRole { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? Updated { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
