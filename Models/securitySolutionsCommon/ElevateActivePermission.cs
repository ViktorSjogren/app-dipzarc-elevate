using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateActivePermission
{
    public int ElevateActivePermissionsId { get; set; }

    public int CustomerId { get; set; }

    public bool Active { get; set; }

    public string UserName { get; set; } = null!;

    public int PermissionType { get; set; }

    public string Permission { get; set; } = null!;

    public bool ManuallyAssigned { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ElevatePermissionType PermissionTypeNavigation { get; set; } = null!;
}
