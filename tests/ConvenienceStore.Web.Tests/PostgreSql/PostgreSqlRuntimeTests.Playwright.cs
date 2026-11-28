using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// Issue #244 / QA-03: actual Chromium browser, isolated loopback PG17.
// Run only from the dedicated qa-browser-e2e workflow, never shared Supabase.
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    [Trait("Category", "BrowserE2E")]
    public async Task PosBrowserE2e_LoginAddPayAndReceipt_WithTraceAndDatabaseReconciliation()
    {
        var product = await Product(stock: 20);
        var address = await StartWeb();
        var evidence = Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? Directory.GetCurrentDirectory(), "artifacts", "browser-e2e");
        Directory.CreateDirectory(evidence);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--no-sandbox"]
        });
        await using var context = await browser.NewContextAsync(new()
        {
            BaseURL = address.ToString(),
            ViewportSize = new() { Width = 1365, Height = 900 }
        });
        await context.Tracing.StartAsync(new()
        {
            Screenshots = true, Snapshots = true, Sources = true
        });
        var page = await context.NewPageAsync();
        try
        {
            // Authenticate in the real Chromium login form.
            await page.GotoAsync("/login.html");
            await page.Locator("#username").FillAsync("pg_cashier");
            await page.Locator("#password").FillAsync(Password);
            await page.Locator("#loginForm button[type=submit]").ClickAsync();
            await page.WaitForURLAsync("**/Home/Workspace");
            await page.GotoAsync("/Pos");
            Assert.Contains("Quầy bán hàng", await page.TitleAsync());
            await page.Locator(".pos-product form[action*='/Pos/Add'] button[type=submit]").First.ClickAsync();
            await page.WaitForURLAsync("**/Pos");
            Assert.Contains("Giỏ hàng: 1 sản phẩm", await page.Locator(".pos-cart-badge").InnerTextAsync());

            await page.GotoAsync("/Pos/Checkout");
            Assert.Contains("Thanh toán", await page.TitleAsync());
            await page.GotoAsync("/Pos/Cash");
            await page.Locator("#cashReceived").FillAsync("100000");
            await page.Locator("#confirmCashButton").ClickAsync();
            await page.Locator("#completeOrderButton").ClickAsync();
            await page.WaitForURLAsync("**/Pos/Receipt*");
            await page.Locator(".receipt-page").WaitForAsync();
            Assert.Contains("Hóa đơn thanh toán", await page.TitleAsync());
            await page.ScreenshotAsync(new() { Path = Path.Combine(evidence, "pos-receipt.png"), FullPage = true });

            await using var db = Db();
            Assert.Single(await db.HoaDons.ToListAsync());
            Assert.Equal(19, (await db.TonKhos.SingleAsync()).SoLuongTon);
            Assert.Single(await db.GiaoDichTienMats.ToListAsync());
            Assert.Equal(1020000, (await db.QuyTienMats.SingleAsync()).SoDu);
        }
        catch
        {
            try
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(evidence, "pos-failure.png"), FullPage = true });
            }
            catch { /* preserve original failure */ }
            throw;
        }
        finally
        {
            await context.Tracing.StopAsync(new() { Path = Path.Combine(evidence, "pos-trace.zip") });
        }
    }
}
