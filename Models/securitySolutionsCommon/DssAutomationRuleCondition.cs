using System;
using System.Collections.Generic;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class DssAutomationRuleCondition
{
    public int DssAutomationRuleConditionId { get; set; }

    public int DssAutomationRuleId { get; set; }

    public string Property { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public string Value { get; set; } = null!;

    public DateTime? Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? Updated { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual DssAutomationRule DssAutomationRule { get; set; } = null!;
}
