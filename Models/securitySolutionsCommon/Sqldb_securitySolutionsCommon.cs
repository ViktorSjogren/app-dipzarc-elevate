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

    public virtual DbSet<CustomersView> CustomersViews { get; set; }

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
