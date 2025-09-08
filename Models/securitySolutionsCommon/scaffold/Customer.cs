using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual ICollection<CustomersDatum> CustomersData { get; set; } = new List<CustomersDatum>();

    public virtual ICollection<DssDatum> DssData { get; set; } = new List<DssDatum>();

    public virtual ICollection<ElevateServer> ElevateServers { get; set; } = new List<ElevateServer>();

    public virtual ICollection<ElevateUser> ElevateUsers { get; set; } = new List<ElevateUser>();
}
