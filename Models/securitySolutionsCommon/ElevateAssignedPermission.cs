using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAssignedPermission
{
    public int ElevatePermissionsId { get; set; }

    public int ElevateUserId { get; set; }

    public int PermissionId { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual ElevateUser ElevateUser { get; set; } = null!;

    public virtual ElevatePermission Permission { get; set; } = null!;
}
