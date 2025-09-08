using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateUser
{
    public int ElevateUsersId { get; set; }

    public int CustomerId { get; set; }

    public string Username { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ElevatePermission> ElevatePermissions { get; set; } = new List<ElevatePermission>();
}
