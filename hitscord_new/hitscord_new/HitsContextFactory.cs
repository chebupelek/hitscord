using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using hitscord.Contexts;

public class HitsContextFactory : IDesignTimeDbContextFactory<HitsContext>
{
	public HitsContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<HitsContext>();

		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=hitscord;Username=postgres;Password=postgres");

		return new HitsContext(optionsBuilder.Options);
	}
}