using EWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<CategoryBG> BGCategories { get; set; }
        public DbSet<ProductBG> BGProducts { get; set; }
        public DbSet<ShoppingCart> BGShoppingCart { get; set; }
        public DbSet<ShippingAddress> BGShippingAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUser>().ToTable("BGUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("BGRoles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("BGUserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("BGUserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("BGUserLogins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("BGUserTokens");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("BGRoleClaims");

            modelBuilder.Entity<CategoryBG>().ToTable("BGCategories");

            modelBuilder.Entity<ProductBG>().ToTable("BGProducts");

            modelBuilder.Entity<ShoppingCart>(entity =>
            {
                entity.ToTable("BGShoppingCart");

                entity.HasOne(sc => sc.ProductBG)
                      .WithMany()
                      .HasForeignKey(sc => sc.ProductID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sc => sc.IdentityUser)
                      .WithMany()
                      .HasForeignKey(sc => sc.IdentityUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(sc => new { sc.IdentityUserId, sc.ProductID })
                      .IsUnique();
            });

            modelBuilder.Entity<ShippingAddress>(entity =>
            {
                entity.ToTable("BGShippingAddresses");

                entity.HasOne(sa => sa.IdentityUser)
                      .WithMany()
                      .HasForeignKey(sa => sa.IdentityUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(sa => new { sa.IdentityUserId, sa.IsDefault })
                      .HasFilter("[IsDefault] = 1")
                      .IsUnique();
            });

            modelBuilder.Entity<CategoryBG>().HasData(
                new CategoryBG { Id = 1, DisplayOrder = 1, Name = "Fantasy" },
                new CategoryBG { Id = 2, DisplayOrder = 1, Name = "SciFi" },
                new CategoryBG { Id = 3, DisplayOrder = 2, Name = "Comedy" }
            );

            modelBuilder.Entity<ProductBG>().HasData(
                new ProductBG
                {
                    Id = 1,
                    Title = "Fortune of Time",
                    Author = "Billy Spark",
                    Description = "Praesent vitae sodales libero. Praesent molestie orci augue.",
                    ISBN = "SWD9999001",
                    ListPrice = 99,
                    Price = 90,
                    Price50 = 80,
                    Price100 = 80,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 200
                },
                new ProductBG
                {
                    Id = 2,
                    Title = "Dark Skies",
                    Author = "Nancy Hoover",
                    Description = "Praesent vitae sodales libero. Praesent molestie orci augue.",
                    ISBN = "CAW777777701",
                    ListPrice = 40,
                    Price = 30,
                    Price50 = 25,
                    Price100 = 20,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 150
                },
                new ProductBG
                {
                    Id = 3,
                    Title = "Vanish in the Sunset",
                    Author = "Julian Button",
                    Description = "Praesent vitae sodales libero.",
                    ISBN = "RITO5555501",
                    ListPrice = 55,
                    Price = 50,
                    Price50 = 45,
                    Price100 = 35,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 180
                },
                new ProductBG
                {
                    Id = 4,
                    Title = "Cotton Candy",
                    Author = "Abby Muscles",
                    Description = "Praesent vitae sodales libero.",
                    ISBN = "WS3333333301",
                    ListPrice = 70,
                    Price = 65,
                    Price50 = 60,
                    Price100 = 55,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 300
                },
                new ProductBG
                {
                    Id = 5,
                    Title = "Rock in the Ocean",
                    Author = "Ron Parker",
                    Description = "Praesent vitae sodales libero.",
                    ISBN = "SOTJ11111101",
                    ListPrice = 30,
                    Price = 27,
                    Price50 = 25,
                    Price100 = 20,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 120
                },
                new ProductBG
                {
                    Id = 6,
                    Title = "Leaves and Wonders",
                    Author = "Laura Phantom",
                    Description = "Praesent vitae sodales libero.",
                    ISBN = "FOT00000001",
                    ListPrice = 25,
                    Price = 23,
                    Price50 = 22,
                    Price100 = 20,
                    CategoryID = 1,
                    ImageURL = "",
                    MaxQuantityPerOrder = 160
                }
            );
        }
    }
}