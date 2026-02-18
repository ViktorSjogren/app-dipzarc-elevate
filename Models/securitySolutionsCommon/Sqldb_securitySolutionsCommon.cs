using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dizparc_elevate.Models.securitySolutionsCommon;

public partial class Sqldb_securitySolutionsCommon : DbContext
{
    public Sqldb_securitySolutionsCommon()
    {
    }

    public Sqldb_securitySolutionsCommon(DbContextOptions<Sqldb_securitySolutionsCommon> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomersDatum> CustomersData { get; set; }

    public virtual DbSet<CustomersDssView> CustomersDssViews { get; set; }

    public virtual DbSet<CustomersView> CustomersViews { get; set; }

    public virtual DbSet<DssAutomationRule> DssAutomationRules { get; set; }

    public virtual DbSet<DssAutomationRuleCondition> DssAutomationRuleConditions { get; set; }

    public virtual DbSet<DssAutomationRuleView> DssAutomationRuleViews { get; set; }

    public virtual DbSet<DssDatum> DssData { get; set; }

    public virtual DbSet<DssWhitelist> DssWhitelists { get; set; }

    public virtual DbSet<DssWhitelistView> DssWhitelistViews { get; set; }

    public virtual DbSet<ElevateActivePermission> ElevateActivePermissions { get; set; }

    public virtual DbSet<ElevateAdRoleMembershipView> ElevateAdRoleMembershipViews { get; set; }

    public virtual DbSet<ElevateAdServer> ElevateAdServers { get; set; }

    public virtual DbSet<ElevateAdServersWithOnboardingStatusView> ElevateAdServersWithOnboardingStatusViews { get; set; }

    public virtual DbSet<ElevateAdmin> ElevateAdmins { get; set; }

    public virtual DbSet<ElevateAssignedPermission> ElevateAssignedPermissions { get; set; }

    public virtual DbSet<ElevateAuditLog> ElevateAuditLogs { get; set; }

    public virtual DbSet<ElevateAvailableTier> ElevateAvailableTiers { get; set; }

    public virtual DbSet<ElevateAvdServer> ElevateAvdServers { get; set; }

    public virtual DbSet<ElevateAvdServersView> ElevateAvdServersViews { get; set; }

    public virtual DbSet<ElevateJob> ElevateJobs { get; set; }

    public virtual DbSet<ElevatePermission> ElevatePermissions { get; set; }

    public virtual DbSet<ElevatePermissionType> ElevatePermissionTypes { get; set; }

    public virtual DbSet<ElevateServersView> ElevateServersViews { get; set; }

    public virtual DbSet<ElevateUser> ElevateUsers { get; set; }

    public virtual DbSet<ElevateUserAdminsView> ElevateUserAdminsViews { get; set; }

    public virtual DbSet<ElevateUserPermissionsView> ElevateUserPermissionsViews { get; set; }

    public virtual DbSet<ElevateUsersView> ElevateUsersViews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__customer__B611CB7D0BD7A77D");

            entity.ToTable("customers");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");
        });

        modelBuilder.Entity<CustomersDatum>(entity =>
        {
            entity.HasKey(e => e.CustomerDataId).HasName("PK__customer__CD137D3A68DE75F9");

            entity.ToTable("customersData");

            entity.Property(e => e.CustomerDataId).HasColumnName("customerDataId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.Domains)
                .HasMaxLength(512)
                .HasColumnName("domains");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomersData)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__customers__custo__628FA481");
        });

        modelBuilder.Entity<CustomersDssView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("customersDssView");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(200)
                .HasColumnName("abbreviation");
            entity.Property(e => e.BreakGlassAccount)
                .HasMaxLength(200)
                .HasColumnName("breakGlassAccount");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.Domains)
                .HasMaxLength(512)
                .HasColumnName("domains");
            entity.Property(e => e.FidoKey).HasColumnName("fidoKey");
            entity.Property(e => e.Plan)
                .HasMaxLength(20)
                .HasColumnName("plan");
            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(36)
                .HasColumnName("subscriptionId");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
        });

        modelBuilder.Entity<CustomersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("customersView");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
        });

        modelBuilder.Entity<DssAutomationRule>(entity =>
        {
            entity.HasKey(e => e.DssAutomationRuleId).HasName("PK__dssAutom__3594F57C329C8A5C");

            entity.ToTable("dssAutomationRule");

            entity.Property(e => e.DssAutomationRuleId).HasColumnName("dssAutomationRuleId");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .HasColumnName("action");
            entity.Property(e => e.Classification)
                .HasMaxLength(50)
                .HasColumnName("classification");
            entity.Property(e => e.ClassificationComment)
                .HasMaxLength(500)
                .HasColumnName("classificationComment");
            entity.Property(e => e.ClassificationReason)
                .HasMaxLength(50)
                .HasColumnName("classificationReason");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("isEnabled");
            entity.Property(e => e.NewSeverity)
                .HasMaxLength(20)
                .HasColumnName("newSeverity");
            entity.Property(e => e.RuleName)
                .HasMaxLength(200)
                .HasColumnName("ruleName");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.DssAutomationRules)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_dssAutomationRule_customers");
        });

        modelBuilder.Entity<DssAutomationRuleCondition>(entity =>
        {
            entity.HasKey(e => e.DssAutomationRuleConditionId).HasName("PK__dssAutom__C67B0CBE93BEF45B");

            entity.ToTable("dssAutomationRuleCondition");

            entity.Property(e => e.DssAutomationRuleConditionId).HasColumnName("dssAutomationRuleConditionId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.DssAutomationRuleId).HasColumnName("dssAutomationRuleId");
            entity.Property(e => e.Operation)
                .HasMaxLength(20)
                .HasColumnName("operation");
            entity.Property(e => e.Property)
                .HasMaxLength(50)
                .HasColumnName("property");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");
            entity.Property(e => e.Value)
                .HasMaxLength(500)
                .HasColumnName("value");

            entity.HasOne(d => d.DssAutomationRule).WithMany(p => p.DssAutomationRuleConditions)
                .HasForeignKey(d => d.DssAutomationRuleId)
                .HasConstraintName("FK_dssAutomationRuleCondition_dssAutomationRule");
        });

        modelBuilder.Entity<DssAutomationRuleView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("dssAutomationRuleView");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(200)
                .HasColumnName("abbreviation");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .HasColumnName("action");
            entity.Property(e => e.Classification)
                .HasMaxLength(50)
                .HasColumnName("classification");
            entity.Property(e => e.ClassificationComment)
                .HasMaxLength(500)
                .HasColumnName("classificationComment");
            entity.Property(e => e.ClassificationReason)
                .HasMaxLength(50)
                .HasColumnName("classificationReason");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.DssAutomationRuleId).HasColumnName("dssAutomationRuleId");
            entity.Property(e => e.IsEnabled).HasColumnName("isEnabled");
            entity.Property(e => e.NewSeverity)
                .HasMaxLength(20)
                .HasColumnName("newSeverity");
            entity.Property(e => e.RuleName)
                .HasMaxLength(200)
                .HasColumnName("ruleName");
            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(36)
                .HasColumnName("subscriptionId");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
        });

        modelBuilder.Entity<DssDatum>(entity =>
        {
            entity.HasKey(e => e.DssDataId).HasName("PK__dssData__BF369639521303BF");

            entity.ToTable("dssData");

            entity.Property(e => e.DssDataId).HasColumnName("dssDataId");
            entity.Property(e => e.Abbreviation)
                .HasMaxLength(200)
                .HasColumnName("abbreviation");
            entity.Property(e => e.BreakGlassAccount)
                .HasMaxLength(200)
                .HasColumnName("breakGlassAccount");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.DssServer).HasColumnName("dssServer");
            entity.Property(e => e.FidoKey).HasColumnName("fidoKey");
            entity.Property(e => e.Plan)
                .HasMaxLength(20)
                .HasColumnName("plan");
            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(36)
                .HasColumnName("subscriptionId");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.DssData)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__dssData__custome__02084FDA");
        });

        modelBuilder.Entity<DssWhitelist>(entity =>
        {
            entity.HasKey(e => e.DssWhitelistId).HasName("PK__dssWhite__9EF005BD4B176EB8");

            entity.ToTable("dssWhitelist");

            entity.Property(e => e.DssWhitelistId).HasColumnName("dssWhitelistId");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .HasColumnName("entityName");
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .HasColumnName("entityType");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");
        });

        modelBuilder.Entity<DssWhitelistView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("dssWhitelistView");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(200)
                .HasColumnName("abbreviation");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .HasColumnName("entityName");
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .HasColumnName("entityType");
            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(36)
                .HasColumnName("subscriptionId");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
        });

        modelBuilder.Entity<ElevateActivePermission>(entity =>
        {
            entity.HasKey(e => e.ElevateActivePermissionsId).HasName("PK__elevateA__EF5A02EFB3F03AE6");

            entity.ToTable("elevateActivePermissions");

            entity.Property(e => e.ElevateActivePermissionsId).HasColumnName("elevateActivePermissionsId");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.ManuallyAssigned).HasColumnName("manuallyAssigned");
            entity.Property(e => e.Permission)
                .HasMaxLength(100)
                .HasColumnName("permission");
            entity.Property(e => e.PermissionType).HasColumnName("permissionType");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("userName");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateActivePermissions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAc__custo__59904A2C");

            entity.HasOne(d => d.PermissionTypeNavigation).WithMany(p => p.ElevateActivePermissions)
                .HasForeignKey(d => d.PermissionType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAc__permi__589C25F3");
        });

        modelBuilder.Entity<ElevateAdRoleMembershipView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateAdRoleMembershipView");

            entity.Property(e => e.AddedBy)
                .HasMaxLength(200)
                .HasColumnName("addedBy");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(255)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.LastModified)
                .HasColumnType("datetime")
                .HasColumnName("lastModified");
            entity.Property(e => e.MemberSince)
                .HasColumnType("datetime")
                .HasColumnName("memberSince");
            entity.Property(e => e.RoleMembershipId).HasColumnName("roleMembershipId");
            entity.Property(e => e.RoleValue)
                .HasMaxLength(100)
                .HasColumnName("roleValue");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<ElevateAdServer>(entity =>
        {
            entity.HasKey(e => e.ElevateAdServersId).HasName("PK__elevateA__8DBBEE6B6C239AA7");

            entity.ToTable("elevateAdServers");

            entity.Property(e => e.ElevateAdServersId).HasColumnName("elevateAdServersId");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.FirstSeen)
                .HasColumnType("datetime")
                .HasColumnName("firstSeen");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateAdServers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAd__custo__5B78929E");
        });

        modelBuilder.Entity<ElevateAdServersWithOnboardingStatusView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateAdServersWithOnboardingStatusView");

            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.OnboardingStatus).HasColumnName("onboardingStatus");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Tier)
                .HasMaxLength(100)
                .HasColumnName("tier");
        });

        modelBuilder.Entity<ElevateAdmin>(entity =>
        {
            entity.HasKey(e => e.ElevateAdminsId).HasName("PK__elevateA__76747A9A631363F4");

            entity.ToTable("elevateAdmins");

            entity.Property(e => e.ElevateAdminsId).HasColumnName("elevateAdminsId");
            entity.Property(e => e.AdminRole)
                .HasMaxLength(255)
                .HasColumnName("adminRole");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateAdmins)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAd__custo__5A846E65");
        });

        modelBuilder.Entity<ElevateAssignedPermission>(entity =>
        {
            entity.HasKey(e => e.ElevatePermissionsId).HasName("PK__elevateA__5B69C9512903F8BB");

            entity.ToTable("elevateAssignedPermissions");

            entity.Property(e => e.ElevatePermissionsId).HasColumnName("elevatePermissionsId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.ElevateUserId).HasColumnName("elevateUserId");
            entity.Property(e => e.PermissionId).HasColumnName("permissionId");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.ElevateUser).WithMany(p => p.ElevateAssignedPermissions)
                .HasForeignKey(d => d.ElevateUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAs__eleva__5D60DB10");

            entity.HasOne(d => d.Permission).WithMany(p => p.ElevateAssignedPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAs__permi__5C6CB6D7");
        });

        modelBuilder.Entity<ElevateAuditLog>(entity =>
        {
            entity.HasKey(e => e.ElevateAuditLogId).HasName("PK__elevateA__827E03D9997DF055");

            entity.ToTable("elevateAuditLog");

            entity.Property(e => e.ElevateAuditLogId).HasColumnName("elevateAuditLogId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.Event).HasColumnName("event");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
        });

        modelBuilder.Entity<ElevateAvailableTier>(entity =>
        {
            entity.HasKey(e => e.ElevateAvailableTiersId).HasName("PK__elevateA__81D4A85E53D1E614");

            entity.ToTable("elevateAvailableTiers");

            entity.Property(e => e.ElevateAvailableTiersId).HasColumnName("elevateAvailableTiersId");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasColumnName("displayName");
            entity.Property(e => e.EntraGroupId)
                .HasMaxLength(100)
                .HasColumnName("entraGroupId");
            entity.Property(e => e.TierName)
                .HasMaxLength(100)
                .HasColumnName("tierName");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateAvailableTiers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAv__custo__5E54FF49");
        });

        modelBuilder.Entity<ElevateAvdServer>(entity =>
        {
            entity.HasKey(e => e.ElevateAvdServersId).HasName("PK__elevateA__4E7E1EF08B16BFC7");

            entity.ToTable("elevateAvdServers");

            entity.Property(e => e.ElevateAvdServersId).HasColumnName("elevateAvdServersId");
            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.FirstSeen)
                .HasColumnType("datetime")
                .HasColumnName("firstSeen");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ipAddress");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.Tier).HasColumnName("tier");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateAvdServers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateAv__custo__5F492382");

            entity.HasOne(d => d.TierNavigation).WithMany(p => p.ElevateAvdServers)
                .HasForeignKey(d => d.Tier)
                .HasConstraintName("FK__elevateAvd__tier__603D47BB");
        });

        modelBuilder.Entity<ElevateAvdServersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateAvdServersView");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ipAddress");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Tier)
                .HasMaxLength(100)
                .HasColumnName("tier");
        });

        modelBuilder.Entity<ElevateJob>(entity =>
        {
            entity.HasKey(e => e.ElevateJobsId).HasName("PK__elevateJ__ADDB310E273A2724");

            entity.ToTable("elevateJobs");

            entity.Property(e => e.ElevateJobsId).HasColumnName("elevateJobsId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.Job)
                .HasMaxLength(200)
                .HasColumnName("job");
            entity.Property(e => e.Reference)
                .HasMaxLength(200)
                .HasColumnName("reference");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateJobs)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateJo__custo__61316BF4");
        });

        modelBuilder.Entity<ElevatePermission>(entity =>
        {
            entity.HasKey(e => e.ElevatePermissionsId).HasName("PK__elevateP__5B69C9512316E742");

            entity.ToTable("elevatePermissions");

            entity.Property(e => e.ElevatePermissionsId).HasColumnName("elevatePermissionsId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.OnboardingStatus).HasColumnName("onboardingStatus");
            entity.Property(e => e.Tier).HasColumnName("tier");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
            entity.Property(e => e.Value)
                .HasMaxLength(100)
                .HasColumnName("value");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevatePermissions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevatePe__custo__6225902D");

            entity.HasOne(d => d.TierNavigation).WithMany(p => p.ElevatePermissions)
                .HasForeignKey(d => d.Tier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevatePer__tier__6319B466");

            entity.HasOne(d => d.TypeNavigation).WithMany(p => p.ElevatePermissions)
                .HasForeignKey(d => d.Type)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevatePer__type__640DD89F");
        });

        modelBuilder.Entity<ElevatePermissionType>(entity =>
        {
            entity.HasKey(e => e.ElevatePermissionTypesId).HasName("PK__elevateP__C4975ED58E091772");

            entity.ToTable("elevatePermissionTypes");

            entity.Property(e => e.ElevatePermissionTypesId).HasColumnName("elevatePermissionTypesId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
        });

        modelBuilder.Entity<ElevateServersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateServersView");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.OnboardingStatus).HasColumnName("onboardingStatus");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Tier)
                .HasMaxLength(100)
                .HasColumnName("tier");
        });

        modelBuilder.Entity<ElevateUser>(entity =>
        {
            entity.HasKey(e => e.ElevateUsersId).HasName("PK__elevateU__281193EEFE3B099D");

            entity.ToTable("elevateUsers");

            entity.Property(e => e.ElevateUsersId).HasColumnName("elevateUsersId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(255)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.Tier).HasColumnName("tier");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("(suser_sname())")
                .HasColumnName("updatedBy");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateUsers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateUs__custo__6501FCD8");

            entity.HasOne(d => d.TierNavigation).WithMany(p => p.ElevateUsers)
                .HasForeignKey(d => d.Tier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateUse__tier__65F62111");
        });

        modelBuilder.Entity<ElevateUserAdminsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateUserAdminsView");

            entity.Property(e => e.AdminRole)
                .HasMaxLength(255)
                .HasColumnName("adminRole");
            entity.Property(e => e.AdminRoleAssigned).HasColumnName("adminRoleAssigned");
            entity.Property(e => e.AssignedBy)
                .HasMaxLength(100)
                .HasColumnName("assignedBy");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(255)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.ElevateUsersId).HasColumnName("elevateUsersId");
            entity.Property(e => e.LastUpdated).HasColumnName("lastUpdated");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.UserCreated).HasColumnName("userCreated");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<ElevateUserPermissionsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateUserPermissionsView");

            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(255)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.ElevateUsersId).HasColumnName("elevateUsersId");
            entity.Property(e => e.GrantedBy)
                .HasMaxLength(200)
                .HasColumnName("grantedBy");
            entity.Property(e => e.LastModified)
                .HasColumnType("datetime")
                .HasColumnName("lastModified");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.PermissionGranted)
                .HasColumnType("datetime")
                .HasColumnName("permissionGranted");
            entity.Property(e => e.PermissionType)
                .HasMaxLength(255)
                .HasColumnName("permissionType");
            entity.Property(e => e.PermissionValue)
                .HasMaxLength(100)
                .HasColumnName("permissionValue");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.TierLevel)
                .HasMaxLength(100)
                .HasColumnName("tierLevel");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<ElevateUsersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevateUsersView");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(255)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Tier)
                .HasMaxLength(100)
                .HasColumnName("tier");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("userName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
