using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed class OperationalReportsService : IOperationalReportsService
{
    private readonly IOperationalReportDataSource _source;
    private readonly ITransactionalReportsDataSource? _transactions;
    private readonly TimeProvider _clock;

    public OperationalReportsService(IOperationalReportDataSource source, TimeProvider clock)
    {
        _source = source;
        _transactions = source as ITransactionalReportsDataSource;
        _clock = clock;
    }

    public OperationalReportsService(IOperationalReportDataSource source,
        ITransactionalReportsDataSource transactions, TimeProvider clock)
    {
        _source = source;
        _transactions = transactions;
        _clock = clock;
    }

    public async Task<OperationalReportResponse> GetRevenueAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-02", "Doanh thu theo thời gian", range!, query, error);
        var invoices = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var totalsByBucket = new Dictionary<DateOnly, (int Count, long Gross, long Deductions)>();
        foreach (var invoice in invoices)
        {
            var date = ReportRangeResolver.Bucket(DateOnly.FromDateTime(invoice.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime), range.GroupBy);
            totalsByBucket.TryGetValue(date, out var value);
            totalsByBucket[date] = (value.Count + 1, value.Gross + Gross(invoice), value.Deductions + invoice.PromotionDiscount);
        }
        var returns = await _source.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
        foreach (var returned in returns)
        {
            var date = ReportRangeResolver.Bucket(DateOnly.FromDateTime(returned.ReturnedAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime), range.GroupBy);
            totalsByBucket.TryGetValue(date, out var value);
            totalsByBucket[date] = (value.Count, value.Gross, value.Deductions + returned.Lines.Sum(line => line.RefundAmount));
        }
        var groups = totalsByBucket.OrderBy(x => x.Key).Select(x =>
        {
            var net = x.Value.Gross - x.Value.Deductions;
            return new RevenueBucket(x.Key, x.Value.Count, x.Value.Gross, x.Value.Deductions, net,
                x.Value.Count == 0 ? 0 : Round(net, x.Value.Count));
        }).ToList();
        var grossTotal = invoices.Sum(Gross);
        var deductionsTotal = invoices.Sum(x => x.PromotionDiscount) + returns.Sum(x => x.Lines.Sum(line => line.RefundAmount));
        var totals = new RevenueBucket(range.From, invoices.Count, grossTotal, deductionsTotal,
            grossTotal - deductionsTotal, invoices.Count == 0 ? 0 : Round(grossTotal - deductionsTotal, invoices.Count));
        return Build("BC-02", "Doanh thu theo thời gian", range, query,
            [Metric("Số hóa đơn", totals.InvoiceCount, "number"), Metric("Doanh thu gộp", totals.Gross),
             Metric("Giảm trừ", totals.Deductions), Metric("Doanh thu thuần", totals.Net),
             Metric("TB mỗi đơn", totals.Average)],
            ["Kỳ", "Số hóa đơn", "Doanh thu gộp", "Giảm trừ", "Doanh thu thuần", "TB mỗi đơn"],
            groups.Select(g => Row(g.Date.ToString("yyyy-MM-dd"),
                Label(g.Date, range.GroupBy), g.InvoiceCount.ToString(), Money(g.Gross), Money(g.Deductions),
                Money(g.Net), Money(g.Average)))
                .Append(Row("total", "Tổng cộng", totals.InvoiceCount.ToString(), Money(totals.Gross),
                    Money(totals.Deductions), Money(totals.Net), Money(totals.Average))).ToArray(),
            groups.Select(g => new ReportChartPoint(Label(g.Date, range.GroupBy), g.Net)).ToArray(),
            invoices.Count + returns.Count == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetProductsAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-03", "Doanh thu theo sản phẩm", range!, query, error);
        var products = await _source.GetProductsAsync(ct);
        var invoices = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var productMap = products.ToDictionary(p => p.Id);
        var sales = new Dictionary<long, ProductSale>();
        foreach (var invoice in invoices)
        {
            var shares = AllocateDeductions(invoice);
            for (var i = 0; i < invoice.Lines.Count; i++)
            {
                var line = invoice.Lines[i];
                if (!productMap.TryGetValue(line.ProductId, out var product)) continue;
                if (query.CategoryId.HasValue && product.CategoryId != query.CategoryId.Value) continue;
                if (query.ProductId.HasValue && product.Id != query.ProductId.Value) continue;
                if (!sales.TryGetValue(product.Id, out var sale)) sale = new ProductSale(product);
                sale.Quantity += line.Quantity;
                sale.Gross += (long)line.Quantity * line.ListPriceSnapshot;
                sale.Deductions += shares[i];
                sale.InvoiceIds.Add(invoice.Id);
                sales[product.Id] = sale;
            }
        }
        var returns = await _source.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
        foreach (var returned in returns)
        foreach (var line in returned.Lines)
        {
            if (!productMap.TryGetValue(line.ProductId, out var product) ||
                (query.CategoryId.HasValue && product.CategoryId != query.CategoryId.Value) ||
                (query.ProductId.HasValue && product.Id != query.ProductId.Value)) continue;
            if (!sales.TryGetValue(product.Id, out var sale)) sale = new ProductSale(product);
            sale.Deductions += line.RefundAmount;
            sale.InvoiceIds.Add(returned.InvoiceId);
            sales[product.Id] = sale;
        }
        var rows = sales.Values.Select(x => x with { Net = x.Gross - x.Deductions }).ToList();
        var totalNet = rows.Sum(x => x.Net);
        var sorted = (query.SortBy.Equals("revenue", StringComparison.OrdinalIgnoreCase)
                ? rows.OrderByDescending(x => x.Net).ThenByDescending(x => x.Quantity)
                : rows.OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Net))
            .ThenBy(x => x.Product.Name, StringComparer.Ordinal).ToList();
        var topN = query.TopN is 10 or 20 or 50 ? query.TopN : 10;
        var chartRows = sorted.Take(topN).ToArray();
        var unsold = products.Where(p => (!query.CategoryId.HasValue || p.CategoryId == query.CategoryId) &&
                                          (!query.ProductId.HasValue || p.Id == query.ProductId) &&
                                          !sales.ContainsKey(p.Id)).ToArray();
        var allRows = chartRows.Select(x => Row($"product:{x.Product.Id}", x.Product.Name,
            x.Quantity.ToString(), Money(x.Gross), Money(x.Deductions), Money(x.Net),
            Percent(totalNet == 0 ? 0 : x.Net * 100m / totalNet), string.Join(',', x.InvoiceIds))).ToList();
        allRows.AddRange(unsold.Select(p => Row($"product:{p.Id}", p.Name + " (không bán)", "0", Money(0), Money(0), Money(0), "0,0%", "")));
        allRows.Add(Row("total", "Tổng cộng", rows.Sum(x => x.Quantity).ToString(), Money(rows.Sum(x => x.Gross)),
            Money(rows.Sum(x => x.Deductions)), Money(totalNet), "100,0%", ""));
        return Build("BC-03", "Doanh thu theo sản phẩm", range, query,
            [Metric("Số lượng bán", rows.Sum(x => x.Quantity), "number"), Metric("Doanh thu gộp", rows.Sum(x => x.Gross)),
             Metric("Giảm trừ", rows.Sum(x => x.Deductions)), Metric("Doanh thu thuần", totalNet)],
            ["Sản phẩm", "Số lượng", "Gộp", "Giảm trừ", "Thuần", "Tỷ trọng", "Mã hóa đơn"],
            allRows,
            chartRows.Select(x => new ReportChartPoint(x.Product.Name, x.Quantity, x.Net)).ToArray(),
            invoices.Count + returns.Count == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetCategoriesAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-04", "Doanh thu theo danh mục", range!, query, error);
        var products = (await _source.GetProductsAsync(ct)).ToDictionary(p => p.Id);
        var invoices = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var categories = new Dictionary<int, CategorySale>();
        foreach (var invoice in invoices)
        {
            var shares = AllocateDeductions(invoice);
            for (var i = 0; i < invoice.Lines.Count; i++)
            {
                var line = invoice.Lines[i];
                if (!products.TryGetValue(line.ProductId, out var product)) continue;
                if (!categories.TryGetValue(product.CategoryId, out var sale))
                    sale = new CategorySale(product.CategoryName);
                sale.Quantity += line.Quantity;
                sale.Gross += (long)line.Quantity * line.ListPriceSnapshot;
                sale.Deductions += shares[i];
                sale.InvoiceIds.Add(invoice.Id);
                categories[product.CategoryId] = sale;
            }
        }
        var returns = await _source.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
        foreach (var returned in returns)
        foreach (var line in returned.Lines)
        {
            if (query.CategoryId.HasValue && query.CategoryId.Value != line.CategoryId) continue;
            if (!categories.TryGetValue(line.CategoryId, out var sale)) sale = new CategorySale(line.CategoryName);
            sale.Deductions += line.RefundAmount;
            sale.InvoiceIds.Add(returned.InvoiceId);
            categories[line.CategoryId] = sale;
        }
        var netTotal = categories.Values.Sum(x => x.Gross - x.Deductions);
        var data = categories.OrderByDescending(x => x.Value.Gross - x.Value.Deductions).ToArray();
        return Build("BC-04", "Doanh thu theo danh mục", range, query,
            [Metric("Số danh mục", data.Length, "number"), Metric("Số lượng bán", data.Sum(x => x.Value.Quantity), "number"),
             Metric("Doanh thu thuần", netTotal)],
            ["Danh mục", "Số lượng", "Doanh thu gộp", "Giảm trừ", "Doanh thu thuần", "Tỷ trọng", "Mã hóa đơn"],
            data.Select(x => Row($"category:{x.Key}", x.Value.Name, x.Value.Quantity.ToString(),
                Money(x.Value.Gross), Money(x.Value.Deductions), Money(x.Value.Gross - x.Value.Deductions),
                Percent(netTotal == 0 ? 0 : (x.Value.Gross - x.Value.Deductions) * 100m / netTotal),
                string.Join(',', x.Value.InvoiceIds)))
                .Append(Row("total", "Tổng cộng", data.Sum(x => x.Value.Quantity).ToString(),
                    Money(data.Sum(x => x.Value.Gross)), Money(data.Sum(x => x.Value.Deductions)),
                    Money(netTotal), "100,0%", "")).ToArray(),
            data.Select(x => new ReportChartPoint(x.Value.Name, x.Value.Gross - x.Value.Deductions)).ToArray(),
            invoices.Count + returns.Count == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetCashiersAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-05", "Doanh thu theo thu ngân", range!, query, error);
        var invoices = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var returns = await _source.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
        var cancelled = await _source.GetCancelledOrdersAsync(range.FromInclusive, range.ToExclusive, ct);
        var voided = await _source.GetVoidedInvoicesAsync(range.FromInclusive, range.ToExclusive, ct);
        var cashierIds = invoices.Select(x => x.CashierId).Concat(returns.Select(x => x.CashierId)).Concat(cancelled.Select(x => x.CashierId))
            .Concat(voided.Select(x => x.CashierId)).Distinct().ToArray();
        var items = cashierIds.Select(id =>
        {
            var sales = invoices.Where(x => x.CashierId == id).ToArray();
            var returnedBy = returns.Where(x => x.CashierId == id).ToArray();
            var name = sales.FirstOrDefault()?.CashierName ?? returnedBy.FirstOrDefault()?.CashierName ?? cancelled.FirstOrDefault(x => x.CashierId == id)?.CashierName
                ?? voided.FirstOrDefault(x => x.CashierId == id)?.CashierName ?? "Không rõ";
            var refund = returnedBy.Sum(x => x.Lines.Sum(line => line.RefundAmount));
            return new CashierSale(id, name, sales.Length, sales.Sum(Gross), sales.Sum(Deductions) + refund,
                sales.Sum(Gross) - sales.Sum(Deductions) - refund, sales.Sum(x => x.CashPaid),
                cancelled.Count(x => x.CashierId == id), voided.Count(x => x.CashierId == id),
                sales.Select(x => x.Id).ToArray());
        }).Where(x => !query.CashierId.HasValue || x.Id == query.CashierId.Value)
            .OrderByDescending(x => x.Net).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
        return Build("BC-05", "Doanh thu theo thu ngân", range, query,
            [Metric("Số hóa đơn", items.Sum(x => x.Invoices), "number"), Metric("Doanh thu thuần", items.Sum(x => x.Net)),
             Metric("Đơn hủy trước thanh toán", items.Sum(x => x.CancelledOrders), "number"),
             Metric("Hóa đơn hủy sau thanh toán", items.Sum(x => x.VoidedInvoices), "number"),
             Metric("Tiền mặt thu", items.Sum(x => x.CashPaid))],
            ["Thu ngân", "Số hóa đơn", "Doanh thu thuần", "Đơn hủy trước TT", "Hóa đơn hủy", "Tiền mặt thu", "Mã hóa đơn"],
            items.Select(x => Row($"cashier:{x.Id}", x.Name, x.Invoices.ToString(), Money(x.Net),
                x.CancelledOrders.ToString(), x.VoidedInvoices.ToString(), Money(x.CashPaid), string.Join(',', x.InvoiceIds)))
                .Append(Row("total", "Tổng cộng", items.Sum(x => x.Invoices).ToString(), Money(items.Sum(x => x.Net)),
                    items.Sum(x => x.CancelledOrders).ToString(), items.Sum(x => x.VoidedInvoices).ToString(),
                    Money(items.Sum(x => x.CashPaid)), "")).ToArray(),
            items.Select(x => new ReportChartPoint(x.Name, x.Net)).ToArray(),
            invoices.Count + returns.Count + cancelled.Count + voided.Count == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetInventoryAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-06", "Báo cáo tồn kho", range!, query, error);
        var products = await _source.GetProductsAsync(ct);
        // Pull movements from range start through today to reconstruct the closing stock on the selected date.
        var todayEnd = new DateTimeOffset(DateOnly.FromDateTime(_clock.GetUtcNow().ToOffset(ReportRangeResolver.VietnamOffset).DateTime)
            .AddDays(1).ToDateTime(TimeOnly.MinValue), ReportRangeResolver.VietnamOffset);
        var movements = await _source.GetStockMovementsAsync(range!.FromInclusive, todayEnd, ct);
        var selected = movements.Where(m => m.OccurredAt >= range.FromInclusive && m.OccurredAt < range.ToExclusive).ToArray();
        var result = products.Select(p =>
        {
            var productMoves = movements.Where(m => m.ProductId == p.Id).ToArray();
            var selectedMoves = selected.Where(m => m.ProductId == p.Id).ToArray();
            var laterNet = productMoves.Where(m => m.OccurredAt >= range.ToExclusive)
                .Sum(m => m.IsInbound ? m.Quantity : -m.Quantity);
            var closing = p.CurrentStock - laterNet;
            var incoming = selectedMoves.Where(m => m.IsInbound).Sum(m => m.Quantity);
            var outgoing = selectedMoves.Where(m => !m.IsInbound).Sum(m => m.Quantity);
            var opening = closing - incoming + outgoing;
            return new InventoryLine(p, opening, incoming, outgoing, closing);
        }).Where(x => !query.CategoryId.HasValue || x.Product.CategoryId == query.CategoryId.Value)
          .Where(x => !query.ProductId.HasValue || x.Product.Id == query.ProductId.Value)
          .Where(x => query.StockStatus.ToLowerInvariant() switch
          {
              "low" => x.Product.CurrentStock > 0 && x.Product.CurrentStock <= x.Product.ReorderPoint,
              "out" => x.Product.CurrentStock == 0,
              _ => true
          }).OrderBy(x => x.Product.CategoryName, StringComparer.Ordinal).ThenBy(x => x.Product.Name, StringComparer.Ordinal).ToArray();
        var lowCount = result.Count(p => p.Product.CurrentStock > 0 && p.Product.CurrentStock <= p.Product.ReorderPoint);
        var outCount = result.Count(p => p.Product.CurrentStock == 0);
        var value = result.Sum(x => (long)x.Product.CurrentStock * x.Product.UnitCost);
        return Build("BC-06", "Báo cáo tồn kho", range, query,
            [Metric("Sản phẩm", result.Length, "number"), Metric("Sắp hết", lowCount, "number"),
             Metric("Hết hàng", outCount, "number"), Metric("Giá trị tồn", value)],
            ["Sản phẩm", "Danh mục", "Tồn hiện tại", "Giá vốn", "Giá trị tồn", "Trạng thái", "Tồn đầu", "Nhập", "Xuất", "Tồn cuối"],
            result.Select(x => Row($"product:{x.Product.Id}", x.Product.Name, x.Product.CategoryName,
                x.Product.CurrentStock.ToString(), Money(x.Product.UnitCost), Money((long)x.Product.CurrentStock * x.Product.UnitCost),
                x.Product.CurrentStock == 0 ? "Hết hàng" : x.Product.CurrentStock <= x.Product.ReorderPoint ? "Sắp hết" : "Đủ hàng",
                x.Opening.ToString(), x.Incoming.ToString(), x.Outgoing.ToString(), x.Closing.ToString()))
                .Append(Row("total", "Tổng cộng", "", "", Money(value), "",
                    result.Sum(x => x.Opening).ToString(), result.Sum(x => x.Incoming).ToString(),
                    result.Sum(x => x.Outgoing).ToString(), result.Sum(x => x.Closing).ToString())).ToArray(),
            [], result.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetPurchasesAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-07", "Báo cáo nhập hàng", range!, query, error);
        var transactions = _transactions ?? throw new InvalidOperationException("BC-07 cần nguồn dữ liệu phiếu nhập.");
        var receipts = await transactions.GetPurchaseReceiptsAsync(range!.FromInclusive, range.ToExclusive, ct);
        var selected = receipts.SelectMany(receipt => receipt.Lines
                .Where(line => !query.ProductId.HasValue || line.ProductId == query.ProductId.Value)
                .Select(line => (Receipt: receipt, Line: line)))
            .Where(x => !query.SupplierId.HasValue || x.Receipt.SupplierId == query.SupplierId.Value)
            .ToArray();
        var productRows = selected.GroupBy(x => x.Line.ProductId).Select(group =>
        {
            var first = group.First().Line;
            var quantity = group.Sum(x => x.Line.Quantity);
            var value = group.Sum(x => (long)x.Line.Quantity * x.Line.UnitCost);
            return new PurchaseProduct(group.Key, first.ProductName, group.Select(x => x.Receipt.Id).Distinct().Count(),
                quantity, value, Round(value, quantity));
        }).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
        var suppliers = selected.GroupBy(x => (x.Receipt.SupplierId, x.Receipt.SupplierName))
            .Select(group => new { group.Key.SupplierName, Value = group.Sum(x => (long)x.Line.Quantity * x.Line.UnitCost) })
            .OrderByDescending(x => x.Value).ToArray();
        var receiptCount = selected.Select(x => x.Receipt.Id).Distinct().Count();
        var totalQuantity = productRows.Sum(x => x.Quantity);
        var totalValue = productRows.Sum(x => x.Value);
        var rows = productRows.Select(x => Row($"product:{x.ProductId}", x.Name, x.ReceiptCount.ToString(),
                x.Quantity.ToString(), Money(x.Value), Money(x.AverageUnitCost))).ToList();
        if (rows.Count > 0)
            rows.Add(Row("total", "Tổng cộng", receiptCount.ToString(), totalQuantity.ToString(), Money(totalValue), "—"));
        return Build("BC-07", "Báo cáo nhập hàng", range, query,
            [Metric("Số phiếu nhập", receiptCount, "number"), Metric("Tổng số lượng", totalQuantity, "number"),
             Metric("Tổng giá trị nhập", totalValue)],
            ["Sản phẩm", "Số phiếu", "Số lượng nhập", "Giá trị nhập", "Giá nhập TB"], rows,
            suppliers.Select(x => new ReportChartPoint(x.SupplierName, x.Value)).ToArray(),
            receiptCount == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    public async Task<OperationalReportResponse> GetGrossProfitAsync(ReportQuery query, CancellationToken ct)
    {
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid("BC-08", "Báo cáo lợi nhuận gộp", range!, query, error);
        var transactions = _transactions ?? throw new InvalidOperationException("BC-08 cần nguồn dữ liệu giao dịch.");
        var invoices = await transactions.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var returns = await transactions.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
        var writeOffs = await transactions.GetWriteOffsAsync(range.FromInclusive, range.ToExclusive, ct);
        var buckets = new SortedDictionary<DateOnly, GrossProfitBucket>();
        GrossProfitBucket For(DateOnly date)
        {
            if (!buckets.TryGetValue(date, out var bucket)) buckets[date] = bucket = new GrossProfitBucket();
            return bucket;
        }

        foreach (var invoice in invoices)
        {
            var lines = invoice.Lines.Where(line => MatchesProduct(line.ProductId, line.CategoryId, query)).ToArray();
            if (lines.Length == 0) continue;
            var allGross = invoice.Lines.Sum(line => (long)line.Quantity * line.ListPriceSnapshot);
            var filteredGross = lines.Sum(line => (long)line.Quantity * line.ListPriceSnapshot);
            var allocatedPromotion = allGross == 0 ? 0 : (long)Math.Floor((decimal)invoice.PromotionDiscount * filteredGross / allGross);
            var date = DateOnly.FromDateTime(invoice.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime);
            var bucket = For(date);
            bucket.GrossRevenue += filteredGross;
            bucket.PromotionDiscount += allocatedPromotion;
            bucket.SoldCost += lines.Sum(line => (long)line.Quantity * line.UnitCostSnapshot);
        }

        foreach (var returned in returns)
        {
            var lines = returned.Lines.Where(line => MatchesProduct(line.ProductId, line.CategoryId, query)).ToArray();
            if (lines.Length == 0) continue;
            var date = DateOnly.FromDateTime(returned.ReturnedAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime);
            var bucket = For(date);
            bucket.Refunds += lines.Sum(line => line.RefundAmount);
            bucket.RestockedCost += lines.Sum(line => (long)line.RestockedQuantity * line.UnitCostSnapshot);
            bucket.DestroyedReturns += lines.Sum(line => line.DestroyedValue);
        }

        foreach (var writeOff in writeOffs.Where(x => MatchesProduct(x.ProductId, x.CategoryId, query)))
        {
            var date = DateOnly.FromDateTime(writeOff.WrittenOffAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime);
            For(date).WriteOffValue += (long)writeOff.Quantity * writeOff.UnitCostSnapshot;
        }

        var data = buckets.Select(x => new GrossProfitDay(x.Key, x.Value.GrossRevenue - x.Value.PromotionDiscount - x.Value.Refunds,
            x.Value.SoldCost - x.Value.RestockedCost, x.Value.WriteOffValue, x.Value.DestroyedReturns)).ToArray();
        var netRevenue = data.Sum(x => x.NetRevenue);
        var costOfGoods = data.Sum(x => x.CostOfGoods);
        var profit = netRevenue - costOfGoods;
        var margin = netRevenue == 0 ? 0 : profit * 100m / netRevenue;
        var writeOffValue = data.Sum(x => x.WriteOffValue);
        var destroyedReturns = data.Sum(x => x.DestroyedReturns);
        var rows = data.Select(x => Row("day:" + x.Date.ToString("yyyy-MM-dd"), x.Date.ToString("dd/MM/yyyy"),
            Money(x.NetRevenue), Money(x.CostOfGoods), Money(x.Profit), Percent(x.NetRevenue == 0 ? 0 : x.Profit * 100m / x.NetRevenue),
            Money(x.WriteOffValue), Money(x.DestroyedReturns))).ToList();
        if (rows.Count > 0)
            rows.Add(Row("total", "Tổng cộng", Money(netRevenue), Money(costOfGoods), Money(profit), Percent(margin), Money(writeOffValue), Money(destroyedReturns)));
        return Build("BC-08", "Báo cáo lợi nhuận gộp", range, query,
            [Metric("Doanh thu thuần", netRevenue), Metric("Giá vốn hàng bán", costOfGoods),
             Metric("Lợi nhuận gộp", profit), Metric("Tỷ suất lợi nhuận", (long)Math.Round(margin * 10, MidpointRounding.AwayFromZero), "percent"),
             Metric("Giá trị xuất hủy", writeOffValue), Metric("Hàng trả bị hủy", destroyedReturns)],
            ["Ngày", "Doanh thu thuần", "Giá vốn hàng bán", "Lợi nhuận gộp", "Tỷ suất", "Giá trị xuất hủy", "Hàng trả bị hủy"],
            rows, data.Select(x => new ReportChartPoint(x.Date.ToString("dd/MM"), x.NetRevenue, x.Profit)).ToArray(),
            data.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    private static bool MatchesProduct(long productId, int categoryId, ReportQuery query) =>
        (!query.ProductId.HasValue || query.ProductId.Value == productId) &&
        (!query.CategoryId.HasValue || query.CategoryId.Value == categoryId);

    public async Task<OperationalReportResponse> GetDetailsAsync(string reportCode, string key,
        ReportQuery query, CancellationToken ct)
    {
        var normalizedCode = reportCode.ToUpperInvariant();
        var (range, error) = Resolve(query);
        if (error is not null) return Invalid(normalizedCode + " detail", "Chi tiết báo cáo", range!, query, error);
        if (normalizedCode == "BC-07")
        {
            var receipts = await _source.GetPurchaseReceiptsAsync(range!.FromInclusive, range.ToExclusive, ct);
            var detailReceipts = receipts.Where(receipt =>
                (!query.SupplierId.HasValue || receipt.SupplierId == query.SupplierId.Value) &&
                ((key.StartsWith("product:", StringComparison.Ordinal) && long.TryParse(key[8..], out var productId) && receipt.Lines.Any(line => line.ProductId == productId)) ||
                 (key.StartsWith("supplier:", StringComparison.Ordinal) && int.TryParse(key[9..], out var supplierId) && receipt.SupplierId == supplierId))).ToArray();
            var purchaseRows = detailReceipts.SelectMany(receipt => receipt.Lines
                    .Where(line => (!query.ProductId.HasValue || line.ProductId == query.ProductId.Value) &&
                                   (!key.StartsWith("product:", StringComparison.Ordinal) || key == $"product:{line.ProductId}"))
                    .Select(line => Row($"receipt:{receipt.Id}", $"PN-{receipt.Id:000000}",
                        receipt.ReceivedAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm"),
                        receipt.SupplierName, line.ProductName, line.Quantity.ToString(), Money(line.UnitCost),
                        Money((long)line.Quantity * line.UnitCost))))
                .ToArray();
            return Build("BC-07 detail", "Chi tiết phiếu nhập", range, query,
                [Metric("Số dòng nhập", purchaseRows.Length, "number")],
                ["Phiếu nhập", "Thời điểm nhận", "Nhà cung cấp", "Sản phẩm", "Số lượng", "Giá nhập", "Thành tiền"],
                purchaseRows, [], purchaseRows.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
        }
        if (normalizedCode == "BC-08" && key.StartsWith("day:", StringComparison.Ordinal) &&
            DateOnly.TryParse(key[4..], out var grossProfitDate))
        {
            var invoicesForDay = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
            var salesRows = invoicesForDay.Where(invoice =>
                    DateOnly.FromDateTime(invoice.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime) == grossProfitDate)
                .SelectMany(invoice => invoice.Lines.Where(line => MatchesProduct(line.ProductId, line.CategoryId, query))
                    .Select(line => Row($"invoice:{invoice.Id}", $"HD-{invoice.Id:000000}", "Bán hàng",
                        invoice.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm"),
                        line.ProductName, line.Quantity.ToString(), Money(line.ListPriceSnapshot), Money(line.UnitCostSnapshot))))
                .ToList();
            var returnsForDay = await _source.GetReturnsAsync(range.FromInclusive, range.ToExclusive, ct);
            var returnRows = returnsForDay.Where(returned =>
                    DateOnly.FromDateTime(returned.ReturnedAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime) == grossProfitDate)
                .SelectMany(returned => returned.Lines.Where(line => MatchesProduct(line.ProductId, line.CategoryId, query))
                    .Select(line => Row($"return:{returned.Id}", $"PT-{returned.Id:000000}", "Trả hàng",
                        returned.ReturnedAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm"),
                        line.ProductName, line.Quantity.ToString(), Money(line.RefundAmount),
                        line.RestockedQuantity > 0 ? "Nhập lại kho" : "Không nhập lại kho")))
                .ToArray();
            var allRows = salesRows.Concat(returnRows).ToArray();
            return Build("BC-08 detail", "Chi tiết doanh thu và giá vốn", range, query,
                [Metric("Số dòng giao dịch", allRows.Length, "number")],
                ["Mã", "Loại", "Thời điểm (UTC+7)", "Sản phẩm", "Số lượng", "Giá / hoàn tiền", "Giá vốn / xử lý"],
                allRows, [], allRows.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
        }
        var invoices = await _source.GetPaidInvoicesAsync(range!.FromInclusive, range.ToExclusive, ct);
        var selectedInvoices = invoices.AsEnumerable();
        if (normalizedCode == "BC-02" && DateOnly.TryParse(key, out var bucketDate))
            selectedInvoices = selectedInvoices.Where(i => ReportRangeResolver.Bucket(
                DateOnly.FromDateTime(i.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).DateTime), range.GroupBy) == bucketDate);
        else if (normalizedCode == "BC-03" && key.StartsWith("product:", StringComparison.Ordinal) && long.TryParse(key[8..], out var productId))
            selectedInvoices = selectedInvoices.Where(i => i.Lines.Any(l => l.ProductId == productId));
        else if (normalizedCode == "BC-04" && key.StartsWith("category:", StringComparison.Ordinal) && int.TryParse(key[9..], out var categoryId))
        {
            var productsById = (await _source.GetProductsAsync(ct)).ToDictionary(p => p.Id);
            selectedInvoices = selectedInvoices.Where(i => i.Lines.Any(l => productsById.TryGetValue(l.ProductId, out var p) && p.CategoryId == categoryId));
        }
        else if (normalizedCode == "BC-05" && key.StartsWith("cashier:", StringComparison.Ordinal) && long.TryParse(key[8..], out var cashierId))
            selectedInvoices = selectedInvoices.Where(i => i.CashierId == cashierId);
        else if (normalizedCode == "BC-06" && key.StartsWith("product:", StringComparison.Ordinal) && long.TryParse(key[8..], out var stockProductId))
        {
            var todayEnd = new DateTimeOffset(DateOnly.FromDateTime(_clock.GetUtcNow().ToOffset(ReportRangeResolver.VietnamOffset).DateTime)
                .AddDays(1).ToDateTime(TimeOnly.MinValue), ReportRangeResolver.VietnamOffset);
            var moves = await _source.GetStockMovementsAsync(range.FromInclusive, todayEnd, ct);
            var rows = moves.Where(m => m.ProductId == stockProductId && m.OccurredAt >= range.FromInclusive && m.OccurredAt < range.ToExclusive)
                .OrderBy(m => m.OccurredAt)
                .Select(m => Row("movement", m.OccurredAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm"),
                    m.IsInbound ? "Nhập" : "Xuất", m.Quantity.ToString(), (m.IsInbound ? "+" : "-") + m.Quantity))
                .ToArray();
            return Build("BC-06 detail", "Chi tiết thẻ kho", range, query,
                [Metric("Số biến động", rows.Length, "number")], ["Thời điểm", "Loại", "Số lượng", "Ảnh hưởng tồn"], rows, [],
                rows.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
        }
        else
            return Invalid(normalizedCode + " detail", "Chi tiết báo cáo", range, query, "Dòng báo cáo không hợp lệ.");

        var detailInvoices = selectedInvoices.OrderByDescending(i => i.PaidAt).ToArray();
        var detailRows = detailInvoices.Select(i =>
        {
            var gross = Gross(i);
            return Row($"invoice:{i.Id}", $"HD-{i.Id:000000}",
                i.PaidAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm"),
                i.CashierName,
                string.Join("; ", i.Lines.Select(l => $"{l.ProductName} × {l.Quantity}")),
                Money(gross), Money(Deductions(i)), Money(gross - Deductions(i)));
        }).ToArray();
        var detailGross = detailInvoices.Sum(Gross);
        var detailDeductions = detailInvoices.Sum(Deductions);
        return Build(normalizedCode + " detail", "Chi tiết hóa đơn", range, query,
            [Metric("Số hóa đơn", detailInvoices.Length, "number"), Metric("Doanh thu gộp", detailGross),
             Metric("Giảm trừ", detailDeductions), Metric("Doanh thu thuần", detailGross - detailDeductions)],
            ["Hóa đơn", "Thanh toán lúc (UTC+7)", "Thu ngân", "Sản phẩm", "Gộp", "Giảm trừ", "Thuần"], detailRows, [],
            detailRows.Length == 0 ? "Không có dữ liệu trong khoảng đã chọn" : null);
    }

    private (ResolvedReportRange? Range, string? Error) Resolve(ReportQuery query)
    {
        var ok = ReportRangeResolver.TryResolve(query, _clock.GetUtcNow(), out var range, out var error);
        return ok ? (range, null) : (DefaultRange(query), error);
    }

    private static ResolvedReportRange DefaultRange(ReportQuery query)
    {
        var date = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        return new ResolvedReportRange(date, date, new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), ReportRangeResolver.VietnamOffset),
            new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), ReportRangeResolver.VietnamOffset), "custom", "day");
    }

    private static OperationalReportResponse Invalid(string code, string title, ResolvedReportRange range, ReportQuery query, string error) =>
        new(code, title, range, [], [], [], [], error, query);

    private static OperationalReportResponse Build(string code, string title, ResolvedReportRange range,
        ReportQuery query, IReadOnlyList<ReportMetric> metrics, IReadOnlyList<string> columns,
        IReadOnlyList<ReportTableRow> rows, IReadOnlyList<ReportChartPoint> chart, string? message) =>
        new(code, title, range, metrics, columns, rows, chart, message, query);

    private static ReportMetric Metric(string label, long value, string format = "currency") => new(label, value, format);
    private static ReportTableRow Row(string key, params string[] cells) => new(key, cells);
    private static long Gross(ReportPaidInvoiceFact i) => i.Lines.Sum(l => (long)l.Quantity * l.ListPriceSnapshot);
    private static long Deductions(ReportPaidInvoiceFact i) => i.PromotionDiscount + i.RefundAmount;
    private static long Round(long value, int count) => (long)Math.Round((decimal)value / count, MidpointRounding.AwayFromZero);
    private static string Money(long value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    private static string Percent(decimal value) => value.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + "%";
    private static string Label(DateOnly date, string groupBy) => groupBy switch
    {
        "month" => date.ToString("MM/yyyy"),
        "week" => "Tuần từ " + date.ToString("dd/MM/yyyy"),
        _ => date.ToString("dd/MM/yyyy")
    };

    private static long[] AllocateDeductions(ReportPaidInvoiceFact invoice)
    {
        var grosses = invoice.Lines.Select(l => (long)l.Quantity * l.ListPriceSnapshot).ToArray();
        var total = grosses.Sum();
        var deductions = Math.Clamp(invoice.PromotionDiscount + invoice.RefundAmount, 0, total);
        var allocations = new long[grosses.Length];
        var allocated = 0L;
        for (var i = 0; i < grosses.Length; i++)
        {
            allocations[i] = i == grosses.Length - 1 || total == 0
                ? deductions - allocated
                : (long)Math.Floor((decimal)deductions * grosses[i] / total);
            allocated += allocations[i];
        }
        return allocations;
    }

    private sealed record RevenueBucket(DateOnly Date, int InvoiceCount, long Gross, long Deductions, long Net, long Average);
    private sealed record ProductSale(ReportProductFact Product)
    {
        public int Quantity { get; set; }
        public long Gross { get; set; }
        public long Deductions { get; set; }
        public long Net { get; init; }
        public HashSet<long> InvoiceIds { get; } = [];
    }
    private sealed record CategorySale(string Name)
    {
        public int Quantity { get; set; }
        public long Gross { get; set; }
        public long Deductions { get; set; }
        public HashSet<long> InvoiceIds { get; } = [];
    }
    private sealed record CashierSale(long Id, string Name, int Invoices, long Gross, long Deductions, long Net,
        long CashPaid, int CancelledOrders, int VoidedInvoices, IReadOnlyList<long> InvoiceIds);
    private sealed record InventoryLine(ReportProductFact Product, int Opening, int Incoming, int Outgoing, int Closing);
    private sealed record PurchaseProduct(long ProductId, string Name, int ReceiptCount, int Quantity, long Value, long AverageUnitCost);
    private sealed class GrossProfitBucket
    {
        public long GrossRevenue { get; set; }
        public long PromotionDiscount { get; set; }
        public long Refunds { get; set; }
        public long SoldCost { get; set; }
        public long RestockedCost { get; set; }
        public long WriteOffValue { get; set; }
        public long DestroyedReturns { get; set; }
    }
    private sealed record GrossProfitDay(DateOnly Date, long NetRevenue, long CostOfGoods, long WriteOffValue, long DestroyedReturns)
    {
        public long Profit => NetRevenue - CostOfGoods;
    }
}

