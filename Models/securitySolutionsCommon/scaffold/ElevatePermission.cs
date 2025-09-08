using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

public partial class ElevatePermission
{
    public int ElevatePermissionsId { get; set; }

    public int ElevateUserId { get; set; }

    public string Type { get; set; } = null!;

    public string Value { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual ElevateUser ElevateUser { get; set; } = null!;
}
