using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateUser
{
    public int ElevateUsersId { get; set; }

    public int CustomerId { get; set; }

    public string UserName { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? Updated { get; set; }

    public string? UpdatedBy { get; set; }

    public string Status { get; set; } = null!;

    public int Tier { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ElevateAssignedPermission> ElevateAssignedPermissions { get; set; } = new List<ElevateAssignedPermission>();

    public virtual ElevateAvailableTier TierNavigation { get; set; } = null!;
}
