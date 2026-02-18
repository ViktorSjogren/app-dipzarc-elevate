using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateAdServer
{
    public int ElevateAdServersId { get; set; }

    public int CustomerId { get; set; }

    public byte Active { get; set; }

    public string ServerName { get; set; } = null!;

    public DateTime FirstSeen { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
