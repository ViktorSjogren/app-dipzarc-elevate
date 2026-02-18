using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class ElevateJob
{
    public int ElevateJobsId { get; set; }

    public int CustomerId { get; set; }

    public string Type { get; set; } = null!;

    public string Reference { get; set; } = null!;

    public string Job { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
