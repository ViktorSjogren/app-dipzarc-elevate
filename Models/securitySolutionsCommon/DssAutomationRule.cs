using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssAutomationRule
{
    public int DssAutomationRuleId { get; set; }

    public int? CustomerId { get; set; }

    public string RuleName { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public string Action { get; set; } = null!;

    public string? Classification { get; set; }

    public string? ClassificationReason { get; set; }

    public string? ClassificationComment { get; set; }

    public string? NewSeverity { get; set; }

    public string? Comment { get; set; }

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<DssAutomationRuleCondition> DssAutomationRuleConditions { get; set; } = new List<DssAutomationRuleCondition>();
}
