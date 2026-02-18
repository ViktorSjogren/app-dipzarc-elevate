using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevatePermission
{
    public int ElevatePermissionsId { get; set; }

    public int CustomerId { get; set; }

    public int Type { get; set; }

    public string Value { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? Updated { get; set; }

    public string? UpdatedBy { get; set; }

    public byte OnboardingStatus { get; set; }

    public int Tier { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ElevateAssignedPermission> ElevateAssignedPermissions { get; set; } = new List<ElevateAssignedPermission>();

    public virtual ElevateAvailableTier TierNavigation { get; set; } = null!;

    public virtual ElevatePermissionType TypeNavigation { get; set; } = null!;
}
