using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateUserPermissionsView
{
    public int ElevateUsersId { get; set; }

    public string UserName { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string PermissionType { get; set; } = null!;

    public string PermissionValue { get; set; } = null!;

    public string TierLevel { get; set; } = null!;

    public DateTime? PermissionGranted { get; set; }

    public string GrantedBy { get; set; } = null!;

    public DateTime? LastModified { get; set; }

    public string ModifiedBy { get; set; } = null!;
}
