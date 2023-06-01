using Goatrello.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartEnum.EFCore;

namespace Goatrello.Data;

//public class GoatrelloDataContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
public class GoatrelloDataContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public GoatrelloDataContext(DbContextOptions<GoatrelloDataContext> options)
        : base(options)
    {
    }

    public DbSet<Activity> Activities { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<LabelList> LabelLists { get; set; }
    public DbSet<LabelListLabel> LabelListLabels { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Listing> Listings { get; set; }
    public DbSet<ServerFile> ServerFiles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserList> UserLists { get; set; }
    public DbSet<UserListUser> UserListUsers { get; set; }
    public DbSet<CustomField> CustomFields { get; set; }
    public DbSet<CustomFieldData> CustomFieldDatas { get; set; }
    
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportFilter> ReportFilters { get; set; }
    public DbSet<ReportOperation> ReportOperations { get; set; }
    public DbSet<ReportResult> ReportResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //the identity stuff
        base.OnModelCreating(modelBuilder);
        
        //stores ReportFilter and ReportOperation values in the db
        modelBuilder.ConfigureSmartEnum();

        //UserList >-< User
        modelBuilder.Entity<UserListUser>()
        .HasKey(u => new { u.ListId, u.UserId });
        
        //LabelList >-< Label
        modelBuilder.Entity<LabelListLabel>()
            .HasKey(l => new { l.ListId, l.LabelId });
        
        //Card >-< CustomField
        modelBuilder.Entity<CustomFieldData>()
            .HasKey(c => new { c.CardId, c.CustomFieldId });

        // Board -< Listing
        modelBuilder.Entity<Board>()
            .HasMany<Listing>(b => b.Listings)
            .WithOne(l => l.Board)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        // Listing -< Card
        modelBuilder.Entity<Listing>()
            .HasMany<Card>(l => l.Cards)
            .WithOne(c => c.Listing)
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Card -< Checklist
        modelBuilder.Entity<Card>()
            .HasMany<Checklist>(c => c.Checklists)
            .WithOne(c => c.Card)
            .HasForeignKey(c => c.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Checklist -< ChecklistItem
        modelBuilder.Entity<Checklist>()
            .HasMany<ChecklistItem>(c => c.ChecklistItems)
            .WithOne(c => c.Checklist)
            .HasForeignKey(c => c.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Card -< Attachment
        modelBuilder.Entity<Card>()
            .HasMany<Attachment>(c => c.Attachments)
            .WithOne(a => a.Card)
            .HasForeignKey(a => a.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // ServerFile - Attachment
        modelBuilder.Entity<ServerFile>()
            .HasOne<Attachment>()
            .WithOne(a => a.ServerFile)
            .HasForeignKey<Attachment>(a => a.ServerFileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Card -< Link
        modelBuilder.Entity<Card>()
            .HasMany<Link>(c => c.Links)
            .WithOne(l => l.Card)
            .HasForeignKey(l => l.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Card -< Activity
        modelBuilder.Entity<Card>()
            .HasMany<Activity>(c => c.Activities)
            .WithOne(a => a.Card)
            .HasForeignKey(a => a.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Card -< CustomFieldData
        modelBuilder.Entity<Card>()
            .HasMany<CustomFieldData>(c => c.CustomFieldDatas)
            .WithOne(c => c.Card)
            .HasForeignKey(c => c.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // User -< ServerFiles
        modelBuilder.Entity<User>()
            .HasMany<ServerFile>()
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Unique Key ReportFilter
        modelBuilder.Entity<ReportFilter>()
            .HasIndex(r => new { r.Name, r.ReportId });
        
        // Unique Key ReportOperation
        modelBuilder.Entity<ReportOperation>()
            .HasIndex(r => new { r.Name, r.ReportId });
        
        // Report -< ReportFilter
        modelBuilder.Entity<Report>()
            .HasMany<ReportFilter>(r => r.Filters)
            .WithOne(r => r.Report)
            .HasForeignKey(r => r.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Report -< ReportOperation
        modelBuilder.Entity<Report>()
            .HasMany<ReportOperation>(r => r.Operations)
            .WithOne(r => r.Report)
            .HasForeignKey(r => r.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Report -< ReportResult
        modelBuilder.Entity<Report>()
            .HasMany<ReportResult>(r => r.Results)
            .WithOne(r => r.Report)
            .HasForeignKey(r => r.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

