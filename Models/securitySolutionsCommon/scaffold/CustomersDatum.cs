using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

public partial class CustomersDatum
{
    public int CustomerDataId { get; set; }

    public int CustomerId { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string? Domains { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
