﻿using Microsoft.EntityFrameworkCore;
using OCIRegistry.Models.Database;

namespace OCIRegistry.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Repository> Repositories { get; set; }
        public DbSet<Manifest> Manifests { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Blob> Blobs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Repository>().HasMany(r => r.Manifests).WithOne(m => m.Repository).HasForeignKey(m => m.RepositoryId);
            modelBuilder.Entity<Repository>().HasMany(r => r.Blobs).WithMany(b => b.Repositories);
            modelBuilder.Entity<Manifest>().HasMany(m => m.Blobs).WithMany(b => b.Manifests);
            modelBuilder.Entity<Tag>().HasOne(t => t.Manifest);
            modelBuilder.Entity<User>().HasMany(u => u.Permissions).WithOne(p => p.User).HasForeignKey(p => p.UserId);
        }   
    }
}
