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

        builder.Services.AddHttpClient();

        builder.Services.AddTransient<IItchService, ItchService>();
        builder.Services.AddTransient<FileService>();

        builder.Services.AddScoped<GameService>();
        builder.Services.AddDbContext<ReplayDatabase>();

        var app = builder
            .Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

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
