using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class CustomersView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;
}
