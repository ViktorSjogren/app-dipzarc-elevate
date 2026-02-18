using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual ICollection<CustomersDatum> CustomersData { get; set; } = new List<CustomersDatum>();

    public virtual ICollection<DssAutomationRule> DssAutomationRules { get; set; } = new List<DssAutomationRule>();

    public virtual ICollection<DssDatum> DssData { get; set; } = new List<DssDatum>();

    public virtual ICollection<ElevateActivePermission> ElevateActivePermissions { get; set; } = new List<ElevateActivePermission>();

    public virtual ICollection<ElevateAdServer> ElevateAdServers { get; set; } = new List<ElevateAdServer>();

    public virtual ICollection<ElevateAdmin> ElevateAdmins { get; set; } = new List<ElevateAdmin>();

    public virtual ICollection<ElevateAvailableTier> ElevateAvailableTiers { get; set; } = new List<ElevateAvailableTier>();

    public virtual ICollection<ElevateAvdServer> ElevateAvdServers { get; set; } = new List<ElevateAvdServer>();

    public virtual ICollection<ElevateJob> ElevateJobs { get; set; } = new List<ElevateJob>();

    public virtual ICollection<ElevatePermission> ElevatePermissions { get; set; } = new List<ElevatePermission>();

    public virtual ICollection<ElevateUser> ElevateUsers { get; set; } = new List<ElevateUser>();
}
