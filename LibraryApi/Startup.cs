using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryApi.Persistance;
using LibraryApi.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LibraryApi
{
    using System.IO;

    using Microsoft.EntityFrameworkCore.Diagnostics;

    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = string.Format(Configuration.GetConnectionString("Database"), AppContext.BaseDirectory);

            var isDbInMemory = Configuration.GetConnectionString("Database") == "MEMORY";

            // Before main service configuration
            if (isDbInMemory)
            {
                services.AddDbContext<DatabaseContext>(opt => opt
                    .UseInMemoryDatabase("LibraryInMemoryDb")
                    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            }
            else
            {
                services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connectionString));
            }

            services.AddCors();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddTransient<UserManager>();

            services.AddSwaggerGen(
                c =>
                    {
                        c.SwaggerDoc(
                            "v1",
                            new Info
                                {
                                    Version = "v1",
                                    Title = "Document Handling API",
                                    Description = "Store and sign documents",
                                    TermsOfService = "None"
                                });

                        // Set the comments path for the swagger json and ui.
                        var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
                        var xmlPath = Path.Combine(basePath, "Library.xml");
                        c.IncludeXmlComments(xmlPath);
                    });

            var key = Encoding.ASCII.GetBytes("myhugesecretkey123456789012345678901234567890qwertyuio");
            services.AddAuthentication(x =>
                    {
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddJwtBearer(x =>
                    {
                        x.Events = new JwtBearerEvents
                                       {
                                           OnTokenValidated = context =>
                                               {
                                                   var db = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();
                                                   var userId = int.Parse(context.Principal.Identity.Name);
                                                   var user = db.Users.FirstOrDefault(u => u.Id == userId);
                                                   if (user == null)
                                                   {
                                                       // return unauthorized if user no longer exists
                                                       context.Fail("Unauthorized");
                                                   }
                                                   return Task.CompletedTask;
                                               }
                                       };
                        x.RequireHttpsMetadata = false;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                                                          {
                                                              ValidateIssuerSigningKey = true,
                                                              IssuerSigningKey = new SymmetricSecurityKey(key),
                                                              ValidateIssuer = false,
                                                              ValidateAudience = false
                                                          };
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library"); });
            app.Use(async (httpContext, next) =>
                {
                    if (httpContext.Request.Path == "/")
                    {
                        httpContext.Response.Redirect("swagger");
                        return;
                    }

                    await next().ConfigureAwait(false);
                });

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
