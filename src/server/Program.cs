using Game.Server.Services;
using Game.Server.Services.Abstractions;

namespace Game.Server;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
        });

        builder.Services.AddHealthChecks();

        builder.Services.AddHttpClient();

        builder.Services.AddTransient<IItchService, ItchService>();
        builder.Services.AddTransient<FileService>();

        builder.Services.AddScoped<GameService>();
        builder.Services.AddDbContext<ReplayDatabase>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(config =>
            {
                config
                    .SetIsOriginAllowed(origin => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
            options.AddPolicy(Policies.HostOnly, config =>
            {
                var serverHost = builder.Configuration["Server:Host"];
                config
                    .SetIsOriginAllowed(origin => new Uri(origin).Host.Equals(serverHost, StringComparison.CurrentCultureIgnoreCase))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddHttpLogging(config => { });

        var app = builder
            .Build();

        app.UseHttpLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            // On production nginx takes care of this
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        else if (app.Environment.IsProduction())
        {
            using var scope = app.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<ReplayDatabase>();

            db.Database.EnsureCreated();
        }

        app.UseCors();

        app.UseHealthChecks("/health");

        // Don't use app.UseStaticFiles(): the database lives there and we do not want
        // to serve that

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
