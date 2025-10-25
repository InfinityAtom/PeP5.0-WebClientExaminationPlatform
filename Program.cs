

}).RequireAuthorization();
app.MapPost("/auth/login", async (LoginRequest request, AppRepository repo, JwtTokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Missing credentials" });
    }
    
    var user = await repo.GetUserByEmailAsync(request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }
    
    var token = tokenService.CreateToken(user.Id, user.Email, user.Name, user.Role);
    return Results.Ok(new Models.AuthResponse
    {
        Token = token,
        User = user.ToUserInfo()
    });
});

app.MapGet("/auth/me", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var userId = user.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var entity = await repo.GetUserByIdAsync(userId.Value);
    if (entity is null) return Results.NotFound(new { error = "User not found" });
    return Results.Ok(entity.ToUserInfo());
}).RequireAuthorization();



app.MapGet("/auth/me", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var userId = user.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var entity = await repo.GetUserByIdAsync(userId.Value);
    if (entity is null) return Results.NotFound(new { error = "User not found" });
    return Results.Ok(entity.ToUserInfo());
}).RequireAuthorization();

