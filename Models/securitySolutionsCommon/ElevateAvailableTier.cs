using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAvailableTier
{
    public int ElevateAvailableTiersId { get; set; }

    public int CustomerId { get; set; }

    public byte Active { get; set; }

    public string TierName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string EntraGroupId { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ElevateAvdServer> ElevateAvdServers { get; set; } = new List<ElevateAvdServer>();

    public virtual ICollection<ElevatePermission> ElevatePermissions { get; set; } = new List<ElevatePermission>();

    public virtual ICollection<ElevateUser> ElevateUsers { get; set; } = new List<ElevateUser>();
}
