using LABMEDIS.Core.Repositories.Base;
using CompanyProfileEntity = LABMEDIS.Core.Models.Entities.CompanyProfile;

namespace LABMEDIS.Core.Repositories.CompanyProfile;

public interface ICompanyProfileRepository : IBaseRepository<CompanyProfileEntity>
{
    /// <summary>Returns the single active company profile row, creating a default one if none exists yet.</summary>
    Task<CompanyProfileEntity> GetActiveProfileAsync(CancellationToken cancellationToken = default);
}
