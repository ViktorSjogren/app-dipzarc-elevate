using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class CustomersDssView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? TenantId { get; set; }

    public string Plan { get; set; } = null!;

    public string SubscriptionId { get; set; } = null!;

    public string BreakGlassAccount { get; set; } = null!;

    public byte FidoKey { get; set; }

    public string Abbreviation { get; set; } = null!;

    public string? Domains { get; set; }
}
