using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAdRoleMembershipView
{
    public int RoleMembershipId { get; set; }

    public string RoleValue { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public DateTime? MemberSince { get; set; }

    public string AddedBy { get; set; } = null!;

    public DateTime? LastModified { get; set; }
}
