using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Estud.Back.Extensions;

public static class EstudDbContextExtensions
{
    extension(EstudDbContext context)
    {
        public bool HasMissingMigration()
        {
            var modelDiffer = context.GetService<IMigrationsModelDiffer>();

            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            var modelInitializer = context.GetService<IModelRuntimeInitializer>();

            var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
            if (snapshotModel is IMutableModel mutableModel)
                snapshotModel = mutableModel.FinalizeModel();
            if (snapshotModel is not null)
                snapshotModel = modelInitializer.Initialize(snapshotModel);

            var designTimeModel = context.GetService<IDesignTimeModel>();

            return modelDiffer.HasDifferences(
                snapshotModel?.GetRelationalModel(),
                designTimeModel.Model.GetRelationalModel());
        }
    }
}
