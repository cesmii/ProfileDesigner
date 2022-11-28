namespace CESMII.ProfileDesigner.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;
    using CESMII.ProfileDesigner.Data.Entities;

    public class ProfileDesignerPgContext : DbContext
    {
        public ProfileDesignerPgContext(DbContextOptions<ProfileDesignerPgContext> options) : base(options)
        {
            // Blank
        }

        protected ProfileDesignerPgContext(DbContextOptions options)
        {
            // Blank
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseLazyLoadingProxies(); 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Org
            modelBuilder.Entity<Organization>().ToTable("organization", "public");

            // Users, Roles and User to Roles
            modelBuilder.Entity<User>().ToTable("user", "public");

            //FK lookups to org
            modelBuilder.Entity<User>()
                .HasOne(p => p.Organization).WithMany().HasForeignKey(p => p.OrganizationId);

            // User to permission Join table.
            modelBuilder.Entity<UserPermission>()
                .ToTable("user_permission", "public")
                .HasKey(p => new { p.PermissionId, p.UserId });

            // Permission table.
            modelBuilder.Entity<Permission>().ToTable("permission", "public");

            //------------------------------------------------
            // Profiles
            //------------------------------------------------
            modelBuilder.Entity<ProfileTypeDefinition>().ToTable("profile_type_definition", "public")
                .HasOne(p => p.Parent).WithMany(/*p => p.Children*/).HasForeignKey(p => p.ParentId);
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasOne(p => p.InstanceParent).WithMany(/*p => p.Children*/).HasForeignKey(p => p.InstanceParentId);
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasOne(p => p.VariableDataType).WithMany(/*p => p.Children*/).HasForeignKey(p => p.VariableDataTypeId);
            //FK lookups to profile
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasOne(p => p.ProfileType).WithMany().HasForeignKey(p => p.ProfileTypeId);
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasOne(p => p.Author).WithMany().HasForeignKey(v => v.AuthorId);
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasOne(r => r.Profile).WithMany().HasForeignKey(r => r.ProfileId);
            modelBuilder.Entity<ProfileTypeDefinition>()
                .HasMany(r => r.Compositions).WithOne().HasForeignKey(r => r.ProfileTypeDefinitionId);

            //------------------------------------------------
            // Profile Interfaces Join Table
            //------------------------------------------------
            modelBuilder.Entity<ProfileInterface>()
                .ToTable("profile_interface", "public");
            //FK lookups to composition
            modelBuilder.Entity<ProfileInterface>()
                .HasOne(p => p.ProfileTypeDefinition).WithMany(p => p.Interfaces).HasForeignKey(p => p.ProfileTypeDefinitionId);
            modelBuilder.Entity<ProfileInterface>()
                .HasOne(p => p.Interface).WithMany().HasForeignKey(p => p.InterfaceId);

            //------------------------------------------------
            // Profile Attributes
            //------------------------------------------------
            modelBuilder.Entity<ProfileAttribute>().ToTable("profile_attribute", "public")
                .HasOne(p => p.ProfileTypeDefinition).WithMany(p => p.Attributes).HasForeignKey(p => p.ProfileTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProfileAttribute>()
                .HasOne(a => a.VariableTypeDefinition).WithMany().HasForeignKey(a => a.VariableTypeDefinitionId);
            //FK lookups for attributes
            modelBuilder.Entity<ProfileAttribute>()
                .HasOne(a => a.EngUnit).WithMany().HasForeignKey(a => a.EngUnitId);
            modelBuilder.Entity<ProfileAttribute>()
                .HasOne(a => a.DataType).WithMany().HasForeignKey(a => a.DataTypeId);
            modelBuilder.Entity<ProfileAttribute>()
                .HasOne(a => a.AttributeType).WithMany().HasForeignKey(a => a.AttributeTypeId);


            //------------------------------------------------
            // Profile Compositions Join Table
            //------------------------------------------------
            modelBuilder.Entity<ProfileComposition>()
                .ToTable("profile_composition", "public");
            //FK lookups to composition
            modelBuilder.Entity<ProfileComposition>()
                .HasOne(p => p.ProfileTypeDefinition).WithMany(p => p.Compositions).HasForeignKey(p => p.ProfileTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProfileComposition>()
                .HasOne(p => p.Composition).WithMany().HasForeignKey(p => p.CompositionId);

            //------------------------------------------------
            // Profile analytic Join Table
            //------------------------------------------------
            modelBuilder.Entity<ProfileTypeDefinitionAnalytic>()
                .ToTable("profile_type_definition_user_analytics", "public");
            modelBuilder.Entity<ProfileTypeDefinitionAnalytic>().ToTable("profile_type_definition_user_analytics", "public")
                .HasOne(p => p.ProfileTypeDefinition).WithOne(p => p.Analytics) //.HasForeignKey(p => p.ProfileTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            //------------------------------------------------
            // Profile favorite Join Table
            //------------------------------------------------
            modelBuilder.Entity<ProfileTypeDefinitionFavorite>().ToTable("profile_type_definition_user_favorite", "public")
                .HasOne(p => p.ProfileTypeDefinition).WithOne(p => p.Favorite) //.HasForeignKey(p => p.ProfileTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            //------------------------------------------------
            // Engineering Unit
            //------------------------------------------------
            modelBuilder.Entity<EngineeringUnit>().ToTable("engineering_unit", "public");

            //read only / get only version of this entity
            modelBuilder.Entity<EngineeringUnitRanked>().ToTable("v_engineering_unit_rank", "public");

            //------------------------------------------------
            //Lookup data
            //------------------------------------------------
            modelBuilder.Entity<LookupItem>().ToTable("lookup", "public");
            modelBuilder.Entity<LookupType>().ToTable("lookup_type", "public");
            //FK lookups to lookup
            modelBuilder.Entity<LookupItem>()
                .HasOne(r => r.LookupType).WithMany().HasForeignKey(r => r.TypeId);

            modelBuilder.Entity<LookupDataType>().ToTable("data_type", "public") 
                .HasOne(a => a.CustomType).WithMany().HasForeignKey(a => a.CustomTypeId);

            //read only / get only version of this entity
            modelBuilder.Entity<LookupDataTypeRanked>().ToTable("v_data_type_rank", "public"); 

            //NodeSet Tables
            modelBuilder.Entity<StandardNodeSet>().ToTable("standard_nodeset", "public");
            modelBuilder.Entity<Profile>().ToTable("profile", "public");
            //FK nodeset to lookup nodeset table
            modelBuilder.Entity<Profile>()
                .HasOne(r => r.StandardProfile).WithMany().HasForeignKey(r => r.StandardProfileID);

            modelBuilder.Entity<Profile>()
                .HasMany(r => r.NodeSetFiles).WithMany(f => f.Profiles)
                //.UsingEntity(pf => pf.ToTable("profile_nodeset_file", "public"))
                .UsingEntity<LookupProfileNodeSetFile>(
                    pf => pf.HasOne(pf => pf.NodeSetFile).WithMany().HasForeignKey(pf => pf.NodeSetFileId),
                    pf => pf.HasOne(pf => pf.Profile).WithMany().HasForeignKey(pf => pf.ProfileId))
                ;
            modelBuilder.Entity<Profile>()
                .HasMany(x => x.ImportWarnings).WithOne();

            modelBuilder.Entity<NodeSetFile>().ToTable("nodeset_file", "public")
                .HasMany(f => f.Profiles).WithMany(p => p.NodeSetFiles)
                .UsingEntity<LookupProfileNodeSetFile>(
                    pf => pf.HasOne(r => r.Profile).WithMany().HasForeignKey(r => r.ProfileId),
                    pf => pf.HasOne(r => r.NodeSetFile).WithMany().HasForeignKey(r => r.NodeSetFileId))
            ;
            modelBuilder.Entity<Profile>()
                .HasOne(p => p.Author).WithMany().HasForeignKey(v => v.AuthorId);

            modelBuilder.Entity<UAProperty>().ToTable("profile_additional_properties", "public")
                .HasOne(prop => prop.Profile).WithMany(p => p.AdditionalProperties)
            ;

            //------------------------------------------------
            //Import log data
            //------------------------------------------------
            modelBuilder.Entity<ImportLog>().ToTable("import_log", "public")
                .HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
            modelBuilder.Entity<ImportLog>()
                .HasMany(x => x.Messages).WithOne();
            modelBuilder.Entity<ImportLog>()
                .HasMany(x => x.ProfileWarnings).WithOne();
            modelBuilder.Entity<ImportLogMessage>().ToTable("import_log_message", "public")
                .HasOne(x => x.ImportLog).WithMany(x => x.Messages).HasForeignKey(x => x.ImportLogId)
                .OnDelete(DeleteBehavior.Cascade);
            //note can cascade delete warnings if user deletes profile (or if we delete import log in future)
            modelBuilder.Entity<ImportProfileWarning>().ToTable("import_log_warning", "public")
                .HasOne(x => x.ImportLog).WithMany(x => x.ProfileWarnings).HasForeignKey(x => x.ImportLogId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ImportProfileWarning>()
                .HasOne(x => x.Profile).WithMany(x => x.ImportWarnings).HasForeignKey(x => x.ProfileId)
                ;

        }
    }
}