using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssWhitelist
{
    public int DssWhitelistId { get; set; }

    public int CustomerId { get; set; }

    public string Comment { get; set; } = null!;

    public string? EntityName { get; set; }

    public string EntityType { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;
}
