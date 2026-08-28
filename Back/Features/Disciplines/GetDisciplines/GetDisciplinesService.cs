using Dapper;

namespace Estud.Back.Features.Disciplines.GetDisciplines;

public class GetDisciplinesService(EstudDbContext ctx) : IEstudService
{
    private const int MaxPageSize = 100;

    public async Task<GetDisciplinesOut> Get(GetDisciplinesIn query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var connection = await ctx.GetOpenConnectionAsync();

        const string sql = @"
            WITH filtered AS (
                SELECT
                    d.id,
                    d.name,
                    d.code
                FROM
                    estud.disciplines d
                WHERE
                    d.institution_id = @InstitutionId
                    AND (@Filter IS NULL OR d.name ILIKE @Filter OR d.code ILIKE @Filter)
            )
            SELECT
                d.id,
                d.name,
                d.code,
                EXISTS (
                    SELECT 1 FROM estud.courses_disciplines cd WHERE cd.discipline_id = d.id
                ) AS has_courses,
                EXISTS (
                    SELECT 1 FROM estud.teachers_disciplines td WHERE td.discipline_id = d.id
                ) AS has_teachers,
                COUNT(*) OVER() AS total_rows
            FROM
                estud.disciplines d
            WHERE
                d.institution_id = @InstitutionId
                    AND
                (@Filter IS NULL OR d.name ILIKE @Filter OR d.code ILIKE @Filter)
                    AND
                (
                    @HasCourses IS NULL
                    OR @HasCourses = EXISTS (
                        SELECT 1 FROM estud.courses_disciplines cd WHERE cd.discipline_id = d.id
                    )
                )
                    AND
                (
                    @HasTeachers IS NULL
                    OR @HasTeachers = EXISTS (
                        SELECT 1 FROM estud.teachers_disciplines td WHERE td.discipline_id = d.id
                    )
                )
            ORDER BY
                d.name, d.id
            LIMIT @PageSize
            OFFSET @Offset
        ";

        var filter = query.Filter.HasValue() ? $"%{query.Filter}%" : null;
        var parameters = new
        {
            Filter = filter,
            ctx.RequestUser.InstitutionId,
            query.HasCourses,
            query.HasTeachers,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize,
        };

        var rows = (await connection.QueryAsync<DisciplineRow>(sql, parameters)).ToList();

        return new GetDisciplinesOut
        {
            Page = page,
            PageSize = pageSize,
            Total = rows.FirstOrDefault()?.TotalRows ?? 0,
            Items = rows.ConvertAll(r => r.ToGetDisciplinesItemOut()),
        };
    }
}
