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
                    .SetIsOriginAllowed(origin => true) // TODO add itch host here
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
            options.AddPolicy(Policies.HostOnly, config =>
            {
                config
                    .SetIsOriginAllowed(origin => true) // TODO add hosted domain here
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder
            .Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors();

        app.UseHealthChecks("/health");

        app.UseHsts();
        app.UseHttpsRedirection();

        // Don't use app.UseStaticFiles(): the database lives there and we do not want
        // to serve that

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
