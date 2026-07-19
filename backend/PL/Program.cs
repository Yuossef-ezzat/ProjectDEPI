using BLL.Hubs;
using BLL.Mapper;
using BLL.Services.AbstractServices;
using BLL.Services.AbstractServices.Admin;
using BLL.Services.AbstractServices.AppointmentModule;
using BLL.Services.AbstractServices.ConsultationModule;
using BLL.Services.AbstractServices.MedicationModule;
using BLL.Services.AbstractServices.Users;
using BLL.Services.ImplementationService;
using BLL.Services.ImplementationService.Admin;
using BLL.Services.ImplementationService.AppointmentModule;
using BLL.Services.ImplementationService.ConsultationModule;
using BLL.Services.ImplementationService.MedicationModule;
using BLL.Services.ImplementationService.NursingModule;
using DAL.Data;
using DAL.Models.Users;
using DAL.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PL.Utilites;
using ServiceAbstractionLayer;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using TalabatDemo.CustomMiddleWares;

namespace PL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            builder.Services.AddScoped<DataSeeder, DataSeeder>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IBasketService, BasketService>();
            builder.Services.AddScoped<IMedicationService, MedicationService>();
            builder.Services.AddScoped<INursingService, NursingService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();
            builder.Services.AddScoped<IConsultationChatService, ConsultationChatService>();
            builder.Services.AddScoped<IConsultationReviewService, ConsultationReviewService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
            builder.Services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IProfileUserService, ProfileUserService>();

            
            builder.Services.AddAutoMapper((x) => { }, typeof(DomainProfile).Assembly);
            builder.Services.AddSignalR();

            builder.Services.AddControllers();

            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options => {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    Description = "Enter 'Bearer' Followed by space And your token"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new string []{}
                    }
                });
            });
            


            builder.Services.AddAuthentication(Config =>
            {
                Config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                Config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(Config =>
            {
                Config.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtOptions:Issuer"],
                    ValidateAudience = true,
                    
                    ValidAudience = builder.Configuration["JwtOptions:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtOptions:SecretKey"]!)),
                };
                Config.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && 
                                    context.HttpContext.Request.Path.StartsWithSegments("/notificationHub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            
            builder.Services.AddDbContext<TabibyDbContext>(
                options => {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("TabibyDbContext"));
                }
            );
            builder.Services.AddIdentityCore<ApplicationUser>(
                options =>
                {
                    
                    
                    
                })
                .AddRoles<IdentityRole<int>>()
                .AddRoleManager<RoleManager<IdentityRole<int>>>()
                .AddEntityFrameworkStores<TabibyDbContext>()
                .AddDefaultTokenProviders();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", builder =>
                {
                    builder
                        .WithOrigins("http://localhost:3000", "http://localhost:3001")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            var app = builder.Build();

            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.ConfigObject = new ConfigObject()
                    {
                        DisplayRequestDuration = true
                    };
                    options.DocumentTitle = "Tabiby APP";
                    options.DocExpansion(DocExpansion.None);
                    options.EnableFilter();
                    options.EnablePersistAuthorization();
                });
            }
            
            using var scope = app.Services.CreateScope();
            var seed = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seed.SeedDatabaseAsync();

            EmailSettings.Initialize(app.Configuration);

            app.UseHttpsRedirection();
            app.UseMiddleware<CustomExceptionHandlerMiddleWare>();
            app.UseCors("AllowLocalhost");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseStaticFiles();
            app.MapHub<NotificationHub>("/notificationHub");
            app.MapHub<ChatHub>("/chatHub");
            app.MapControllers();

            app.Run();
        }
    }
}
