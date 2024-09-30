using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enuns;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelsViwer;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura;

#region builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(option =>{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>{
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,

    };
});

builder.Services.AddAuthentication();

builder.Services.AddScoped <IAdminisatradorServicos, AdministradorServicos>();
builder.Services.AddScoped<IVeiculosServicos, VeiculosServicos>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name  = "Autorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o Token de Validação: "
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options =>
{

    var conexao = builder.Configuration.GetConnectionString("ConexaoPadrao");
    options.UseSqlServer(conexao); 

});
var app = builder.Build();
#endregion

app.UseSwagger();
app.UseSwaggerUI();

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Adm

//Criação do TOKEN
string GerarTokenJwt(Administrador administrador){
    if(string.IsNullOrEmpty(key)) return string.Empty;

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


    var claims = new List<Claim>(){
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Perfil),
    };

    var token = new JwtSecurityToken(
        expires : DateTime.Now.AddDays(1),
        signingCredentials : credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
};


//Criação do caminho do login
app.MapPost("/login", (LoginDTO loginDTO, IAdminisatradorServicos adminisatradorServicos) =>{
    // if (loginDTO.Email == "adm@teste.com" && loginDTO.Senha == "123456")
    var adm = adminisatradorServicos.Login(loginDTO);

    if(adm != null){
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token

        });
    }else{
        return Results.Unauthorized();
    }
}).AllowAnonymous()
.WithTags("Administradores");


app.MapGet("/Administradores", ([FromQuery] int? pagina,  IAdminisatradorServicos adminisatradorServicos) =>{
    var adms = new List<AdministradorModelView>();
    var administradores = adminisatradorServicos.Todos(pagina);

    foreach(var adm in administradores){
        adms.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }

    return Results.Ok(adms);


} ).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");

/*
app.MapPost("/administradoresTodos", ([FromQuery] int? pagina,  IAdminisatradorServicos adminisatradorServicos) =>{

    return Results.Ok(adminisatradorServicos.Todos(pagina));

}).WithTags("Administradores").RequireAuthorization();
*/

app.MapGet("/Administradores/{id}", ([FromRoute] int id,  IAdminisatradorServicos adminisatradorServicos) =>{
    var administrador = adminisatradorServicos.BuscarPorId(id);
    if(administrador == null) return Results.NotFound();
    
    // return Results.Ok(administrador);
    return Results.Ok(new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil

    });
    

}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");


app.MapPost("/Administradores", ([FromBody] AdministradorDTO administradorDTO, IAdminisatradorServicos adminisatradorServicos) =>{
    
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("Email não pode ser vazio");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("Senha não pode ser vazia");

    if (administradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil não pode ser vazio");

    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var administrador = new Administrador{
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };

    adminisatradorServicos.Incluir(administrador);

    return Results.Created($"/administrador {administrador.Id}", new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil

    });        
 
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");

#endregion

#region Veiculos

ErrosDeValidacao validacaoDTO(VeiculoDTO veiculoDTO){
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");

    if(string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("A marca não pode fica em branco");
    
    if(veiculoDTO.Ano < 1900)
        validacao.Mensagens.Add("Veiculo muito antigo, aceito somente anos superiores a 1900");

    return validacao;

}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculosServicos veiculosServicos) =>{
    
    var validacao = validacaoDTO(veiculoDTO);
    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);
    

    var veiculo = new Veiculo{
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano,
    };
    veiculosServicos.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery ]int? pagina, IVeiculosServicos veiculosServicos) =>{

    var veiculos = veiculosServicos.Todos(pagina);

    return Results.Ok(veiculos);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute]int id, IVeiculosServicos veiculosServicos) =>{

    var veiculo = veiculosServicos.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();
    return Results.Ok(veiculo);

}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute]int id, VeiculoDTO veiculoDTO ,IVeiculosServicos veiculosServicos) =>{

    var veiculo = veiculosServicos.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();

    var validacao = validacaoDTO(veiculoDTO);
    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;


    veiculosServicos.Atualizar(veiculo);
    
    return Results.Ok(veiculo);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute]int id, IVeiculosServicos veiculosServicos) =>{

    var veiculo = veiculosServicos.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();

    veiculosServicos.Apagar(veiculo);

    return Results.NoContent();

}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");

#endregion


#region App
app.UseAuthentication();
app.UseAuthorization();

app.Run();

#endregion


