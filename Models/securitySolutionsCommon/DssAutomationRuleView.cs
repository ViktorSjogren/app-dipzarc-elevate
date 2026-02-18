using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssAutomationRuleView
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

    public string? CustomerName { get; set; }

    public string? TenantId { get; set; }

    public string? SubscriptionId { get; set; }

    public string? Abbreviation { get; set; }
}
