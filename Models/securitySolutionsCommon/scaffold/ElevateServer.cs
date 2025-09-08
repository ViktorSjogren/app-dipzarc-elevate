using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

public partial class ElevateServer
{
    public int ElevateServersId { get; set; }

    public int CustomerId { get; set; }

    public string Tier { get; set; } = null!;

    public string ServerName { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
