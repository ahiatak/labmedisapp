using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using CompanyProfileEntity = LABMEDIS.Core.Models.Entities.CompanyProfile;

namespace LABMEDIS.Core.Repositories.CompanyProfile;

public class CompanyProfileRepository(AppDbContext context)
    : BaseRepository<CompanyProfileEntity>(context), ICompanyProfileRepository
{
    public async Task<CompanyProfileEntity> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await DbSet.FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = new CompanyProfileEntity();
        return await AddAsync(profile, cancellationToken);
    }
}
