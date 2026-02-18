using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAvdServer
{
    public int ElevateAvdServersId { get; set; }

    public int CustomerId { get; set; }

    public byte Active { get; set; }

    public string ServerName { get; set; } = null!;

    public string? IpAddress { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public int? Tier { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ElevateAvailableTier? TierNavigation { get; set; }
}
