using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateServersView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string ServerName { get; set; } = null!;

    public string Tier { get; set; } = null!;
}
