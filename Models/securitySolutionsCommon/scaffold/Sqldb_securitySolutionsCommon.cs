using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dizparc_elevate.Models.securitySolutionsCommon.scaffold;

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

    public virtual DbSet<DssDatum> DssData { get; set; }

    public virtual DbSet<DssWhitelist> DssWhitelists { get; set; }

    public virtual DbSet<DssWhitelistView> DssWhitelistViews { get; set; }

    public virtual DbSet<ElevatePermission> ElevatePermissions { get; set; }

    public virtual DbSet<ElevatePermissionsView> ElevatePermissionsViews { get; set; }

    public virtual DbSet<ElevateServer> ElevateServers { get; set; }

    public virtual DbSet<ElevateServersView> ElevateServersViews { get; set; }

    public virtual DbSet<ElevateUser> ElevateUsers { get; set; }

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

        modelBuilder.Entity<ElevatePermission>(entity =>
        {
            entity.HasKey(e => e.ElevatePermissionsId).HasName("PK__elevateP__5B69C95176C3AAEF");

            entity.ToTable("elevatePermissions");

            entity.Property(e => e.ElevatePermissionsId).HasColumnName("elevatePermissionsId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.ElevateUserId).HasColumnName("elevateUserId");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");
            entity.Property(e => e.Value)
                .HasMaxLength(100)
                .HasColumnName("value");

            entity.HasOne(d => d.ElevateUser).WithMany(p => p.ElevatePermissions)
                .HasForeignKey(d => d.ElevateUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevatePe__eleva__2180FB33");
        });

        modelBuilder.Entity<ElevatePermissionsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("elevatePermissionsView");

            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customerName");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(100)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
            entity.Property(e => e.Value)
                .HasMaxLength(100)
                .HasColumnName("value");
        });

        modelBuilder.Entity<ElevateServer>(entity =>
        {
            entity.HasKey(e => e.ElevateServersId).HasName("PK__elevateS__E171415851796044");

            entity.ToTable("elevateServers");

            entity.Property(e => e.ElevateServersId).HasColumnName("elevateServersId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.ServerName)
                .HasMaxLength(100)
                .HasColumnName("serverName");
            entity.Property(e => e.Tier)
                .HasMaxLength(100)
                .HasColumnName("tier");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateServers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateSe__custo__1CBC4616");
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
            entity.HasKey(e => e.ElevateUsersId).HasName("PK__elevateU__281193EE54C4C737");

            entity.ToTable("elevateUsers");

            entity.Property(e => e.ElevateUsersId).HasColumnName("elevateUsersId");
            entity.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200)
                .HasColumnName("createdBy");
            entity.Property(e => e.CustomerId).HasColumnName("customerId");
            entity.Property(e => e.ElevateAccount)
                .HasMaxLength(100)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(12)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Updated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updatedBy");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.Customer).WithMany(p => p.ElevateUsers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__elevateUs__custo__17F790F9");
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
                .HasMaxLength(100)
                .HasColumnName("elevateAccount");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(12)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.TenantId)
                .HasMaxLength(200)
                .HasColumnName("tenantId");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
