using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssWhitelistView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string SubscriptionId { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public string? EntityName { get; set; }

    public string EntityType { get; set; } = null!;

    public string Comment { get; set; } = null!;
}
