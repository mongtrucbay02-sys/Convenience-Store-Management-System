using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
