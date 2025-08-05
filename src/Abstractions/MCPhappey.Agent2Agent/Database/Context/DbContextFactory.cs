using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MCPhappey.Servers.SQL.Context;

public class DbContextFactory : IDesignTimeDbContextFactory<A2ADatabaseContext>
{
  public A2ADatabaseContext CreateDbContext(string[] args)
  {
    if (args.Length != 1)
    {
      throw new InvalidOperationException("Please provide connection string like this: dotnet ef database update -- \"yourConnectionString\"");
    }

    var optionsBuilder = new DbContextOptionsBuilder<A2ADatabaseContext>();
    optionsBuilder.UseSqlServer(args[0], options => options.EnableRetryOnFailure());

    return new A2ADatabaseContext(optionsBuilder.Options);
  }
}
