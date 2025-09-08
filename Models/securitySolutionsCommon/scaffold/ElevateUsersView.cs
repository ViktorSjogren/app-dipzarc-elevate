using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

public partial class ElevateUsersView
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string ElevateAccount { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;
}
