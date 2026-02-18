using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateUserAdminsView
{
    public int ElevateUsersId { get; set; }

    public string UserName { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? AdminRole { get; set; }

    public DateTime? AdminRoleAssigned { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? UserCreated { get; set; }

    public DateTime? LastUpdated { get; set; }
}
