using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.CustomerReturn;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using CreditNoteEntity = LABMEDIS.Core.Models.Entities.CreditNote;
using CustomerReturnEntity = LABMEDIS.Core.Models.Entities.CustomerReturn;

namespace LABMEDIS.Service.Services;

/// <summary>Customer returns and credit notes (US8 — FR-060 à FR-062). Inherits CustomerReturnRepository directly (Principle II).</summary>
public class CustomerReturnService(AppDbContext context, IStockLotService stockLotService) : CustomerReturnRepository(context), ICustomerReturnService
{
    /// <summary>No explicit period is specified anywhere in the spec (FR-060 only requires "verifying" a deadline exists) — 30 days from delivery is a documented, conservative default pending business confirmation.</summary>
    public const int ReturnWindowDays = 30;

    public async Task<CustomerReturnResponse> CreateAsync(Guid saleOrderId, CreateReturnRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new AppException(400, "INVALID_QUANTITY", "La quantité du retour doit être supérieure à zéro.");
        }

        var disposition = Enum.Parse<ReturnDisposition>(request.Disposition);
        if (disposition == ReturnDisposition.Quarantaine && string.IsNullOrWhiteSpace(request.Motif))
        {
            throw new AppException(400, "QUARANTINE_MOTIF_REQUIRED", "Un motif est requis pour une mise en quarantaine (FR-061).");
        }

        var saleOrder = await Context.Set<SaleOrder>().FirstOrDefaultAsync(o => o.Id == saleOrderId, cancellationToken)
            ?? throw new AppException(404, "SALE_ORDER_NOT_FOUND", "Commande de vente introuvable.");

        if (saleOrder.Status is not (SaleOrderStatus.Livree or SaleOrderStatus.Facturee))
        {
            throw new AppException(400, "SALE_ORDER_NOT_DELIVERED", "Un retour ne peut être initié que sur une commande livrée (FR-060).");
        }

        var delivery = await Context.Set<Delivery>()
            .Where(d => d.SaleOrderId == saleOrderId)
            .OrderByDescending(d => d.DeliveryDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (delivery is not null && delivery.DeliveryDate.AddDays(ReturnWindowDays) < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new AppException(422, "RETURN_WINDOW_EXPIRED", $"Le délai de retour de {ReturnWindowDays} jours après livraison est dépassé.");
        }

        var line = await Context.Set<SaleOrderLine>().FirstOrDefaultAsync(l => l.Id == request.SaleOrderLineId && l.SaleOrderId == saleOrderId, cancellationToken)
            ?? throw new AppException(422, "SALE_ORDER_LINE_NOT_FOUND", "Cette ligne n'appartient pas à cette commande de vente.");

        if (request.Quantity > line.Quantity)
        {
            throw new AppException(422, "QUANTITY_EXCEEDS_DELIVERED", "La quantité retournée dépasse la quantité livrée sur cette ligne.");
        }

        var returnDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var sequence = await GetNextSequenceForTodayAsync(cancellationToken);

        var returnEntity = new CustomerReturnEntity
        {
            ReturnNumber = $"RET-{returnDate:yyyyMMdd}-{sequence:D4}",
            SaleOrderId = saleOrderId,
            CustomerId = saleOrder.CustomerId,
            ReturnDate = returnDate,
            Status = CustomerReturnStatus.Initie,
            Reason = request.Motif ?? "Non spécifié",
            Lines =
            [
                new ReturnLine
                {
                    SaleOrderLineId = request.SaleOrderLineId,
                    OriginalStockLotId = line.AllocatedStockLotId,
                    Quantity = request.Quantity,
                    Disposition = disposition,
                    Motif = request.Motif
                }
            ]
        };

        await AddAsync(returnEntity, cancellationToken);

        if (line.AllocatedStockLotId.HasValue)
        {
            await stockLotService.CreateFromReturnAsync(line.AllocatedStockLotId.Value, request.Quantity, request.Disposition, request.Motif, userId, cancellationToken);
        }

        // FR-062 — an avoir (credit note) is generated for every processed return, regardless of disposition.
        var creditNoteAmount = line.UnitPriceHt * request.Quantity;
        var creditNoteNumber = $"AV-{returnDate:yyyyMMdd}-{sequence:D4}";
        var creditNote = new CreditNoteEntity { CreditNoteNumber = creditNoteNumber, CustomerReturnId = returnEntity.Id, Amount = creditNoteAmount };
        Context.Set<CreditNoteEntity>().Add(creditNote);

        returnEntity.CreditNoteId = creditNote.Id;
        returnEntity.Status = CustomerReturnStatus.Traite;
        await UpdateAsync(returnEntity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        return new CustomerReturnResponse(returnEntity, creditNoteNumber, creditNoteAmount);
    }

    public async Task<CustomerReturnResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var creditNote = entity.CreditNoteId.HasValue
            ? await Context.Set<CreditNoteEntity>().FirstOrDefaultAsync(c => c.Id == entity.CreditNoteId, cancellationToken)
            : null;
        return new CustomerReturnResponse(entity, creditNote?.CreditNoteNumber, creditNote?.Amount);
    }

    public async Task<IReadOnlyList<CustomerReturnResponse>> ListBySaleOrderAsync(Guid saleOrderId, CancellationToken cancellationToken = default)
    {
        var returns = await DbSet.Where(r => r.SaleOrderId == saleOrderId).ToListAsync(cancellationToken);
        var creditNoteIds = returns.Where(r => r.CreditNoteId.HasValue).Select(r => r.CreditNoteId!.Value).ToList();
        var creditNotes = await Context.Set<CreditNoteEntity>().Where(c => creditNoteIds.Contains(c.Id)).ToListAsync(cancellationToken);

        return returns.Select(r =>
        {
            var creditNote = creditNotes.FirstOrDefault(c => c.Id == r.CreditNoteId);
            return new CustomerReturnResponse(r, creditNote?.CreditNoteNumber, creditNote?.Amount);
        }).ToList();
    }
}
