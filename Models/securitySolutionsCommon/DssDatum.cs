using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssDatum
{
    public int DssDataId { get; set; }

    public int CustomerId { get; set; }

    public string Plan { get; set; } = null!;

    public string SubscriptionId { get; set; } = null!;

    public string BreakGlassAccount { get; set; } = null!;

    public byte FidoKey { get; set; }

    public string Abbreviation { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public bool DssServer { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
