using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(options => options.AddPolicy("Policy_Role_A", authBuilder => authBuilder.RequireRole("app-role-A")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    var scheme = new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(builder.Configuration["AzureAd:Swagger:AuthorizationUrl"]),
                TokenUrl = new Uri(builder.Configuration["AzureAd:Swagger:TokenUrl"])
            }
        },
        Type = SecuritySchemeType.OAuth2  
    };
    options.AddSecurityDefinition("miniapp-oauth2", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { 
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Id = "miniapp-oauth2", Type = ReferenceType.SecurityScheme }, 
                Type = SecuritySchemeType.OAuth2,
            }, 
            new List<string> { } 
        }
    });    
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options => { 
            options.OAuthAppName(builder.Configuration["AzureAd:AppName"]);
            options.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
            options.OAuthScopes(builder.Configuration["AzureAd:Scopes"]);
            options.OAuthUsePkce();
        }
    );
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!").RequireAuthorization( "Policy_Role_A" );

app.MapFallback(() => Results.Redirect("/swagger"));

app.Run();
