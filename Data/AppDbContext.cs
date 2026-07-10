using BookingHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public override int SaveChanges()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        var softDeleted = new HashSet<object>();
        CascadeSoftDelete(now, softDeleted);
        ApplyModifiedTimestamps(now, softDeleted);
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        var softDeleted = new HashSet<object>();
        await CascadeSoftDeleteAsync(now, softDeleted, cancellationToken);
        ApplyModifiedTimestamps(now, softDeleted);
        return await base.SaveChangesAsync(cancellationToken);
    }

    // Zamienia twarde DELETE na soft-delete i schodzi w dół po relacjach Cascade,
    // oznaczając też dzieci (również te niezaładowane — dociągane przez nawigacje).
    private void CascadeSoftDelete(DateTime now, HashSet<object> visited)
    {
        var queue = new Queue<EntityEntry>(
            ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Deleted));

        while (queue.Count > 0)
        {
            var entry = queue.Dequeue();
            if (!visited.Add(entry.Entity))
                continue;

            MarkSoftDeleted(entry, now);

            foreach (var nav in GetCascadeNavigations(entry.Metadata))
            {
                if (nav.IsCollection)
                    entry.Collection(nav.Name).Load();
                else
                    entry.Reference(nav.Name).Load();

                foreach (var dependent in GetLoadedDependents(entry, nav))
                    if (!visited.Contains(dependent))
                        queue.Enqueue(Entry(dependent));
            }
        }
    }

    private async Task CascadeSoftDeleteAsync(DateTime now, HashSet<object> visited, CancellationToken ct)
    {
        var queue = new Queue<EntityEntry>(
            ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Deleted));

        while (queue.Count > 0)
        {
            var entry = queue.Dequeue();
            if (!visited.Add(entry.Entity))
                continue;

            MarkSoftDeleted(entry, now);

            foreach (var nav in GetCascadeNavigations(entry.Metadata))
            {
                if (nav.IsCollection)
                    await entry.Collection(nav.Name).LoadAsync(ct);
                else
                    await entry.Reference(nav.Name).LoadAsync(ct);

                foreach (var dependent in GetLoadedDependents(entry, nav))
                    if (!visited.Contains(dependent))
                        queue.Enqueue(Entry(dependent));
            }
        }
    }

    private void ApplyModifiedTimestamps(DateTime now, HashSet<object> softDeleted)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified && !softDeleted.Contains(entry.Entity))
                entry.Entity.UpdatedAt = now;
        }
    }

    private static void MarkSoftDeleted(EntityEntry entry, DateTime now)
    {
        entry.State = EntityState.Modified;
        var entity = (BaseEntity)entry.Entity;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
    }

    // Nawigacje principal→dependent, których FK ma zachowanie Cascade (kandydaci do kaskady).
    private static IEnumerable<INavigation> GetCascadeNavigations(IEntityType entityType)
    {
        foreach (var nav in entityType.GetNavigations())
            if (!nav.IsOnDependent && nav.ForeignKey.DeleteBehavior == DeleteBehavior.Cascade)
                yield return nav;
    }

    private static IEnumerable<BaseEntity> GetLoadedDependents(EntityEntry entry, INavigation nav)
    {
        if (nav.IsCollection)
        {
            if (entry.Collection(nav.Name).CurrentValue is { } items)
                foreach (var item in items)
                    if (item is BaseEntity dependent)
                        yield return dependent;
        }
        else if (entry.Reference(nav.Name).CurrentValue is BaseEntity dependent)
        {
            yield return dependent;
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationMemberRole> OrganizationMemberRoles => Set<OrganizationMemberRole>();
    public DbSet<ParentChildRelation> ParentChildRelations => Set<ParentChildRelation>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamGroup> TeamGroups => Set<TeamGroup>();
    public DbSet<GroupCostRate> GroupCostRates => Set<GroupCostRate>();
    public DbSet<TrainerSessionRate> TrainerSessionRates => Set<TrainerSessionRate>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageRecipient> MessageRecipients => Set<MessageRecipient>();
    public DbSet<CancellationRequest> CancellationRequests => Set<CancellationRequest>();
    public DbSet<MemberAvailability> MemberAvailabilities => Set<MemberAvailability>();
    public DbSet<ParticipantTrainer> ParticipantTrainers => Set<ParticipantTrainer>();
    public DbSet<TeamTrainer> TeamTrainers => Set<TeamTrainer>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<EventSeries> EventSeries => Set<EventSeries>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventTrainer> EventTrainers => Set<EventTrainer>();
    public DbSet<EventEnrollment> EventEnrollments => Set<EventEnrollment>();
    public DbSet<EventTeamEnrollment> EventTeamEnrollments => Set<EventTeamEnrollment>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Globalne filtry soft delete ──────────────────────────────────────
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Person>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Organization>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrganizationMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrganizationMemberRole>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ParentChildRelation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Group>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GroupMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Team>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TeamMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TeamGroup>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GroupCostRate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainerSessionRate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Message>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MessageRecipient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CancellationRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MemberAvailability>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ParticipantTrainer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TeamTrainer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Location>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EventSeries>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EventTrainer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EventEnrollment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EventTeamEnrollment>().HasQueryFilter(e => !e.IsDeleted);

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.ExternalId).HasMaxLength(200).IsRequired();
            e.Property(u => u.AuthProvider).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.IsActive).HasDefaultValue(true);

            // Częściowe indeksy unikalne — soft-delete'owany User nie blokuje ponownego użycia
            // tego samego emaila/ExternalId przez nowy rekord
            e.HasIndex(u => new { u.ExternalId, u.AuthProvider })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(u => u.Email)
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");
        });

        // ── Person ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Person>(e =>
        {
            e.Property(p => p.FirstName).HasMaxLength(100);
            e.Property(p => p.LastName).HasMaxLength(100);
            e.Property(p => p.PhotoUrl).HasMaxLength(500);

            // 1 User = 0..1 Person (UserId nullable)
            e.HasOne(p => p.User)
             .WithOne(u => u.Person)
             .HasForeignKey<Person>(p => p.UserId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            // Jawny częściowy indeks unikalny na UserId — EF tworzy automatyczny dla 1-to-1,
            // ale bez filtra soft delete blokuje reużycie UserId po soft-delete Person
            e.HasIndex(p => p.UserId)
             .IsUnique()
             .HasFilter("\"UserId\" IS NOT NULL AND \"IsDeleted\" = false");
        });

        // ── Organization ─────────────────────────────────────────────────────
        modelBuilder.Entity<Organization>(e =>
        {
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.Description).HasMaxLength(1000);

            e.HasOne(o => o.CreatedByPerson)
             .WithMany()
             .HasForeignKey(o => o.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── OrganizationMember ───────────────────────────────────────────────
        modelBuilder.Entity<OrganizationMember>(e =>
        {
            e.Property(m => m.DisplayName).HasMaxLength(100);
            e.Property(m => m.PhotoUrl).HasMaxLength(500);
            e.Property(m => m.Color).HasMaxLength(7);
            e.Property(m => m.IsActive).HasDefaultValue(true);

            // Jedna osoba = jeden membership per organizacja
            e.HasIndex(m => new { m.OrganizationId, m.PersonId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(m => m.OrganizationId);
            e.HasIndex(m => m.PersonId);

            e.HasOne(m => m.Organization)
             .WithMany(o => o.Members)
             .HasForeignKey(m => m.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Person)
             .WithMany(p => p.Memberships)
             .HasForeignKey(m => m.PersonId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.CreatedBy)
             .WithMany()
             .HasForeignKey(m => m.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── OrganizationMemberRole ────────────────────────────────────────────
        modelBuilder.Entity<OrganizationMemberRole>(e =>
        {
            e.Property(r => r.Role).HasConversion<string>().HasMaxLength(50);

            // Ta sama rola nie może być przypisana dwa razy do tego samego membership
            e.HasIndex(r => new { r.OrganizationMemberId, r.Role })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(r => r.OrganizationMemberId);

            e.HasOne(r => r.OrganizationMember)
             .WithMany(m => m.Roles)
             .HasForeignKey(r => r.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ParentChildRelation ──────────────────────────────────────────────
        modelBuilder.Entity<ParentChildRelation>(e =>
        {
            // Częściowy indeks unikalny — soft-delete pozwala na ponowne powiązanie
            // tego samego rodzica z dzieckiem jeśli poprzednia relacja została usunięta
            e.HasIndex(r => new { r.ParentPersonId, r.ChildPersonId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(r => r.ParentPersonId);
            e.HasIndex(r => r.ChildPersonId);

            // Restrict — nie usuwamy relacji automatycznie przy usunięciu Person
            e.HasOne(r => r.Parent)
             .WithMany(p => p.ChildRelations)
             .HasForeignKey(r => r.ParentPersonId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Child)
             .WithMany(p => p.ParentRelations)
             .HasForeignKey(r => r.ChildPersonId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.CreatedBy)
             .WithMany()
             .HasForeignKey(r => r.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Group ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Group>(e =>
        {
            e.Property(g => g.Name).HasMaxLength(200).IsRequired();
            e.Property(g => g.Description).HasMaxLength(1000);
            e.Property(g => g.Color).HasMaxLength(7);
            e.Property(g => g.IsActive).HasDefaultValue(true);

            e.HasIndex(g => g.OrganizationId);

            e.HasOne(g => g.Organization)
             .WithMany(o => o.Groups)
             .HasForeignKey(g => g.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.CreatedBy)
             .WithMany()
             .HasForeignKey(g => g.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── GroupMember ──────────────────────────────────────────────────────
        modelBuilder.Entity<GroupMember>(e =>
        {
            // Ten sam uczestnik nie może być dwa razy w tej samej grupie
            e.HasIndex(gm => new { gm.GroupId, gm.OrganizationMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(gm => gm.GroupId);
            e.HasIndex(gm => gm.OrganizationMemberId);

            e.HasOne(gm => gm.Group)
             .WithMany(g => g.Members)
             .HasForeignKey(gm => gm.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(gm => gm.OrganizationMember)
             .WithMany(m => m.GroupMemberships)
             .HasForeignKey(gm => gm.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(gm => gm.CreatedBy)
             .WithMany()
             .HasForeignKey(gm => gm.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Team ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Team>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.Notes).HasMaxLength(1000);
            e.Property(t => t.IsActive).HasDefaultValue(true);
            e.Property(t => t.Priority).IsRequired(false);

            e.HasIndex(t => t.OrganizationId);

            e.HasOne(t => t.Organization)
             .WithMany(o => o.Teams)
             .HasForeignKey(t => t.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.CreatedBy)
             .WithMany()
             .HasForeignKey(t => t.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TeamMember ────────────────────────────────────────────────────────
        modelBuilder.Entity<TeamMember>(e =>
        {
            // Ten sam uczestnik nie może być dwa razy w tym samym składzie
            e.HasIndex(tm => new { tm.TeamId, tm.OrganizationMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(tm => tm.TeamId);
            e.HasIndex(tm => tm.OrganizationMemberId);

            e.HasOne(tm => tm.Team)
             .WithMany(t => t.Members)
             .HasForeignKey(tm => tm.TeamId)
             .OnDelete(DeleteBehavior.Cascade);

            // Restrict — usunięcie uczestnika z org nie kasuje historii składu automatycznie
            e.HasOne(tm => tm.OrganizationMember)
             .WithMany(m => m.TeamMemberships)
             .HasForeignKey(tm => tm.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(tm => tm.CreatedBy)
             .WithMany()
             .HasForeignKey(tm => tm.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TeamGroup ─────────────────────────────────────────────────────────
        modelBuilder.Entity<TeamGroup>(e =>
        {
            // Skład może być przypisany do grupy tylko raz
            e.HasIndex(tg => new { tg.TeamId, tg.GroupId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(tg => tg.TeamId);
            e.HasIndex(tg => tg.GroupId);

            e.HasOne(tg => tg.Team)
             .WithMany(t => t.Groups)
             .HasForeignKey(tg => tg.TeamId)
             .OnDelete(DeleteBehavior.Cascade);

            // Usunięcie grupy kasuje przypisania składów — sam skład zostaje w organizacji
            e.HasOne(tg => tg.Group)
             .WithMany(g => g.Teams)
             .HasForeignKey(tg => tg.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(tg => tg.CreatedBy)
             .WithMany()
             .HasForeignKey(tg => tg.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── GroupCostRate ─────────────────────────────────────────────────────
        modelBuilder.Entity<GroupCostRate>(e =>
        {
            e.Property(r => r.MonthlyCost).HasPrecision(18, 2);
            e.Property(r => r.Currency).HasMaxLength(3).IsRequired();

            e.HasIndex(r => r.GroupId);
            e.HasIndex(r => new { r.GroupId, r.ValidFrom });

            e.HasOne(r => r.Group)
             .WithMany(g => g.CostRates)
             .HasForeignKey(r => r.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.CreatedBy)
             .WithMany()
             .HasForeignKey(r => r.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TrainerSessionRate ────────────────────────────────────────────────
        modelBuilder.Entity<TrainerSessionRate>(e =>
        {
            e.Property(r => r.RatePerHour).HasPrecision(18, 2);
            e.Property(r => r.Currency).HasMaxLength(3).IsRequired();

            e.HasIndex(r => r.TrainerMemberId);
            e.HasIndex(r => new { r.TrainerMemberId, r.ValidFrom });

            // Restrict — historia stawek zostaje nawet gdy trener opuści organizację
            e.HasOne(r => r.Trainer)
             .WithMany(m => m.SessionRates)
             .HasForeignKey(r => r.TrainerMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.CreatedBy)
             .WithMany()
             .HasForeignKey(r => r.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Message ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Message>(e =>
        {
            e.Property(m => m.Subject).HasMaxLength(200).IsRequired();
            e.Property(m => m.Body).HasMaxLength(5000).IsRequired();
            e.Property(m => m.IsAutomatic).HasDefaultValue(false);

            e.HasIndex(m => m.OrganizationId);
            e.HasIndex(m => m.SenderMemberId);
            e.HasIndex(m => new { m.SenderMemberId, m.SentAt });

            e.HasOne(m => m.Organization)
             .WithMany(o => o.Messages)
             .HasForeignKey(m => m.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
             .WithMany(om => om.SentMessages)
             .HasForeignKey(m => m.SenderMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.RelatedEvent)
             .WithMany(ev => ev.RelatedMessages)
             .HasForeignKey(m => m.RelatedEventId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            // Self-referential: odpowiedź wskazuje na wiadomość nadrzędną
            e.HasOne(m => m.ParentMessage)
             .WithMany(m => m.Replies)
             .HasForeignKey(m => m.ParentMessageId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(m => m.CreatedBy)
             .WithMany()
             .HasForeignKey(m => m.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── MessageRecipient ──────────────────────────────────────────────────
        modelBuilder.Entity<MessageRecipient>(e =>
        {
            e.Property(mr => mr.IsRead).HasDefaultValue(false);

            // Ta sama osoba nie może być dwa razy odbiorcą tej samej wiadomości
            e.HasIndex(mr => new { mr.MessageId, mr.RecipientMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(mr => mr.RecipientMemberId);

            // Szybkie pobieranie nieprzeczytanych dla skrzynki odbiorczej
            e.HasIndex(mr => new { mr.RecipientMemberId, mr.IsRead });

            e.HasOne(mr => mr.Message)
             .WithMany(m => m.Recipients)
             .HasForeignKey(mr => mr.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(mr => mr.Recipient)
             .WithMany(om => om.ReceivedMessages)
             .HasForeignKey(mr => mr.RecipientMemberId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── CancellationRequest ───────────────────────────────────────────────
        modelBuilder.Entity<CancellationRequest>(e =>
        {
            e.Property(cr => cr.Reason).HasMaxLength(500);
            e.Property(cr => cr.ReviewNote).HasMaxLength(500);
            e.Property(cr => cr.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(cr => cr.Status).HasDefaultValue(CancellationStatus.Pending);

            // Pobieranie wszystkich wniosków dla danego zapisu
            e.HasIndex(cr => cr.EventEnrollmentId);

            // Pobieranie oczekujących wniosków (najczęstsze zapytanie trenera)
            e.HasIndex(cr => new { cr.EventEnrollmentId, cr.Status });

            e.HasOne(cr => cr.EventEnrollment)
             .WithMany(ee => ee.CancellationRequests)
             .HasForeignKey(cr => cr.EventEnrollmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cr => cr.RequestedBy)
             .WithMany()
             .HasForeignKey(cr => cr.RequestedByMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(cr => cr.ReviewedBy)
             .WithMany()
             .HasForeignKey(cr => cr.ReviewedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(cr => cr.CreatedBy)
             .WithMany()
             .HasForeignKey(cr => cr.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── MemberAvailability ────────────────────────────────────────────────
        modelBuilder.Entity<MemberAvailability>(e =>
        {
            e.Property(a => a.DayOfWeek).HasConversion<string>().HasMaxLength(10);

            // Szybkie pobieranie dostępności per członek
            e.HasIndex(a => a.OrganizationMemberId);

            // Typowe zapytanie: „dostępność w konkretnym dniu tygodnia dla wielu członków"
            e.HasIndex(a => new { a.OrganizationMemberId, a.DayOfWeek });

            e.HasOne(a => a.OrganizationMember)
             .WithMany(m => m.Availability)
             .HasForeignKey(a => a.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.CreatedBy)
             .WithMany()
             .HasForeignKey(a => a.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── ParticipantTrainer ────────────────────────────────────────────────
        modelBuilder.Entity<ParticipantTrainer>(e =>
        {
            // Ten sam uczestnik nie może mieć tego samego trenera dwa razy
            e.HasIndex(pt => new { pt.ParticipantMemberId, pt.TrainerMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(pt => pt.ParticipantMemberId);
            e.HasIndex(pt => pt.TrainerMemberId);

            e.HasOne(pt => pt.Participant)
             .WithMany(m => m.AssignedTrainers)
             .HasForeignKey(pt => pt.ParticipantMemberId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pt => pt.Trainer)
             .WithMany(m => m.AssignedParticipants)
             .HasForeignKey(pt => pt.TrainerMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(pt => pt.CreatedBy)
             .WithMany()
             .HasForeignKey(pt => pt.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TeamTrainer ───────────────────────────────────────────────────────
        modelBuilder.Entity<TeamTrainer>(e =>
        {
            // Ten sam trener nie może być przypisany dwa razy do tego samego składu
            e.HasIndex(tt => new { tt.TeamId, tt.TrainerMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(tt => tt.TeamId);
            e.HasIndex(tt => tt.TrainerMemberId);

            e.HasOne(tt => tt.Team)
             .WithMany(t => t.Trainers)
             .HasForeignKey(tt => tt.TeamId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(tt => tt.Trainer)
             .WithMany(m => m.AssignedTeams)
             .HasForeignKey(tt => tt.TrainerMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(tt => tt.CreatedBy)
             .WithMany()
             .HasForeignKey(tt => tt.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Location ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Location>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(200).IsRequired();
            e.Property(l => l.Address).HasMaxLength(500);
            e.Property(l => l.Description).HasMaxLength(1000);
            e.Property(l => l.IsActive).HasDefaultValue(true);

            e.HasIndex(l => l.OrganizationId);

            e.HasOne(l => l.Organization)
             .WithMany(o => o.Locations)
             .HasForeignKey(l => l.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.CreatedBy)
             .WithMany()
             .HasForeignKey(l => l.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── EventSeries ───────────────────────────────────────────────────────
        modelBuilder.Entity<EventSeries>(e =>
        {
            e.Property(es => es.Title).HasMaxLength(200).IsRequired();
            e.Property(es => es.Description).HasMaxLength(2000);
            e.Property(es => es.RecurrenceRule).HasMaxLength(500);
            e.Property(es => es.DefaultColor).HasMaxLength(7);
            e.Property(es => es.DefaultEventType).HasConversion<string>().HasMaxLength(50);
            e.Property(es => es.DefaultEventType).HasDefaultValue(EventType.GroupTraining);
            e.Property(es => es.IsActive).HasDefaultValue(true);

            e.HasIndex(es => es.OrganizationId);

            e.HasOne(es => es.Organization)
             .WithMany(o => o.EventSeries)
             .HasForeignKey(es => es.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(es => es.DefaultGroup)
             .WithMany(g => g.EventSeriesDefaults)
             .HasForeignKey(es => es.DefaultGroupId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(es => es.DefaultLocation)
             .WithMany(l => l.EventSeries)
             .HasForeignKey(es => es.DefaultLocationId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(es => es.CreatedBy)
             .WithMany()
             .HasForeignKey(es => es.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Event ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(ev => ev.Title).HasMaxLength(200).IsRequired();
            e.Property(ev => ev.Description).HasMaxLength(2000);
            e.Property(ev => ev.EventType).HasConversion<string>().HasMaxLength(50);
            e.Property(ev => ev.EventType).HasDefaultValue(EventType.GroupTraining);
            e.Property(ev => ev.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(ev => ev.Status).HasDefaultValue(EventStatus.Scheduled);
            e.Property(ev => ev.Color).HasMaxLength(7);
            e.Property(ev => ev.UnitCost).HasPrecision(18, 2).IsRequired(false);
            e.Property(ev => ev.Currency).HasMaxLength(3).IsRequired(false);

            e.HasIndex(ev => ev.OrganizationId);
            e.HasIndex(ev => ev.EventSeriesId);
            e.HasIndex(ev => ev.StartTime);

            e.HasOne(ev => ev.Organization)
             .WithMany(o => o.Events)
             .HasForeignKey(ev => ev.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ev => ev.EventSeries)
             .WithMany(es => es.Events)
             .HasForeignKey(ev => ev.EventSeriesId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ev => ev.Location)
             .WithMany(l => l.Events)
             .HasForeignKey(ev => ev.LocationId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(ev => ev.Group)
             .WithMany(g => g.Events)
             .HasForeignKey(ev => ev.GroupId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(ev => ev.CreatedBy)
             .WithMany()
             .HasForeignKey(ev => ev.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── EventTrainer ──────────────────────────────────────────────────────
        modelBuilder.Entity<EventTrainer>(e =>
        {
            // Ten sam trener nie może być przypisany dwa razy do tych samych zajęć
            e.HasIndex(et => new { et.EventId, et.OrganizationMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(et => et.EventId);
            e.HasIndex(et => et.OrganizationMemberId);

            e.HasOne(et => et.Event)
             .WithMany(ev => ev.Trainers)
             .HasForeignKey(et => et.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(et => et.OrganizationMember)
             .WithMany(m => m.EventsAsTrainer)
             .HasForeignKey(et => et.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(et => et.CreatedBy)
             .WithMany()
             .HasForeignKey(et => et.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── EventEnrollment ───────────────────────────────────────────────────
        modelBuilder.Entity<EventEnrollment>(e =>
        {
            // Uczestnik nie może być zapisany dwa razy na te same zajęcia
            e.HasIndex(ee => new { ee.EventId, ee.OrganizationMemberId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(ee => ee.EventId);
            e.HasIndex(ee => ee.OrganizationMemberId);

            e.Property(ee => ee.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(ee => ee.Status).HasDefaultValue(EventEnrollmentStatus.Enrolled);

            e.HasOne(ee => ee.Event)
             .WithMany(ev => ev.Enrollments)
             .HasForeignKey(ee => ee.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ee => ee.OrganizationMember)
             .WithMany(m => m.EventEnrollments)
             .HasForeignKey(ee => ee.OrganizationMemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ee => ee.CreatedBy)
             .WithMany()
             .HasForeignKey(ee => ee.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── EventTeamEnrollment ───────────────────────────────────────────────
        modelBuilder.Entity<EventTeamEnrollment>(e =>
        {
            // Ten sam skład nie może być zapisany dwa razy na te same zajęcia
            e.HasIndex(ete => new { ete.EventId, ete.TeamId })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false");

            e.HasIndex(ete => ete.EventId);
            e.HasIndex(ete => ete.TeamId);

            e.Property(ete => ete.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(ete => ete.Status).HasDefaultValue(EventEnrollmentStatus.Enrolled);

            e.HasOne(ete => ete.Event)
             .WithMany(ev => ev.TeamEnrollments)
             .HasForeignKey(ete => ete.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ete => ete.Team)
             .WithMany(t => t.EventEnrollments)
             .HasForeignKey(ete => ete.TeamId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ete => ete.CreatedBy)
             .WithMany()
             .HasForeignKey(ete => ete.CreatedByPersonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── UserDeviceToken ───────────────────────────────────────────────────
        modelBuilder.Entity<UserDeviceToken>(e =>
        {
            e.Property(t => t.Token).HasMaxLength(500).IsRequired();
            e.Property(t => t.Platform).HasConversion<string>().HasMaxLength(20);

            // Jeden token = jeden user (zapobiega duplikatom przy re-rejestracji)
            e.HasIndex(t => new { t.UserId, t.Token }).IsUnique();
            e.HasIndex(t => t.UserId);

            e.HasOne(t => t.User)
             .WithMany(u => u.DeviceTokens)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Globalne wymuszenie UTC dla wszystkich pól DateTime ───────────────
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcConverter);
            }
        }
    }
}
