using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevatePermissionType
{
    public int ElevatePermissionTypesId { get; set; }

    public string Type { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? Updated { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<ElevateActivePermission> ElevateActivePermissions { get; set; } = new List<ElevateActivePermission>();

    public virtual ICollection<ElevatePermission> ElevatePermissions { get; set; } = new List<ElevatePermission>();
}
