using System.Security.Claims;
using MiniShop.Models;

namespace MiniShop.Security;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        user.FindUserId() ?? throw new InvalidOperationException("В токене нет идентификатора пользователя.");

    public static int? FindUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
