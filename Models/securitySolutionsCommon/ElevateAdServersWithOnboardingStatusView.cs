using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAdServersWithOnboardingStatusView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string ServerName { get; set; } = null!;

    public byte Active { get; set; }

    public byte OnboardingStatus { get; set; }

    public string? Tier { get; set; }
}
