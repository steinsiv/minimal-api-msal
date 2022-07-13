# minimal-api-msal
minimum setup of `.net` minimal api with `swagger` and `msal` using Azure App roles.
OAuth2 flow is `AuthorizationCode` with `PKCE`

### Setup application in Azure
https://entra.microsoft.com

| Menu  | SubMenu  | Action  |   
|---|---|---|---|
|`App Registrations`   |  `Expose an API` |  make sure `Application ID URI` is set |  
|   |   | add a scope `access_as_user`  |   
|   | `App Roles`  | Create some roles, including `app-role-A` |   
|   | `Redirect URI`  | Create SPA redirect URI, e.g. `https://localhost:PORT/swagger/oauth2-redirect.html`  |   
|   | `API Permissions`  | Add Microsoft.Graph -> `User.Read`  |   
|   | `Token configuration` | Add `login_hint`  |   
| `Enterprise Application`  | `Users and Groups`  | Connect security group(s) with your app role(s)  |   

Connect users to the same security groups you want to test.

### Init project
Create project with `dotnet` and primary parameters from your app registration
For more info see https://aka.ms/dotnet-template-ms-identity-platform
```sh
dotnet new webapi -au SingleOrg --aad-instance "https://login.microsoftonline.com/" --client-id "109e12e2-4ca7-48d0-af05-c834c884322c" --tenant-id "b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f" -minimal
```


### Configuration
Add configuration in `appsettings.json` for `Oauth2` and `Swagger`

```json
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f",
    "ClientId": "109e12e2-4ca7-48d0-af05-c834c884322c",
    "Scopes": "User.Read 109e12e2-4ca7-48d0-af05-c834c884322c/access_as_user",
    "TokenValidationParameters": {
      "ValidateAudience": false
    },
    "Swagger": {
      "AuthorizationUrl": "https://login.microsoftonline.com/b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f/oauth2/v2.0/authorize",
      "TokenUrl": "https://login.microsoftonline.com/b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f/oauth2/v2.0/authorize"
    }
  }
  ```

### Extend Program.cs

##### Add the authorization policy to be used by endpoints
```cs
builder.Services.AddAuthorization(options => 
    options.AddPolicy("Policy_Role_A", authBuilder => authBuilder.RequireRole("app-role-A")));
```

##### Add SwaggerGen (use appsettings?)
```cs
builder.Services.AddSwaggerGen(options => {
    var scheme = new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Flows = new OpenApiOAuthFlows {
            AuthorizationCode = new OpenApiOAuthFlow {
                AuthorizationUrl = new Uri("https://login.microsoftonline.com/b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.com/b3edbf8f-e8b2-4c4e-96fc-c86cdd7ed55f/oauth2/v2.0/token")
            }
        },
        Type = SecuritySchemeType.OAuth2  
    };
    options.AddSecurityDefinition("miniapp-oauth2", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { 
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Id = "miniapp-oauth2", Type = ReferenceType.SecurityScheme }, 
                Type = SecuritySchemeType.OAuth2,
            }, 
            new List<string> { } 
        }
    });    
});
```

##### Adjust `UseSwaggerUI`

```cs
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options => { 
            options.OAuthAppName("miniapp");
            options.OAuthClientId("109e12e2-4ca7-48d0-af05-c834c884322c");
            options.OAuthScopes("109e12e2-4ca7-48d0-af05-c834c884322c/access_as_user");
            options.OAuthUsePkce();
            //options.InjectStylesheet("./css/swagger-extras.css");
        }
    );
}
````

##### Add an endpoint w/Auth
```cs
app.MapGet("/", () => "Hello World!").RequireAuthorization( "Policy_Role_A" );
````
