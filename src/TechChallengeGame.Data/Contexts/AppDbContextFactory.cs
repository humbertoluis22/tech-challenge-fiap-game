using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TechChallengeGame.Data.Contexts;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Constrói a configuração para ler a string de conexão do appsettings.json
        var configuration = new ConfigurationBuilder()
            // Acessa a pasta do projeto principal, onde o appsettings.json está
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TechChallengeGame.Application"))
            .AddJsonFile("appsettings.json")
            .Build();

        // Pega a string de conexão
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Cria as opções do DbContext manualmente
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
