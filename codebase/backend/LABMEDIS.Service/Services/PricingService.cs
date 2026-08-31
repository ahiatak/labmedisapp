using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.PricingProfile;
using LABMEDIS.Core.Repositories.ProductPrice;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using PricingProfileEntity = LABMEDIS.Core.Models.Entities.PricingProfile;
using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Pricing engine (US6 — FR-045 à FR-053, RG-004). Inherits PricingProfileRepository
/// directly (Principle II); IProductPriceRepository/IStockLotService are injected
/// (composition — a class can only inherit one repository) since apply-price needs both the
/// immutable price history writer and the current CUMP.
/// </summary>
public class PricingService(AppDbContext context, IProductPriceRepository productPriceRepository, IStockLotService stockLotService)
    : PricingProfileRepository(context), IPricingService
{
    public async Task<PricingSimulationResponse> SimulateAsync(SimulatePricingRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(request.PricingProfileId, cancellationToken);
        if (profile is null || !profile.IsActive)
        {
            throw new AppException(422, "PRICING_PROFILE_NOT_FOUND", "Aucun profil de pricing actif pour cette combinaison.");
        }

        var result = PricingCascadeCalculator.Calculate(
            request.PurchasePriceForeign.ToDecimal(),
            request.ExchangeRate.ToDecimal(),
            profile.CommissionCoeff, profile.FreightCoeff, profile.TransitCoeff, profile.TransferFeeCoeff, profile.TargetMarginCoeff,
            request.VatRate.ToDecimal());

        return new PricingSimulationResponse
        {
            PurchasePriceCfa = result.PurchasePriceCfa.ToInvariantString("0"),
            LandingCostCfa = result.LandingCostCfa.ToInvariantString("0"),
            TargetPriceHtCfa = result.TargetPriceHtCfa.ToInvariantString("0"),
            TargetPriceTtcCfa = result.TargetPriceTtcCfa.ToInvariantString("0")
        };
    }

    public async Task<IReadOnlyList<PricingProfileResponse>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).Select(p => new PricingProfileResponse(p)).ToList();

    public async Task<PricingProfileResponse> CreateProfileAsync(CreatePricingProfileRequest request, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(new PricingProfileEntity(), request);
        await AddAsync(entity, cancellationToken);
        return new PricingProfileResponse(entity);
    }

    public async Task<PricingProfileResponse> UpdateProfileAsync(Guid id, CreatePricingProfileRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "PRICING_PROFILE_NOT_FOUND", "Profil introuvable.");
        MapToEntity(entity, request);
        await UpdateAsync(entity, cancellationToken);
        return new PricingProfileResponse(entity);
    }

    public async Task<ProductPriceResponse> ApplyPriceAsync(Guid productId, ApplyPriceRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var product = await Context.Set<Product>().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new AppException(404, "PRODUCT_NOT_FOUND", "Produit introuvable.");

        var cumpCfa = await stockLotService.GetWeightedAverageCostAsync(productId, cancellationToken);
        var transportMode = product.DefaultTransportMode ?? TransportMode.Maritime;
        var profile = await ResolveAsync(null, product.CategoryId, transportMode, cancellationToken);

        var targetMarginCoeff = profile?.TargetMarginCoeff ?? 1m;
        var pvHtCalculated = (cumpCfa * targetMarginCoeff).ToCfaRounded();
        var pvHtApplied = request.PvHtApplied.ToDecimal();

        var entity = new ProductPriceEntity
        {
            ProductId = productId,
            CumpCfa = cumpCfa,
            PvHtCalculated = pvHtCalculated,
            PvHtApplied = pvHtApplied,
            PriceGap = pvHtCalculated - pvHtApplied,
            VatRate = product.VatRate,
            CreatedByUserId = createdByUserId
        };

        await productPriceRepository.AddAsync(entity, cancellationToken);
        return new ProductPriceResponse(entity);
    }

    public async Task<IReadOnlyList<ProductPriceResponse>> GetHistoryAsync(Guid productId, CancellationToken cancellationToken = default) =>
        (await productPriceRepository.GetHistoryAsync(productId, cancellationToken)).Select(p => new ProductPriceResponse(p)).ToList();

    private static PricingProfileEntity MapToEntity(PricingProfileEntity entity, CreatePricingProfileRequest request)
    {
        entity.Name = request.Name;
        entity.SupplierId = request.SupplierId;
        entity.CategoryId = request.CategoryId;
        entity.TransportMode = Enum.Parse<TransportMode>(request.TransportMode);
        entity.CommissionCoeff = request.CommissionCoeff.ToDecimal();
        entity.FreightCoeff = request.FreightCoeff.ToDecimal();
        entity.TransitCoeff = request.TransitCoeff.ToDecimal();
        entity.TransferFeeCoeff = request.TransferFeeCoeff.ToDecimal();
        entity.TargetMarginCoeff = request.TargetMarginCoeff.ToDecimal();
        entity.IsActive = true;
        return entity;
    }
}
